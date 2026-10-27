using App.Code.Domain.Journal;
using UnityEngine;

namespace App.Code.Presentation.Logging
{
    [CreateAssetMenu(menuName = "Lesson Constructor/Logging/Console Logger", fileName = "ConsoleLogger")]
    public sealed class ConsoleLoggerAsset : LoggerAsset
    {
        public override IScenarioLogger Create(string runId) => new UnityConsoleLogger(runId);

        private sealed class UnityConsoleLogger : IScenarioLogger
        {
            private readonly string _runId;

            public UnityConsoleLogger(string runId)
            {
                _runId = runId;
            }

            public void Write(LogRecord record) => Debug.Log($"[ScenarioJournal] {_runId} {JournalFormatter.Line(record)}");

            public void Dispose()
            {
            }
        }
    }
}
