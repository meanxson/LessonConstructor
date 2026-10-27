using App.Code.Domain.Execution;
using UnityEngine;

namespace App.Code.Presentation.Modes
{
    [CreateAssetMenu(menuName = "Lesson Constructor/Execution/Exam Policy", fileName = "ExamPolicy")]
    public sealed class ExamPolicyAsset : ExecutionPolicyAsset
    {
        [SerializeField, Min(0)] private int violationLimit = ExamPolicy.DefaultViolationLimit;

        public int ViolationLimit => violationLimit;

        public override IExecutionPolicy Create() => new ExamPolicy(violationLimit);

        public override string Describe() => $"{name} (limit {violationLimit})";
    }
}
