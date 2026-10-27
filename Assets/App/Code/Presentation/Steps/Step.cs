using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Journal;
using App.Code.Presentation.Events;
using App.Code.Presentation.State;
using UnityEngine;

namespace App.Code.Presentation.Steps
{
    public enum StepReactionKind
    {
        None,
        Progress,
        Success,
        Violation
    }

    public readonly struct StepReaction
    {
        private StepReaction(StepReactionKind kind, string reason, string message)
        {
            Kind = kind;
            Reason = reason ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public StepReactionKind Kind { get; }

        public string Reason { get; }

        public string Message { get; }

        public static StepReaction None => new StepReaction(StepReactionKind.None, null, null);

        public static StepReaction Success => new StepReaction(StepReactionKind.Success, null, null);

        public static StepReaction Progress(string detail) => new StepReaction(StepReactionKind.Progress, null, detail);

        public static StepReaction Violation(string reason, string message) => new StepReaction(StepReactionKind.Violation, reason, message);
    }

    public abstract class Step : MonoBehaviour, IScenarioResettable
    {
        [SerializeField] private string stepId;
        [SerializeField, TextArea(2, 4)] private string instruction;
        [SerializeField] private List<ScenarioEventSource> violationSources = new List<ScenarioEventSource>();
        [SerializeField] private string violationMessage;

        public string Id => string.IsNullOrWhiteSpace(stepId) ? gameObject.name : stepId.Trim();

        public string Instruction => string.IsNullOrWhiteSpace(instruction) ? Id : instruction;

        public abstract IEnumerable<ScenarioEventSource> OwnSources { get; }

        public IEnumerable<ScenarioEventSource> Sources =>
            OwnSources.Concat(violationSources).Where(source => source != null).Distinct();

        public bool Owns(ScenarioEventSource source) => source != null && OwnSources.Contains(source);

        public StepReaction React(ScenarioSignal signal)
        {
            if (Owns(signal.Source)) return ReactToOwn(signal);
            if (violationSources.Contains(signal.Source))
                return StepReaction.Violation(ViolationReasons.WrongAction, MessageOr(violationMessage, signal.Source.Label));
            return StepReaction.None;
        }

        public virtual void ResetProgress()
        {
        }

        public void ResetState() => ResetProgress();

        public virtual IEnumerable<string> Validate()
        {
            if (!OwnSources.Any(source => source != null)) yield return $"Step '{Id}' has no action source assigned.";
            if (violationSources.Any(source => source == null)) yield return $"Step '{Id}' has an empty violation source slot.";
        }

        protected abstract StepReaction ReactToOwn(ScenarioSignal signal);

        protected static string MessageOr(string message, string fallback) =>
            string.IsNullOrWhiteSpace(message) ? fallback : message;
    }
}
