using System.Linq;
using System.Text;
using App.Code.Domain.Analysis;
using App.Code.Domain.Execution;
using App.Code.Infrastructure.Journal;
using App.Code.Presentation.Analysis;
using App.Code.Presentation.Logging;
using UnityEditor;
using UnityEngine;

namespace App.Editor.Windows
{
    public sealed class JournalAnalyzerWindow : EditorWindow
    {
        private string _path = string.Empty;
        private AnalyzerAsset _analyzer;
        private JournalFile _journal;
        private string _report = string.Empty;
        private Vector2 _scroll;

        [MenuItem("Lesson Constructor/Journal Analyzer")]
        public static void Open() => GetWindow<JournalAnalyzerWindow>("Journal Analyzer");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Score a finished run from its .jsonl journal with any analyzer, without replaying the scenario.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.TextField("Journal", _path);
                if (GUILayout.Button("Browse", GUILayout.Width(70))) Browse();
                if (GUILayout.Button("Latest", GUILayout.Width(60))) PickLatest();
            }

            _analyzer = (AnalyzerAsset)EditorGUILayout.ObjectField("Analyzer", _analyzer, typeof(AnalyzerAsset), false);

            using (new EditorGUI.DisabledScope(_path.Length == 0 || _analyzer == null))
            {
                if (GUILayout.Button("Analyze")) Analyze();
            }

            if (GUILayout.Button("Open journals folder")) EditorUtility.RevealInFinder(JournalsDirectory());

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void Browse()
        {
            var picked = EditorUtility.OpenFilePanel("Scenario journal", JournalsDirectory(), "jsonl");
            if (!string.IsNullOrEmpty(picked)) _path = picked;
        }

        private void PickLatest()
        {
            _path = JsonLinesJournalReader.FindLatest(JournalsDirectory());
            if (_path.Length == 0) _report = $"No journals in {JournalsDirectory()}";
        }

        private void Analyze()
        {
            _journal = JsonLinesJournalReader.Read(_path);
            var report = _analyzer.Create().Analyze(_journal.Records);
            _report = Describe(report, _journal);
        }

        private static string Describe(AnalysisReport report, JournalFile journal)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"{report.AnalyzerName}  mode: {report.Mode}");
            builder.AppendLine($"Score: {report.Score} / {report.MaxScore}");
            builder.AppendLine($"Records: {journal.Records.Count}  Completed {report.Count(StepStatus.Completed)}  " +
                               $"Failed {report.Count(StepStatus.Failed)}  Skipped {report.Count(StepStatus.Skipped)}");
            builder.AppendLine();
            foreach (var step in report.Steps)
                builder.AppendLine($"{step.StepId,-10} {step.Status,-10} attempt {step.Attempts,2}  violations {step.Violations,2}  points {step.Points}");

            foreach (var warning in journal.Warnings.Concat(report.Warnings)) builder.AppendLine("WARNING: " + warning);
            return builder.ToString();
        }

        private static string JournalsDirectory()
        {
            var logger = AssetDatabase.FindAssets("t:" + nameof(JsonFileLoggerAsset))
                .Select(guid => AssetDatabase.LoadAssetAtPath<JsonFileLoggerAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(asset => asset != null);
            return logger != null ? logger.Directory : Application.persistentDataPath;
        }
    }
}
