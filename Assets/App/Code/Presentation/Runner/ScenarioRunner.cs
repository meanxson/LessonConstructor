using System;
using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Analysis;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;
using App.Code.Infrastructure.Journal;
using App.Code.Presentation.Analysis;
using App.Code.Presentation.Events;
using App.Code.Presentation.Logging;
using App.Code.Presentation.Modes;
using App.Code.Presentation.State;
using App.Code.Presentation.Steps;
using UnityEngine;

namespace App.Code.Presentation.Runner
{
    public readonly struct ViolationEvent
    {
        public ViolationEvent(Step step, ViolationNotice notice, string message)
        {
            Step = step;
            Notice = notice;
            Message = message;
        }

        public Step Step { get; }

        public ViolationNotice Notice { get; }

        public string Message { get; }
    }

    public sealed class ScenarioRunner : MonoBehaviour
    {
        [SerializeField] private List<ScenarioMode> modes = new List<ScenarioMode>();
        [SerializeField, Min(0)] private int defaultModeIndex;
        [SerializeField] private bool startOnPlay = true;
        [SerializeField] private List<LoggerAsset> loggers = new List<LoggerAsset>();
        [SerializeField] private List<AnalyzerAsset> analyzers = new List<AnalyzerAsset>();

        private readonly Dictionary<ScenarioEventSource, List<Step>> _routes = new Dictionary<ScenarioEventSource, List<Step>>();
        private readonly Dictionary<string, Step> _stepsById = new Dictionary<string, Step>();
        private FanOutLogger _output;
        private string _pendingViolationMessage = string.Empty;

        public event Action StateChanged;

        public event Action<ViolationEvent> ViolationRaised;

        public event Action<LogRecord> RecordAdded;

        public event Action<AnalysisReport> ReportReady;

        public IReadOnlyList<ScenarioMode> Modes => modes;

        public IReadOnlyList<AnalyzerAsset> Analyzers => analyzers;

        public ScenarioMode CurrentMode { get; private set; }

        public ScenarioSession Session { get; private set; }

        public ScenarioJournal Journal { get; private set; } = new ScenarioJournal();

        public string JournalSource { get; private set; } = string.Empty;

        public AnalysisReport LastReport { get; private set; }

        public AnalyzerAsset LastAnalyzer { get; private set; }

        public string LastError { get; private set; } = string.Empty;

        public bool TryGetStep(string stepId, out Step step) => _stepsById.TryGetValue(stepId ?? string.Empty, out step);

        public void StartMode(int index)
        {
            if (index < 0 || index >= modes.Count || modes[index] == null)
            {
                Fail($"Mode index {index} is not configured.");
                return;
            }

            StartMode(modes[index]);
        }

        public void StartMode(ScenarioMode mode)
        {
            StopCurrent();
            LastReport = null;
            LastAnalyzer = null;
            LastError = string.Empty;
            CurrentMode = mode;
            ResetScene();

            var problems = mode.Validate();
            foreach (var problem in problems) Debug.LogWarning($"[ScenarioRunner] {problem}", mode);

            ScenarioPlan plan;
            try
            {
                plan = mode.BuildPlan();
            }
            catch (ArgumentException exception)
            {
                Fail($"Mode '{mode.DisplayName}' cannot start: {exception.Message}");
                return;
            }

            if (mode.Policy == null)
            {
                Fail($"Mode '{mode.DisplayName}' has no execution policy.");
                return;
            }

            Journal = new ScenarioJournal();
            Journal.Added += OnRecordAdded;
            JournalSource = mode.DisplayName;
            _output = CreateOutput(mode);

            foreach (var step in mode.Steps.Distinct())
            {
                _stepsById[step.Id] = step;
                foreach (var source in step.Sources)
                {
                    if (!_routes.TryGetValue(source, out var routed))
                    {
                        routed = new List<Step>();
                        _routes.Add(source, routed);
                        source.Raised += OnSignal;
                    }

                    routed.Add(step);
                }
            }

            Session = new ScenarioSession(plan, mode.Policy.Create(), mode.OutOfTurn, _output);
            Session.StepStatusChanged += OnStepStatusChanged;
            Session.GroupChanged += NotifyChanged;
            Session.ViolationRecorded += OnViolationRecorded;
            Session.Finished += OnFinished;
            Session.Start();
            NotifyChanged();
        }

        public void Restart()
        {
            if (CurrentMode != null) StartMode(CurrentMode);
            else StartMode(defaultModeIndex);
        }

        public AnalysisReport Analyze(AnalyzerAsset analyzer)
        {
            if (analyzer == null) return null;

            LastAnalyzer = analyzer;
            LastReport = analyzer.Create().Analyze(Journal.Records);
            ReportReady?.Invoke(LastReport);
            return LastReport;
        }

        public bool LoadLatestJournalFile(out string message)
        {
            var directory = loggers.OfType<JsonFileLoggerAsset>().Select(logger => logger.Directory).FirstOrDefault();
            if (directory == null)
            {
                message = "No JSON file logger is configured on the runner.";
                return false;
            }

            var path = JsonLinesJournalReader.FindLatest(directory);
            if (path.Length == 0)
            {
                message = $"No journal files in {directory}";
                return false;
            }

            StopCurrent();
            var file = JsonLinesJournalReader.Read(path);
            foreach (var warning in file.Warnings) Debug.LogWarning($"[ScenarioRunner] {warning}", this);

            Journal = new ScenarioJournal(file.Records);
            JournalSource = System.IO.Path.GetFileName(path);
            message = $"Loaded {file.Records.Count} records from {path}";
            NotifyChanged();
            var analyzer = CurrentMode != null && CurrentMode.Analyzer != null ? CurrentMode.Analyzer : analyzers.FirstOrDefault();
            Analyze(analyzer);
            return true;
        }

        private void Start()
        {
            if (startOnPlay) StartMode(Mathf.Clamp(defaultModeIndex, 0, Mathf.Max(0, modes.Count - 1)));
        }

        private void OnDestroy() => StopCurrent();

        private FanOutLogger CreateOutput(ScenarioMode mode)
        {
            var runId = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Sanitize(mode.DisplayName)}";
            var targets = new List<IScenarioLogger> { Journal };
            foreach (var logger in loggers.Where(logger => logger != null))
            {
                try
                {
                    targets.Add(logger.Create(runId));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[ScenarioRunner] Logger '{logger.name}' is unavailable: {exception.Message}", logger);
                }
            }

            var output = new FanOutLogger(targets);
            output.WriteFailed += (target, exception) =>
                Debug.LogWarning($"[ScenarioRunner] Logger {target.GetType().Name} failed: {exception.Message}", this);
            return output;
        }

        private void StopCurrent()
        {
            foreach (var source in _routes.Keys.Where(source => source != null)) source.Raised -= OnSignal;
            _routes.Clear();
            _stepsById.Clear();

            if (Session != null)
            {
                Session.StepStatusChanged -= OnStepStatusChanged;
                Session.GroupChanged -= NotifyChanged;
                Session.ViolationRecorded -= OnViolationRecorded;
                Session.Finished -= OnFinished;
                Session = null;
            }

            Journal.Added -= OnRecordAdded;
            _output?.Dispose();
            _output = null;
        }

        private void OnSignal(ScenarioSignal signal)
        {
            if (Session == null || !Session.IsRunning || !_routes.TryGetValue(signal.Source, out var steps)) return;

            foreach (var step in steps.ToArray())
            {
                if (Session == null || !Session.IsRunning) return;

                var status = Session.GetStatus(step.Id);
                if (status == StepStatus.Pending && step.Owns(signal.Source))
                {
                    _pendingViolationMessage = string.Empty;
                    Session.ReportOutOfTurn(step.Id);
                    continue;
                }

                if (status != StepStatus.Active) continue;

                var reaction = step.React(signal);
                switch (reaction.Kind)
                {
                    case StepReactionKind.Success:
                        Session.Complete(step.Id);
                        break;
                    case StepReactionKind.Progress:
                        Session.ReportProgress(step.Id, reaction.Message);
                        break;
                    case StepReactionKind.Violation:
                        _pendingViolationMessage = reaction.Message;
                        Session.Violate(step.Id, reaction.Reason);
                        step.ResetProgress();
                        break;
                }
            }
        }

        private void OnViolationRecorded(ViolationNotice notice)
        {
            _stepsById.TryGetValue(notice.StepId, out var step);
            ViolationRaised?.Invoke(new ViolationEvent(step, notice, _pendingViolationMessage));
            _pendingViolationMessage = string.Empty;
        }

        private void OnStepStatusChanged(string stepId, StepStatus status)
        {
            if (status == StepStatus.Active && _stepsById.TryGetValue(stepId, out var step)) step.ResetProgress();
            NotifyChanged();
        }

        private void OnFinished()
        {
            _output?.Dispose();
            _output = null;
            NotifyChanged();
            var analyzer = CurrentMode.Analyzer != null ? CurrentMode.Analyzer : analyzers.FirstOrDefault();
            if (analyzer != null) Analyze(analyzer);
        }

        private void OnRecordAdded(LogRecord record) => RecordAdded?.Invoke(record);

        private void ResetScene()
        {
            var resettables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                .OfType<IScenarioResettable>();
            foreach (var resettable in resettables) resettable.ResetState();
        }

        private void Fail(string message)
        {
            LastError = message;
            Debug.LogError($"[ScenarioRunner] {message}", this);
            NotifyChanged();
        }

        private void NotifyChanged() => StateChanged?.Invoke();

        private static string Sanitize(string value) =>
            new string((value ?? "mode").Select(symbol => char.IsLetterOrDigit(symbol) ? symbol : '_').ToArray());
    }
}
