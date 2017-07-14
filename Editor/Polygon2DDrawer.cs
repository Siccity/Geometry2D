using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(Polygon2D))]
public class Polygon2DDrawer : PropertyDrawer {

    public static bool vertsFoldout = false;

    // Draw the property inside the given rect
    public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);


        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        ArrayGUI(property, "_verts", label);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return 0;
        //return base.GetPropertyHeight(property, label) + 18;
    }

    void ArrayGUI(SerializedProperty obj, string name, GUIContent label) {
        vertsFoldout = EditorGUILayout.Foldout(vertsFoldout, label);

        if (vertsFoldout) {
            EditorGUI.indentLevel++;
            int size = obj.FindPropertyRelative(name + ".Array.size").intValue;

            int newSize = EditorGUILayout.DelayedIntField("Size", size);
            if (newSize < 4) newSize = 4;

            Vector2[] verts = new Vector2[newSize];

            if (newSize != size)
                obj.FindPropertyRelative(name + ".Array.size").intValue = newSize;

            for (int i = 0; i < newSize; i++) {
                verts[i] = obj.FindPropertyRelative(string.Format("{0}.Array.data[{1}]", name, i)).vector2Value;
            }

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < newSize; i++) {
                verts[i] = EditorGUILayout.Vector2Field("Vert " + i, verts[i]);
            }
            if (EditorGUI.EndChangeCheck() || size != newSize) {
                Area2D area2d = obj.serializedObject.targetObject as Area2D;
                area2d.poly = new Polygon2D(verts);
                SceneView.RepaintAll();
            }
        }
    }
}