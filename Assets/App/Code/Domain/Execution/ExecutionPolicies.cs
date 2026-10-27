namespace App.Code.Domain.Execution
{
    public enum ViolationResponse
    {
        Retry,
        Fail
    }

    public enum OutOfTurnBehaviour
    {
        RecordViolation,
        Ignore
    }

    public interface IExecutionPolicy
    {
        string Name { get; }

        ViolationResponse OnViolation(int attempt);

        bool IsGroupExhausted(int groupViolations);
    }

    public sealed class TrainingPolicy : IExecutionPolicy
    {
        public string Name => "training";

        public ViolationResponse OnViolation(int attempt) => ViolationResponse.Retry;

        public bool IsGroupExhausted(int groupViolations) => false;
    }

    public sealed class ExamPolicy : IExecutionPolicy
    {
        public const int DefaultViolationLimit = 2;

        public ExamPolicy(int violationLimit = DefaultViolationLimit)
        {
            ViolationLimit = violationLimit;
        }

        public int ViolationLimit { get; }

        public string Name => "exam";

        public ViolationResponse OnViolation(int attempt) => ViolationResponse.Fail;

        public bool IsGroupExhausted(int groupViolations) => ViolationLimit > 0 && groupViolations >= ViolationLimit;
    }
}
