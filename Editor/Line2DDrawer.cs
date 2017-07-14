using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Line2D))]
public class Line2DDrawer : PropertyDrawer {

    // Draw the property inside the given rect
    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        Rect rect_a = new Rect(position.x, position.y, position.width, position.height);
        Rect rect_b = new Rect(position.x, position.y+18, position.width, position.height);
        EditorGUI.PropertyField(rect_a, property.FindPropertyRelative("_a"), new GUIContent());
        EditorGUI.PropertyField(rect_b, property.FindPropertyRelative("_b"), new GUIContent());
        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return base.GetPropertyHeight(property, label) + 18;
    }
}