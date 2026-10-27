namespace App.Code.Domain.Execution
{
    public enum StepStatus
    {
        Pending,
        Active,
        Completed,
        Failed,
        Skipped
    }

    public static class StepStatusExtensions
    {
        public static bool IsTerminal(this StepStatus status) =>
            status == StepStatus.Completed || status == StepStatus.Failed || status == StepStatus.Skipped;
    }
}
