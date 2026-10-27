using System;
using System.Collections.Generic;

namespace App.Code.Domain.Journal
{
    public sealed class LogRecord
    {
        public const int NoValue = -1;

        private static readonly IReadOnlyDictionary<string, string> EmptyData = new Dictionary<string, string>();

        public LogRecord(
            string type,
            string stepId = "",
            string outcome = "",
            int attempt = NoValue,
            int group = NoValue,
            string reason = "",
            IReadOnlyDictionary<string, string> data = null,
            int sequence = 0,
            double time = 0,
            string timestamp = "")
        {
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("Record type must not be empty", nameof(type));

            Type = type;
            StepId = stepId ?? string.Empty;
            Outcome = outcome ?? string.Empty;
            Attempt = attempt;
            Group = group;
            Reason = reason ?? string.Empty;
            Data = data ?? EmptyData;
            Sequence = sequence;
            Time = time;
            Timestamp = timestamp ?? string.Empty;
        }

        public int Sequence { get; }

        public double Time { get; }

        public string Timestamp { get; }

        public string Type { get; }

        public string StepId { get; }

        public string Outcome { get; }

        public int Attempt { get; }

        public int Group { get; }

        public string Reason { get; }

        public IReadOnlyDictionary<string, string> Data { get; }

        public bool HasStep => StepId.Length > 0;

        public string Get(string key) => Data.TryGetValue(key, out var value) ? value : string.Empty;

        public LogRecord Stamp(int sequence, double time, string timestamp) =>
            new LogRecord(Type, StepId, Outcome, Attempt, Group, Reason, Data, sequence, time, timestamp);
    }

    public static class LogRecordTypes
    {
        public const string SessionStarted = "session_started";
        public const string StepRegistered = "step_registered";
        public const string GroupStarted = "group_started";
        public const string StepActivated = "step_activated";
        public const string StepProgress = "step_progress";
        public const string StepViolation = "step_violation";
        public const string StepCompleted = "step_completed";
        public const string StepFailed = "step_failed";
        public const string StepSkipped = "step_skipped";
        public const string GroupFinished = "group_finished";
        public const string SessionFinished = "session_finished";
    }

    public static class LogOutcomes
    {
        public const string Completed = "Completed";
        public const string Failed = "Failed";
        public const string Skipped = "Skipped";
        public const string Violation = "Violation";
        public const string Retry = "Retry";
        public const string Aborted = "Aborted";
    }

    public static class LogDataKeys
    {
        public const string Mode = "mode";
        public const string Policy = "policy";
        public const string OutOfTurn = "out_of_turn";
        public const string GroupName = "group_name";
        public const string Batch = "batch";
        public const string StepGroup = "step_group";
        public const string Detail = "detail";
        public const string GroupViolations = "group_violations";
    }

    public static class ViolationReasons
    {
        public const string OutOfTurn = "out_of_turn";
        public const string WrongAction = "wrong_action";
        public const string WrongOrder = "wrong_order";
        public const string WrongState = "wrong_state";
    }
}
