using App.Code.Domain.Execution;
using UnityEngine;

namespace App.Code.Presentation.Modes
{
    public abstract class ExecutionPolicyAsset : ScriptableObject
    {
        public abstract IExecutionPolicy Create();

        public virtual string Describe() => name;
    }
}
