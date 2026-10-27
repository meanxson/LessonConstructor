using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Journal;
using App.Code.Presentation.Events;
using UnityEngine;

namespace App.Code.Presentation.Steps
{
    public sealed class SequenceStep : Step
    {
        [SerializeField] private List<ScenarioEventSource> sequence = new List<ScenarioEventSource>();
        [SerializeField] private string wrongOrderMessage;

        private int _next;

        public override IEnumerable<ScenarioEventSource> OwnSources => sequence;

        public override void ResetProgress() => _next = 0;

        public override IEnumerable<string> Validate()
        {
            foreach (var problem in base.Validate()) yield return problem;
            if (sequence.Count < 2) yield return $"Step '{Id}' should list at least two actions in its sequence.";
            if (sequence.Where(source => source != null).Distinct().Count() != sequence.Count(source => source != null))
                yield return $"Step '{Id}' repeats the same action in its sequence.";
        }

        protected override StepReaction ReactToOwn(ScenarioSignal signal)
        {
            var index = sequence.IndexOf(signal.Source);
            if (index < _next) return StepReaction.None;

            if (index > _next)
            {
                _next = 0;
                return StepReaction.Violation(ViolationReasons.WrongOrder, MessageOr(wrongOrderMessage, signal.Source.Label));
            }

            _next++;
            if (_next < sequence.Count) return StepReaction.Progress($"{signal.Source.Label} {_next}/{sequence.Count}");

            _next = 0;
            return StepReaction.Success;
        }
    }
}
