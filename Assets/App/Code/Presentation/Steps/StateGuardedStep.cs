using System.Collections.Generic;
using App.Code.Domain.Journal;
using App.Code.Presentation.Events;
using App.Code.Presentation.State;
using UnityEngine;

namespace App.Code.Presentation.Steps
{
    public sealed class StateGuardedStep : Step
    {
        [SerializeField] private ScenarioEventSource action;
        [SerializeField] private StateFlag requiredState;
        [SerializeField] private bool requiredValue = true;
        [SerializeField] private string wrongStateMessage;

        public override IEnumerable<ScenarioEventSource> OwnSources
        {
            get { yield return action; }
        }

        public override IEnumerable<string> Validate()
        {
            foreach (var problem in base.Validate()) yield return problem;
            if (requiredState == null) yield return $"Step '{Id}' has no required state object assigned.";
        }

        protected override StepReaction ReactToOwn(ScenarioSignal signal)
        {
            if (requiredState == null || requiredState.Value == requiredValue) return StepReaction.Success;
            return StepReaction.Violation(ViolationReasons.WrongState,
                MessageOr(wrongStateMessage, $"{requiredState.Label} != {requiredValue}"));
        }
    }
}
