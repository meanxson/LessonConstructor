using System.Linq;
using App.Code.Domain.Execution;
using App.Code.Presentation.Runner;
using UnityEditor;
using UnityEngine;

namespace App.Editor.Inspectors
{
    [CustomEditor(typeof(ScenarioRunner))]
    public sealed class ScenarioRunnerEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var runner = (ScenarioRunner)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Run", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to start modes and score journals from here.", MessageType.None);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                for (var index = 0; index < runner.Modes.Count; index++)
                    if (runner.Modes[index] != null && GUILayout.Button(runner.Modes[index].DisplayName))
                        runner.StartMode(index);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var analyzer in runner.Analyzers.Where(analyzer => analyzer != null))
                    if (GUILayout.Button("Score: " + analyzer.DisplayName))
                        runner.Analyze(analyzer);
            }

            var session = runner.Session;
            if (session == null) return;

            EditorGUILayout.LabelField("Group", $"{session.CurrentGroupIndex + 1}/{session.Plan.Groups.Count}, violations {session.CurrentGroupViolations}");
            foreach (var id in session.Plan.StepIds)
            {
                var status = session.GetStatus(id);
                EditorGUILayout.LabelField(id, $"{status}  attempt {session.GetAttempt(id)}  violations {session.GetViolations(id)}");
            }

            if (runner.LastReport != null)
                EditorGUILayout.HelpBox($"{runner.LastReport.AnalyzerName}: {runner.LastReport.Score} / {runner.LastReport.MaxScore}", MessageType.Info);
        }
    }
}
