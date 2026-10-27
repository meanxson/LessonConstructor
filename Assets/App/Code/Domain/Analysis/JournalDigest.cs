using System;
using System.Collections.Generic;
using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Domain.Journal;

namespace App.Code.Domain.Analysis
{
    public sealed class StepDigest
    {
        public StepDigest(string stepId)
        {
            StepId = stepId;
        }

        public string StepId { get; }

        public bool Registered { get; set; }

        public int Group { get; set; } = LogRecord.NoValue;

        public StepStatus FinalStatus { get; set; } = StepStatus.Pending;

        public int FinalAttempt { get; set; } = LogRecord.NoValue;

        public int Violations { get; set; }

        public int OutOfTurnViolations { get; set; }
    }

    public sealed class JournalDigest
    {
        private JournalDigest(string mode, bool started, bool finished, IReadOnlyList<StepDigest> steps, IReadOnlyList<string> warnings)
        {
            Mode = mode;
            Started = started;
            Finished = finished;
            Steps = steps;
            Warnings = warnings;
        }

        public string Mode { get; }

        public bool Started { get; }

        public bool Finished { get; }

        public IReadOnlyList<StepDigest> Steps { get; }

        public IReadOnlyList<string> Warnings { get; }

        public static JournalDigest Build(IReadOnlyList<LogRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));

            var warnings = new List<string>();
            var steps = new Dictionary<string, StepDigest>();
            var order = new List<StepDigest>();
            var mode = string.Empty;
            var started = false;
            var finished = false;

            StepDigest Step(string id)
            {
                if (steps.TryGetValue(id, out var digest)) return digest;
                digest = new StepDigest(id);
                steps.Add(id, digest);
                order.Add(digest);
                return digest;
            }

            foreach (var record in records)
            {
                switch (record.Type)
                {
                    case LogRecordTypes.SessionStarted:
                        started = true;
                        mode = record.Get(LogDataKeys.Mode);
                        break;
                    case LogRecordTypes.SessionFinished:
                        finished = true;
                        break;
                    case LogRecordTypes.StepRegistered when record.HasStep:
                        var registered = Step(record.StepId);
                        registered.Registered = true;
                        registered.Group = record.Group;
                        break;
                    case LogRecordTypes.StepViolation when record.HasStep:
                        var violated = Step(record.StepId);
                        violated.Violations++;
                        if (record.Reason == ViolationReasons.OutOfTurn) violated.OutOfTurnViolations++;
                        break;
                    case LogRecordTypes.StepCompleted when record.HasStep:
                        Finalize(Step(record.StepId), StepStatus.Completed, record);
                        break;
                    case LogRecordTypes.StepFailed when record.HasStep:
                        Finalize(Step(record.StepId), StepStatus.Failed, record);
                        break;
                    case LogRecordTypes.StepSkipped when record.HasStep:
                        Finalize(Step(record.StepId), StepStatus.Skipped, record);
                        break;
                    default:
                        if (record.HasStep) Step(record.StepId);
                        break;
                }
            }

            if (records.Count == 0) warnings.Add("Journal is empty: nothing to analyze.");
            else if (!started) warnings.Add("Journal has no session_started record: mode and settings are unknown.");
            if (records.Count > 0 && !finished) warnings.Add("Journal has no session_finished record: the run is incomplete, results are partial.");
            if (order.Count > 0 && !order.Any(step => step.Registered))
                warnings.Add("Journal has no step_registered records: the step list is derived from step events and may be incomplete.");

            return new JournalDigest(mode, started, finished, order, warnings);
        }

        private static void Finalize(StepDigest step, StepStatus status, LogRecord record)
        {
            step.FinalStatus = status;
            step.FinalAttempt = record.Attempt;
        }
    }
}
