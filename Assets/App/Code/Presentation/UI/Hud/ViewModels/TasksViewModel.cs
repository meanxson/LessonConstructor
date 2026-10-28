using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Presentation.Runner;
using App.Code.Presentation.UI.Core;
using App.Code.Presentation.UI.Hud.States;

namespace App.Code.Presentation.UI.Hud.ViewModels
{
    public sealed class TasksViewModel : BaseWindowViewModel<TasksState>
    {
        private readonly IScenarioRunner _runner;

        public TasksViewModel(IScenarioRunner runner)
        {
            _runner = runner;
        }

        protected override void OnInitialize()
        {
            _runner.StateChanged += Refresh;
            Refresh();
        }

        protected override void OnDispose() => _runner.StateChanged -= Refresh;

        private void Refresh()
        {
            var session = _runner.Session;
            if (session == null)
            {
                SetState(new TasksState(_runner.LastError, false, false, string.Empty, 0, 0, false, 0, 0, string.Empty, 0, 0, null));
                return;
            }

            var plan = session.Plan;
            var stepIds = plan.StepIds.ToArray();
            var limit = session.Policy is ExamPolicy exam ? exam.ViolationLimit : 0;
            var tasks = session.ActiveStepIds
                .Select(id => new ActiveTask(_runner.TryGetInstruction(id, out var title) ? title : id, session.GetAttempt(id)))
                .ToArray();

            SetState(new TasksState(
                _runner.LastError,
                session.IsRunning,
                session.IsFinished,
                _runner.CurrentModeName,
                stepIds.Count(id => session.GetStatus(id).IsTerminal()),
                stepIds.Length,
                plan.Groups.Count > 1 || limit > 0,
                session.CurrentGroupIndex + 1,
                plan.Groups.Count,
                session.CurrentGroup?.Name,
                session.CurrentGroupViolations,
                limit,
                tasks));
        }
    }
}
