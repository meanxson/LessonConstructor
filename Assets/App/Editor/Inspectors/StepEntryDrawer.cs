using App.Code.Presentation.Modes;
using UnityEditor;
using UnityEngine;

namespace App.Editor.Inspectors
{
    [CustomPropertyDrawer(typeof(StepEntry))]
    public sealed class StepEntryDrawer : PropertyDrawer
    {
        private const float ActivationWidth = 0.38f;
        private const float Gap = 4f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var activationWidth = position.width * ActivationWidth;
            var stepRect = new Rect(position.x, position.y, position.width - activationWidth - Gap, position.height);
            var activationRect = new Rect(stepRect.xMax + Gap, position.y, activationWidth, position.height);

            EditorGUI.PropertyField(stepRect, property.FindPropertyRelative("step"), GUIContent.none);
            EditorGUI.PropertyField(activationRect, property.FindPropertyRelative("activation"), GUIContent.none);
            EditorGUI.EndProperty();
        }
    }
}
