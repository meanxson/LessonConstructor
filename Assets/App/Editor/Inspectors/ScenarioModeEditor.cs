using System;
using System.Linq;
using System.Text;
using App.Code.Presentation.Modes;
using UnityEditor;

namespace App.Editor.Inspectors
{
    [CustomEditor(typeof(ScenarioMode))]
    public sealed class ScenarioModeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Each group runs after the previous one. Inside a group, 'Together With Previous' activates the step with the one above it, " +
                "'After Previous' waits until every step above is finished. Drag rows to reorder.",
                MessageType.None);

            DrawDefaultInspector();

            var mode = (ScenarioMode)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Activation preview", EditorStyles.boldLabel);
            try
            {
                var plan = mode.BuildPlan();
                var builder = new StringBuilder();
                for (var index = 0; index < plan.Groups.Count; index++)
                {
                    var group = plan.Groups[index];
                    builder.Append(index + 1).Append(". ").Append(string.IsNullOrWhiteSpace(group.Name) ? "Group" : group.Name).Append(": ");
                    builder.Append(string.Join("  →  ", group.Batches.Select(batch => "[" + string.Join(", ", batch.StepIds) + "]")));
                    if (index < plan.Groups.Count - 1) builder.AppendLine();
                }

                EditorGUILayout.HelpBox(builder.ToString(), MessageType.Info);
            }
            catch (ArgumentException exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
            }

            foreach (var problem in mode.Validate()) EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }
    }
}
