using System;
using System.Collections.Generic;
using UnityEditor;
using Object = UnityEngine.Object;

namespace App.Editor.Setup
{
    public static class EditorAssign
    {
        public static void Set(Object target, params (string path, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var (path, value) in values) Write(Find(serialized, path), value);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        public static SerializedProperty Find(SerializedObject serialized, string path) =>
            serialized.FindProperty(path) ?? throw new ArgumentException($"{serialized.targetObject.GetType().Name} has no property '{path}'");

        public static void Write(SerializedProperty property, object value)
        {
            switch (value)
            {
                case null:
                    property.objectReferenceValue = null;
                    break;
                case Object reference:
                    property.objectReferenceValue = reference;
                    break;
                case string text:
                    property.stringValue = text;
                    break;
                case bool flag:
                    property.boolValue = flag;
                    break;
                case int number:
                    property.intValue = number;
                    break;
                case float number:
                    property.floatValue = number;
                    break;
                case Enum option:
                    property.enumValueIndex = Convert.ToInt32(option);
                    break;
                case IReadOnlyList<Object> references:
                    property.arraySize = references.Count;
                    for (var index = 0; index < references.Count; index++)
                        property.GetArrayElementAtIndex(index).objectReferenceValue = references[index];
                    break;
                default:
                    throw new ArgumentException($"Unsupported value type {value.GetType().Name} for '{property.propertyPath}'");
            }
        }
    }
}
