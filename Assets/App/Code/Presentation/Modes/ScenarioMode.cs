using System;
using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Presentation.Analysis;
using App.Code.Presentation.Steps;
using UnityEngine;

namespace App.Code.Presentation.Modes
{
    [Serializable]
    public sealed class StepEntry
    {
        [SerializeField] private Step step;
        [SerializeField] private StepActivation activation = StepActivation.AfterPrevious;

        public StepEntry(Step step, StepActivation activation)
        {
            this.step = step;
            this.activation = activation;
        }

        public Step Step => step;

        public StepActivation Activation => activation;
    }

    [Serializable]
    public sealed class StepGroupSettings
    {
        [SerializeField] private string name;
        [SerializeField] private List<StepEntry> steps = new List<StepEntry>();

        public StepGroupSettings(string name, IEnumerable<StepEntry> steps)
        {
            this.name = name;
            this.steps = steps.ToList();
        }

        public string Name => name;

        public IReadOnlyList<StepEntry> Steps => steps;
    }

    public sealed class ScenarioMode : MonoBehaviour
    {
        [SerializeField] private string displayName;
        [SerializeField] private ExecutionPolicyAsset policy;
        [SerializeField] private OutOfTurnBehaviour outOfTurn = OutOfTurnBehaviour.RecordViolation;
        [SerializeField] private AnalyzerAsset analyzer;
        [SerializeField] private List<StepGroupSettings> groups = new List<StepGroupSettings>();

        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;

        public ExecutionPolicyAsset Policy => policy;

        public OutOfTurnBehaviour OutOfTurn => outOfTurn;

        public AnalyzerAsset Analyzer => analyzer;

        public IReadOnlyList<StepGroupSettings> Groups => groups;

        public IEnumerable<Step> Steps => groups.SelectMany(group => group.Steps).Select(entry => entry.Step).Where(step => step != null);

        public ScenarioPlan BuildPlan() =>
            new ScenarioPlan(DisplayName, groups
                .Select(group => PlanGroup.FromSequence(group.Name, group.Steps
                    .Where(entry => entry.Step != null)
                    .Select(entry => new PlannedStep(entry.Step.Id, entry.Activation))))
                .ToList());

        public List<string> Validate()
        {
            var problems = new List<string>();
            if (policy == null) problems.Add($"Mode '{DisplayName}' has no execution policy.");
            if (analyzer == null) problems.Add($"Mode '{DisplayName}' has no analyzer: results will not be scored automatically.");
            if (!Steps.Any()) problems.Add($"Mode '{DisplayName}' contains no steps.");

            for (var index = 0; index < groups.Count; index++)
            {
                if (groups[index].Steps.Count == 0) problems.Add($"Group {index + 1} of mode '{DisplayName}' is empty.");
                if (groups[index].Steps.Any(entry => entry.Step == null)) problems.Add($"Group {index + 1} of mode '{DisplayName}' has an empty step slot.");
            }

            problems.AddRange(Steps.GroupBy(step => step.Id).Where(same => same.Count() > 1)
                .Select(same => $"Step id '{same.Key}' is used more than once in mode '{DisplayName}'."));
            problems.AddRange(Steps.Distinct().SelectMany(step => step.Validate()));
            return problems;
        }
    }
}
