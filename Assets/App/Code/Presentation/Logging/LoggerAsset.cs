using App.Code.Domain.Journal;
using UnityEngine;

namespace App.Code.Presentation.Logging
{
    public abstract class LoggerAsset : ScriptableObject
    {
        public abstract IScenarioLogger Create(string runId);
    }
}
