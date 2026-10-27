using System;
using System.Collections.Generic;
using System.Linq;

namespace App.Code.Domain.Execution
{
    public enum StepActivation
    {
        TogetherWithPrevious,
        AfterPrevious
    }

    public readonly struct PlannedStep
    {
        public PlannedStep(string stepId, StepActivation activation)
        {
            StepId = stepId;
            Activation = activation;
        }

        public string StepId { get; }

        public StepActivation Activation { get; }
    }

    public sealed class PlanBatch
    {
        public PlanBatch(IReadOnlyList<string> stepIds)
        {
            if (stepIds == null) throw new ArgumentNullException(nameof(stepIds));
            if (stepIds.Count == 0) throw new ArgumentException("Batch must contain at least one step", nameof(stepIds));
            if (stepIds.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Step id must not be empty", nameof(stepIds));
            StepIds = stepIds.ToArray();
        }

        public IReadOnlyList<string> StepIds { get; }
    }

    public sealed class PlanGroup
    {
        public PlanGroup(string name, IReadOnlyList<PlanBatch> batches)
        {
            if (batches == null) throw new ArgumentNullException(nameof(batches));
            Name = name ?? string.Empty;
            Batches = batches.ToArray();
        }

        public string Name { get; }

        public IReadOnlyList<PlanBatch> Batches { get; }

        public IEnumerable<string> StepIds => Batches.SelectMany(batch => batch.StepIds);

        public static PlanGroup FromSequence(string name, IEnumerable<PlannedStep> steps)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));

            var batches = new List<PlanBatch>();
            var current = new List<string>();
            foreach (var step in steps)
            {
                if (step.Activation == StepActivation.AfterPrevious && current.Count > 0)
                {
                    batches.Add(new PlanBatch(current));
                    current = new List<string>();
                }

                current.Add(step.StepId);
            }

            if (current.Count > 0) batches.Add(new PlanBatch(current));
            return new PlanGroup(name, batches);
        }
    }

    public sealed class ScenarioPlan
    {
        public ScenarioPlan(string name, IReadOnlyList<PlanGroup> groups)
        {
            if (groups == null) throw new ArgumentNullException(nameof(groups));

            Name = string.IsNullOrWhiteSpace(name) ? "Scenario" : name;
            Groups = groups.Where(group => group.Batches.Count > 0).ToArray();
            if (Groups.Count == 0) throw new ArgumentException("Plan contains no steps", nameof(groups));

            var seen = new HashSet<string>();
            foreach (var id in StepIds)
                if (!seen.Add(id))
                    throw new ArgumentException($"Step '{id}' appears in the plan more than once", nameof(groups));
        }

        public string Name { get; }

        public IReadOnlyList<PlanGroup> Groups { get; }

        public IEnumerable<string> StepIds => Groups.SelectMany(group => group.StepIds);
    }
}
