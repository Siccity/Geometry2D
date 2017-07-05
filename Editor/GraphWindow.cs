using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Graphs;
using System;
public class GraphWindow : EditorWindow {
    private static int dragging = -1;
    private static int handleSize = 8;
    public static Vector2[] verts = new Vector2[] { new Vector2(100, 100), new Vector2(300, 100) };
    private static Vector2 scrollPos = new Vector2(0,0);
    private static bool panning;
    private float zoom = 1;

    [MenuItem("Examples/Curve Field demo")]
    static void Init() {
        EditorWindow window = GetWindow(typeof(GraphWindow));
        window.wantsMouseMove = true;
        window.position = new Rect(0, 0, 400, 199);
        window.Show();
    }

    void OnGUI() {
        GUI.color = Color.grey;
        GUI.Box(new Rect(0,0,position.width,position.height),"");
        GUI.color = Color.white;
        GUILayout.Label("Zoom: " + zoom + " ScrollPos: " + scrollPos);
        DrawGrid();

        Handles.color = Color.green;

        Handles.BeginGUI();

        if (Event.current.type == EventType.ScrollWheel) {
            zoom -= 0.05f * Event.current.delta.y;
            zoom = Mathf.Clamp(zoom, -0.5f, zoom);
            Repaint();
        }
        if (Event.current.type == EventType.MouseDrag) {
            if (Event.current.button == 2) {
                scrollPos -= Event.current.delta ;
                Repaint();
            }
        }

        if (Event.current.type == EventType.Repaint) {
            //If not dragging
            if (dragging == -1) {
                for (int i = 0; i < verts.Length; i++) {
                    DrawVertex(verts[i]);
                }
            }
            Handles.DrawLine(LocalizePosition(verts[0]), LocalizePosition(verts[1]));
        }
        Handles.EndGUI();
    }

    int GetHoveredVert(float minDist) {
        float closestDist = float.PositiveInfinity;
        int closest = -1;
        for (int i = 0; i < verts.Length; i++) {
            float dist = Vector2.Distance(verts[i], Event.current.mousePosition);
            if (dist < closestDist) {
                closestDist = dist;
                closest = i;
            }
        }
        if (closestDist < minDist) return closest;
        else return -1;
    }

    void DrawGrid() {
        Handles.color = new Color(0.3f, 0.3f, 0.3f);

        Vector2 localCenter = LocalizePosition(new Vector2(0, 0));
        //Draw X axis
        Handles.DrawLine(new Vector2(0, localCenter.y), new Vector3(position.width, localCenter.y));
        Handles.DrawLine(new Vector2(localCenter.x, 0), new Vector3(localCenter.x, position.height));
    }

    Vector2 LocalizePosition(Vector2 pos) {
        Vector2 original = pos;

        //Center the position
        pos += new Vector2(position.width * 0.5f, position.height * 0.5f);
        //Move position
        pos -= scrollPos;

        pos += (original - scrollPos) * zoom;
        return pos;

    }
    void DrawVertex(Vector2 pos) {
        pos = LocalizePosition(pos);
        Handles.DrawAAPolyLine(handleSize, pos - new Vector2(-handleSize * 0.5f, 0), pos - new Vector2(handleSize * 0.5f, 0));
    }
}