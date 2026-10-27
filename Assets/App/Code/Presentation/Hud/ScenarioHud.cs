using System.Globalization;
using System.Linq;
using System.Text;
using App.Code.Domain.Analysis;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;
using App.Code.Presentation.Logging;
using App.Code.Presentation.Modes;
using App.Code.Presentation.Runner;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace App.Code.Presentation.Hud
{
    public sealed class ScenarioHud : MonoBehaviour
    {
        private const int JournalLineLimit = 400;

        [SerializeField] private ScenarioRunner runner;
        [SerializeField] private HudTexts texts = new HudTexts();
        [SerializeField] private Text tasksText;
        [SerializeField] private Text messageText;
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private Text resultText;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private Text journalText;
        [SerializeField] private GameObject journalPanel;
        [SerializeField] private ScrollRect journalScroll;
        [SerializeField] private RectTransform modeButtons;
        [SerializeField] private RectTransform analyzerButtons;
        [SerializeField] private Button buttonTemplate;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button journalButton;
        [SerializeField] private Button loadFileButton;
        [SerializeField] private Button closeResultButton;
        [SerializeField, Min(0.5f)] private float messageSeconds = 3.5f;

        private float _messageHideAt;
        private bool _journalDirty;

        private void Start()
        {
            if (runner == null)
            {
                Debug.LogError("[ScenarioHud] Runner is not assigned", this);
                enabled = false;
                return;
            }

            for (var index = 0; index < runner.Modes.Count; index++)
            {
                var modeIndex = index;
                if (runner.Modes[index] != null) CreateButton(modeButtons, runner.Modes[index].DisplayName, () => runner.StartMode(modeIndex));
            }

            foreach (var analyzer in runner.Analyzers.Where(analyzer => analyzer != null))
                CreateButton(analyzerButtons, $"{texts.AnalyzeWith}: {analyzer.DisplayName}", () => runner.Analyze(analyzer));

            restartButton.onClick.AddListener(runner.Restart);
            journalButton.onClick.AddListener(ToggleJournal);
            loadFileButton.onClick.AddListener(LoadFile);
            closeResultButton.onClick.AddListener(() => resultPanel.SetActive(false));

            runner.StateChanged += RefreshTasks;
            runner.ViolationRaised += ShowViolation;
            runner.RecordAdded += OnRecordAdded;
            runner.ReportReady += ShowReport;

            messagePanel.SetActive(false);
            resultPanel.SetActive(runner.LastReport != null);
            journalPanel.SetActive(false);
            RefreshTasks();
            if (runner.LastReport != null) ShowReport(runner.LastReport);
        }

        private void OnDestroy()
        {
            if (runner == null) return;

            runner.StateChanged -= RefreshTasks;
            runner.ViolationRaised -= ShowViolation;
            runner.RecordAdded -= OnRecordAdded;
            runner.ReportReady -= ShowReport;
        }

        private void Update()
        {
            if (messagePanel.activeSelf && Time.unscaledTime >= _messageHideAt) messagePanel.SetActive(false);
            if (_journalDirty && journalPanel.activeSelf) RefreshJournal();
        }

        private void CreateButton(RectTransform parent, string label, UnityAction onClick)
        {
            var button = Instantiate(buttonTemplate, parent);
            button.name = label;
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<Text>().text = label;
            button.onClick.AddListener(onClick);
        }

        private void RefreshTasks()
        {
            _journalDirty = true;
            var session = runner.Session;
            var builder = new StringBuilder();

            if (runner.LastError.Length > 0) builder.Append("<color=#ff6b6b>").Append(runner.LastError).AppendLine("</color>");

            if (session == null)
            {
                builder.Append(texts.NotStarted);
                tasksText.text = builder.ToString();
                if (runner.Journal.Records.Count > 0) resultPanel.SetActive(runner.LastReport != null);
                return;
            }

            builder.Append("<b>").Append(texts.Mode).Append(": ").Append(runner.CurrentMode.DisplayName).AppendLine("</b>");
            var plan = session.Plan;
            var total = plan.StepIds.Count();
            var done = plan.StepIds.Count(id => session.GetStatus(id).IsTerminal());
            builder.Append(texts.Progress).Append(": ").Append(done).Append('/').Append(total).AppendLine();

            if (session.IsFinished)
            {
                builder.AppendLine().Append("<color=#7bed9f>").Append(texts.Finished).Append("</color>");
                tasksText.text = builder.ToString();
                return;
            }

            if (plan.Groups.Count > 1 || runner.CurrentMode.Policy is ExamPolicyAsset)
            {
                builder.Append(texts.Group).Append(' ').Append(session.CurrentGroupIndex + 1).Append('/').Append(plan.Groups.Count);
                if (!string.IsNullOrWhiteSpace(session.CurrentGroup.Name)) builder.Append(" — ").Append(session.CurrentGroup.Name);
                builder.AppendLine();
                builder.Append(texts.GroupViolations).Append(": ").Append(session.CurrentGroupViolations);
                if (runner.CurrentMode.Policy is ExamPolicyAsset exam && exam.ViolationLimit > 0) builder.Append('/').Append(exam.ViolationLimit);
                builder.AppendLine();
            }

            builder.AppendLine().Append("<b>").Append(texts.ActiveTasks).AppendLine(":</b>");
            foreach (var id in session.ActiveStepIds)
            {
                var title = runner.TryGetStep(id, out var step) ? step.Instruction : id;
                builder.Append("• ").Append(title);
                var attempt = session.GetAttempt(id);
                if (attempt > 1) builder.Append(" <color=#ffd166>(").Append(texts.Attempt).Append(' ').Append(attempt).Append(")</color>");
                builder.AppendLine();
            }

            tasksText.text = builder.ToString();
        }

        private void ShowViolation(ViolationEvent violation)
        {
            var title = violation.Step != null ? violation.Step.Instruction : violation.Notice.StepId;
            var reason = violation.Notice.OutOfTurn ? texts.OutOfTurn : violation.Message;
            var consequence = violation.Notice.OutOfTurn ? string.Empty :
                violation.Notice.Response == ViolationResponse.Retry ? texts.Retry : texts.Failed;

            var builder = new StringBuilder();
            builder.Append("<b>").Append(reason).AppendLine("</b>").Append(title);
            if (consequence.Length > 0) builder.Append(" — ").Append(consequence);
            ShowMessage(builder.ToString());
        }

        private void ShowMessage(string message)
        {
            messageText.text = message;
            messagePanel.SetActive(true);
            _messageHideAt = Time.unscaledTime + messageSeconds;
        }

        private void ShowReport(AnalysisReport report)
        {
            var builder = new StringBuilder();
            var analyzerName = runner.LastAnalyzer != null ? runner.LastAnalyzer.DisplayName : report.AnalyzerName;
            builder.Append("<b>").Append(texts.Result).Append(": ").Append(analyzerName).Append("</b>");
            if (report.Mode.Length > 0) builder.Append(" (").Append(report.Mode).Append(')');
            builder.AppendLine();
            builder.Append("<size=26><b>").Append(texts.Score).Append(": ").Append(Format(report.Score)).Append(" / ")
                .Append(Format(report.MaxScore)).AppendLine("</b></size>");
            builder.Append(texts.JournalSource).Append(": ").AppendLine(runner.JournalSource);
            builder.AppendLine();

            foreach (var step in report.Steps)
            {
                var title = runner.TryGetStep(step.StepId, out var scene) ? scene.Instruction : string.Empty;
                builder.Append("<color=").Append(StatusColor(step.Status)).Append('>').Append(texts.Status(step.Status)).Append("</color>  ")
                    .Append(step.StepId);
                if (title.Length > 0) builder.Append(" <color=#9aa5b1>").Append(Shorten(title, 26)).Append("</color>");
                if (step.Attempts > 0) builder.Append("  ").Append(texts.Attempt).Append(' ').Append(step.Attempts);
                if (step.Violations > 0) builder.Append(", ").Append(texts.Violations).Append(' ').Append(step.Violations);
                builder.Append("  <b>").Append(step.Points > 0 ? "+" : string.Empty).Append(Format(step.Points)).AppendLine("</b>");
            }

            if (report.Warnings.Count > 0)
            {
                builder.AppendLine().Append("<color=#ffd166><b>").Append(texts.Warnings).AppendLine(":</b>");
                foreach (var warning in report.Warnings) builder.Append("• ").AppendLine(warning);
                builder.Append("</color>");
            }

            resultText.text = builder.ToString();
            resultPanel.SetActive(true);
        }

        private void ToggleJournal()
        {
            journalPanel.SetActive(!journalPanel.activeSelf);
            if (journalPanel.activeSelf) RefreshJournal();
        }

        private void LoadFile()
        {
            runner.LoadLatestJournalFile(out var message);
            ShowMessage(message);
            if (journalPanel.activeSelf) RefreshJournal();
        }

        private void OnRecordAdded(LogRecord record) => _journalDirty = true;

        private void RefreshJournal()
        {
            _journalDirty = false;
            var records = runner.Journal.Records;
            var builder = new StringBuilder();
            builder.Append("<b>").Append(texts.Journal).Append("</b> (").Append(texts.JournalSource).Append(": ")
                .Append(runner.JournalSource).Append(", ").Append(records.Count).AppendLine(")");
            foreach (var record in records.Skip(Mathf.Max(0, records.Count - JournalLineLimit)))
                builder.AppendLine(JournalFormatter.Line(record));

            journalText.text = builder.ToString();
            Canvas.ForceUpdateCanvases();
            journalScroll.verticalNormalizedPosition = 0f;
        }

        private static string StatusColor(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Completed: return "#7bed9f";
                case StepStatus.Failed: return "#ff6b6b";
                case StepStatus.Skipped: return "#ffd166";
                default: return "#c8d6e5";
            }
        }

        private static string Shorten(string value, int length) =>
            value.Length <= length ? value : value.Substring(0, length - 1) + "…";

        private static string Format(double value) => value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
