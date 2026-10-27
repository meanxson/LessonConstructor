using System;
using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;

namespace App.Tests.Edit
{
    internal static class ScenarioTestKit
    {
        public static PlanGroup Group(string name, params (string id, StepActivation activation)[] steps) =>
            PlanGroup.FromSequence(name, steps.Select(step => new PlannedStep(step.id, step.activation)));

        public static (string, StepActivation) With(string id) => (id, StepActivation.TogetherWithPrevious);

        public static (string, StepActivation) After(string id) => (id, StepActivation.AfterPrevious);

        public static ScenarioSession Session(ScenarioJournal journal, IExecutionPolicy policy, params PlanGroup[] groups) =>
            Session(journal, policy, OutOfTurnBehaviour.RecordViolation, groups);

        public static ScenarioSession Session(ScenarioJournal journal, IExecutionPolicy policy, OutOfTurnBehaviour outOfTurn, params PlanGroup[] groups)
        {
            var time = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var session = new ScenarioSession(new ScenarioPlan("test", groups), policy, outOfTurn, journal, () => time = time.AddSeconds(1));
            session.Start();
            return session;
        }

        public static IEnumerable<string> Sorted(IEnumerable<string> ids) => ids.OrderBy(id => id, StringComparer.Ordinal);
    }
}
