using System.Collections.Generic;

namespace App.Code.Presentation.UI.Hud.States
{
    public readonly struct ActiveTask
    {
        public ActiveTask(string title, int attempt)
        {
            Title = title;
            Attempt = attempt;
        }

        public string Title { get; }

        public int Attempt { get; }
    }

    public sealed class TasksState
    {
        public TasksState()
        {
        }

        public TasksState(string error, bool isRunning, bool isFinished, string modeName, int done, int total, bool showGroup,
            int groupNumber, int groupCount, string groupName, int groupViolations, int violationLimit, IReadOnlyList<ActiveTask> tasks)
        {
            Error = error ?? string.Empty;
            IsRunning = isRunning;
            IsFinished = isFinished;
            ModeName = modeName ?? string.Empty;
            Done = done;
            Total = total;
            ShowGroup = showGroup;
            GroupNumber = groupNumber;
            GroupCount = groupCount;
            GroupName = groupName ?? string.Empty;
            GroupViolations = groupViolations;
            ViolationLimit = violationLimit;
            Tasks = tasks ?? new ActiveTask[0];
        }

        public string Error { get; } = string.Empty;

        public bool IsRunning { get; }

        public bool IsFinished { get; }

        public string ModeName { get; } = string.Empty;

        public int Done { get; }

        public int Total { get; }

        public bool ShowGroup { get; }

        public int GroupNumber { get; }

        public int GroupCount { get; }

        public string GroupName { get; } = string.Empty;

        public int GroupViolations { get; }

        public int ViolationLimit { get; }

        public IReadOnlyList<ActiveTask> Tasks { get; } = new ActiveTask[0];
    }
}
