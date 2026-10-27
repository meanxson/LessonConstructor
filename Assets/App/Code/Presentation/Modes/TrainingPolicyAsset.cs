using App.Code.Domain.Execution;
using UnityEngine;

namespace App.Code.Presentation.Modes
{
    [CreateAssetMenu(menuName = "Lesson Constructor/Execution/Training Policy", fileName = "TrainingPolicy")]
    public sealed class TrainingPolicyAsset : ExecutionPolicyAsset
    {
        public override IExecutionPolicy Create() => new TrainingPolicy();
    }
}
