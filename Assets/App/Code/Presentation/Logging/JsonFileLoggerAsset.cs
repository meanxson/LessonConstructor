using System.IO;
using App.Code.Domain.Journal;
using App.Code.Infrastructure.Journal;
using UnityEngine;

namespace App.Code.Presentation.Logging
{
    [CreateAssetMenu(menuName = "Lesson Constructor/Logging/JSON Lines File Logger", fileName = "JsonFileLogger")]
    public sealed class JsonFileLoggerAsset : LoggerAsset
    {
        [SerializeField] private string folderName = "ScenarioJournals";

        public string Directory => Path.Combine(Application.persistentDataPath, folderName);

        public override IScenarioLogger Create(string runId) =>
            new JsonLinesFileLogger(Path.Combine(Directory, runId + JsonLinesJournalReader.Extension));
    }
}
