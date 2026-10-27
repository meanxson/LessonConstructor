using System.Collections.Generic;
using App.Code.Presentation.Events;
using UnityEngine;

namespace App.Code.Presentation.Steps
{
    public sealed class ActionStep : Step
    {
        [SerializeField] private ScenarioEventSource action;

        public override IEnumerable<ScenarioEventSource> OwnSources
        {
            get { yield return action; }
        }

        protected override StepReaction ReactToOwn(ScenarioSignal signal) => StepReaction.Success;
    }
}
