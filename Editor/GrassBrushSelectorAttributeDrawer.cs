using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Linq;

namespace PFV.Grass.Editors
{

    [CustomPropertyDrawer(typeof(GrassBrushSelectorAttribute))]
    public class GrassBrushSelectorAttributeDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new Label("WTF");
        }


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            GrassProjectSettings settings = GrassProjectSettings.Get();
            if (settings == null || settings.grassDefinitions == null || settings.grassDefinitions.Count <= 0)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            else
            {
                int index = 0;
                if (property.objectReferenceValue != null)
                    index = settings.grassDefinitions.IndexOf(property.objectReferenceValue as GrassDefinition);
                EditorGUI.BeginChangeCheck();
                int newIndex = EditorGUI.Popup(position, label, index, settings.grassDefinitions.Where(d => d).Select(d => new GUIContent(d.name)).ToArray());
                if (EditorGUI.EndChangeCheck() && newIndex >= 0 && newIndex < settings.grassDefinitions.Count)
                {
                    property.objectReferenceValue = settings.grassDefinitions[newIndex];
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
            EditorGUI.EndProperty();
        }
    }
}