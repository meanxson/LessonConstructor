using System;
using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;

namespace App.Code.Domain.Analysis
{
    public interface IResultAnalyzer
    {
        string Name { get; }

        AnalysisReport Analyze(IReadOnlyList<LogRecord> records);
    }

    public sealed class StepScore
    {
        public StepScore(string stepId, StepStatus status, int attempts, int violations, double points)
        {
            StepId = stepId;
            Status = status;
            Attempts = attempts;
            Violations = violations;
            Points = points;
        }

        public string StepId { get; }

        public StepStatus Status { get; }

        public int Attempts { get; }

        public int Violations { get; }

        public double Points { get; }
    }

    public sealed class AnalysisReport
    {
        public AnalysisReport(string analyzerName, string mode, double score, double maxScore, IReadOnlyList<StepScore> steps, IReadOnlyList<string> warnings)
        {
            AnalyzerName = analyzerName;
            Mode = mode;
            Score = score;
            MaxScore = maxScore;
            Steps = steps;
            Warnings = warnings;
        }

        public string AnalyzerName { get; }

        public string Mode { get; }

        public double Score { get; }

        public double MaxScore { get; }

        public IReadOnlyList<StepScore> Steps { get; }

        public IReadOnlyList<string> Warnings { get; }

        public int Count(StepStatus status) => Steps.Count(step => step.Status == status);
    }

    public sealed class AttemptScoreAnalyzer : IResultAnalyzer
    {
        private readonly int _firstAttemptPoints;
        private readonly int _secondAttemptPoints;
        private readonly int _laterAttemptPoints;

        public AttemptScoreAnalyzer(int firstAttemptPoints = 10, int secondAttemptPoints = 5, int laterAttemptPoints = 0)
        {
            _firstAttemptPoints = firstAttemptPoints;
            _secondAttemptPoints = secondAttemptPoints;
            _laterAttemptPoints = laterAttemptPoints;
        }

        public string Name => "attempt_score";

        public AnalysisReport Analyze(IReadOnlyList<LogRecord> records)
        {
            var digest = JournalDigest.Build(records);
            var warnings = digest.Warnings.ToList();
            var scores = new List<StepScore>();
            var derivedAttempts = 0;

            foreach (var step in digest.Steps)
            {
                var attempts = step.FinalAttempt;
                if (step.FinalStatus == StepStatus.Completed && attempts <= 0)
                {
                    attempts = step.Violations - step.OutOfTurnViolations + 1;
                    derivedAttempts++;
                }

                var points = step.FinalStatus == StepStatus.Completed ? PointsFor(attempts) : 0;
                scores.Add(new StepScore(step.StepId, step.FinalStatus, attempts, step.Violations, points));
            }

            if (derivedAttempts > 0)
                warnings.Add($"{derivedAttempts} completed step(s) have no attempt number: attempts derived from violation count.");

            return new AnalysisReport(Name, digest.Mode, scores.Sum(score => score.Points), _firstAttemptPoints * scores.Count, scores, warnings);
        }

        private int PointsFor(int attempt) =>
            attempt == 1 ? _firstAttemptPoints : attempt == 2 ? _secondAttemptPoints : _laterAttemptPoints;
    }

    public sealed class PenaltyAnalyzer : IResultAnalyzer
    {
        private readonly int _baseScore;
        private readonly int _failedPenalty;
        private readonly int _skippedPenalty;

        public PenaltyAnalyzer(int baseScore = 100, int failedPenalty = 10, int skippedPenalty = 5)
        {
            _baseScore = baseScore;
            _failedPenalty = failedPenalty;
            _skippedPenalty = skippedPenalty;
        }

        public string Name => "penalty";

        public AnalysisReport Analyze(IReadOnlyList<LogRecord> records)
        {
            var digest = JournalDigest.Build(records);
            var warnings = digest.Warnings.ToList();
            var scores = new List<StepScore>();
            var unresolved = 0;

            foreach (var step in digest.Steps)
            {
                var status = step.FinalStatus;
                if (!status.IsTerminal())
                {
                    unresolved++;
                    status = StepStatus.Skipped;
                }

                var penalty = status == StepStatus.Failed ? _failedPenalty : status == StepStatus.Skipped ? _skippedPenalty : 0;
                scores.Add(new StepScore(step.StepId, status, step.FinalAttempt, step.Violations, -penalty));
            }

            if (unresolved > 0)
                warnings.Add($"{unresolved} step(s) have no final status: counted as Skipped.");

            var score = Math.Max(0, _baseScore + scores.Sum(step => step.Points));
            return new AnalysisReport(Name, digest.Mode, score, _baseScore, scores, warnings);
        }
    }
}
