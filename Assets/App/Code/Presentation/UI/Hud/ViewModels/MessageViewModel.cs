using App.Code.Domain.Execution;
using App.Code.Presentation.Runner;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;

namespace App.Code.Presentation.UI.Hud.ViewModels
{
    public sealed class MessageViewModel : BaseWindowViewModel<MessageState>
    {
        private readonly IScenarioRunner _runner;

        public MessageViewModel(IScenarioRunner runner)
        {
            _runner = runner;
        }

        public void ShowInfo(string text) => Show(MessageKind.Info, string.Empty, text, ViolationResponse.Retry);

        protected override void OnInitialize() => _runner.ViolationRaised += OnViolation;

        protected override void OnDispose() => _runner.ViolationRaised -= OnViolation;

        private void OnViolation(ViolationEvent violation)
        {
            var kind = violation.Notice.OutOfTurn ? MessageKind.OutOfTurn : MessageKind.Violation;
            Show(kind, violation.StepTitle, violation.Message, violation.Notice.Response);
        }

        private void Show(MessageKind kind, string title, string text, ViolationResponse response) =>
            UpdateState(state => new MessageState(state.Sequence + 1, kind, title, text, response));
    }
}
