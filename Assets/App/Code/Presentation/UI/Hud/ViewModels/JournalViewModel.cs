using System.Linq;
using App.Code.Domain.Journal;
using App.Code.Presentation.Runner;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;

namespace App.Code.Presentation.UI.Hud.ViewModels
{
    public sealed class JournalViewModel : BaseWindowViewModel<JournalState>
    {
        private readonly IScenarioRunner _runner;

        public JournalViewModel(IScenarioRunner runner)
        {
            _runner = runner;
        }

        public void Toggle() => Publish(!State.IsVisible);

        protected override void OnInitialize()
        {
            _runner.RecordAdded += OnRecordAdded;
            _runner.StateChanged += Refresh;
        }

        protected override void OnDispose()
        {
            _runner.RecordAdded -= OnRecordAdded;
            _runner.StateChanged -= Refresh;
        }

        private void OnRecordAdded(LogRecord record) => Refresh();

        private void Refresh()
        {
            if (State.IsVisible) Publish(true);
        }

        private void Publish(bool isVisible) =>
            SetState(new JournalState(isVisible, _runner.JournalSource, isVisible ? _runner.Records.ToArray() : null));
    }
}
