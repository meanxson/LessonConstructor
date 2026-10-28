using System.Linq;
using App.Code.Domain.Analysis;
using App.Code.Presentation.Runner;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;

namespace App.Code.Presentation.UI.Hud.ViewModels
{
    public sealed class ResultViewModel : BaseWindowViewModel<ResultState>
    {
        private readonly IScenarioRunner _runner;
        private readonly MessageViewModel _messages;

        public ResultViewModel(IScenarioRunner runner, MessageViewModel messages)
        {
            _runner = runner;
            _messages = messages;
        }

        public void Analyze(int analyzerIndex) => _runner.Analyze(analyzerIndex);

        public void LoadLatestJournal()
        {
            _runner.LoadLatestJournalFile(out var message);
            _messages.ShowInfo(message);
        }

        public void Hide() => UpdateState(state => state.WithVisibility(false));

        protected override void OnInitialize()
        {
            _runner.ReportReady += OnReportReady;
            _runner.StateChanged += OnRunnerChanged;
            if (_runner.LastReport != null) OnReportReady(_runner.LastReport);
            else SetState(new ResultState(false, _runner.AnalyzerNames, null, null, null, 0, 0, null, null));
        }

        protected override void OnDispose()
        {
            _runner.ReportReady -= OnReportReady;
            _runner.StateChanged -= OnRunnerChanged;
        }

        private void OnRunnerChanged()
        {
            if (_runner.LastReport == null && State.IsVisible) Hide();
        }

        private void OnReportReady(AnalysisReport report)
        {
            var lines = report.Steps
                .Select(step => new ResultLine(step.Status, step.StepId,
                    _runner.TryGetInstruction(step.StepId, out var title) ? title : string.Empty,
                    step.Attempts, step.Violations, step.Points))
                .ToArray();

            SetState(new ResultState(true, _runner.AnalyzerNames, _runner.LastAnalyzerName, report.Mode, _runner.JournalSource,
                report.Score, report.MaxScore, lines, report.Warnings.ToArray()));
        }
    }
}
