using System.Collections.Generic;
using App.Code.Domain.Execution;

namespace App.Code.Presentation.UI.Hud.States
{
    public readonly struct ResultLine
    {
        public ResultLine(StepStatus status, string stepId, string title, int attempts, int violations, double points)
        {
            Status = status;
            StepId = stepId;
            Title = title;
            Attempts = attempts;
            Violations = violations;
            Points = points;
        }

        public StepStatus Status { get; }

        public string StepId { get; }

        public string Title { get; }

        public int Attempts { get; }

        public int Violations { get; }

        public double Points { get; }
    }

    public sealed class ResultState
    {
        public ResultState()
        {
        }

        public ResultState(bool isVisible, IReadOnlyList<string> analyzerNames, string analyzerName, string mode, string source,
            double score, double maxScore, IReadOnlyList<ResultLine> lines, IReadOnlyList<string> warnings)
        {
            IsVisible = isVisible;
            AnalyzerNames = analyzerNames ?? new string[0];
            AnalyzerName = analyzerName ?? string.Empty;
            Mode = mode ?? string.Empty;
            Source = source ?? string.Empty;
            Score = score;
            MaxScore = maxScore;
            Lines = lines ?? new ResultLine[0];
            Warnings = warnings ?? new string[0];
        }

        public bool IsVisible { get; }

        public IReadOnlyList<string> AnalyzerNames { get; } = new string[0];

        public string AnalyzerName { get; } = string.Empty;

        public string Mode { get; } = string.Empty;

        public string Source { get; } = string.Empty;

        public double Score { get; }

        public double MaxScore { get; }

        public IReadOnlyList<ResultLine> Lines { get; } = new ResultLine[0];

        public IReadOnlyList<string> Warnings { get; } = new string[0];

        public ResultState WithVisibility(bool isVisible) =>
            new ResultState(isVisible, AnalyzerNames, AnalyzerName, Mode, Source, Score, MaxScore, Lines, Warnings);
    }
}
