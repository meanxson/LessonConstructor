using App.Code.Presentation.Runner;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;

namespace App.Code.Presentation.UI.Hud.ViewModels
{
    public sealed class ControlPanelViewModel : BaseWindowViewModel<ControlPanelState>
    {
        private readonly IScenarioRunner _runner;
        private readonly JournalViewModel _journal;

        public ControlPanelViewModel(IScenarioRunner runner, JournalViewModel journal)
        {
            _runner = runner;
            _journal = journal;
        }

        public void StartMode(int index) => _runner.StartMode(index);

        public void Restart() => _runner.Restart();

        public void ToggleJournal() => _journal.Toggle();

        protected override void OnInitialize()
        {
            _runner.StateChanged += Refresh;
            Refresh();
        }

        protected override void OnDispose() => _runner.StateChanged -= Refresh;

        private void Refresh() => SetState(new ControlPanelState(_runner.ModeNames, _runner.CurrentModeName));
    }
}
