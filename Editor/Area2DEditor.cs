using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(Area2D))]
[CanEditMultipleObjects]
public class Area2DEditor : Editor {
    /// <summary> Index of currently dragged vertex. </summary>
	int draggedVert = -1;
	bool recordedDrag;
	public static bool constrainToParentArea = true;
    public static int gridIndex = 2;
    readonly static float[] gridSizes = new float[5] { 1, 0.5f, 0.1f, 0.05f, 0.01f };
    public static float gridSize { get { return gridSizes[gridIndex]; } }
	bool showVert;
    
    public static Texture editColliderIcon { get { return _editColliderIcon != null ? _editColliderIcon : _editColliderIcon = EditorGUIUtility.FindTexture("EditCollider"); } }
    public static Texture _editColliderIcon;
    public static bool editMode; 

    protected Area2D area2d;
    int i = 0;
    public void OnEnable() {
        area2d = target as Area2D;
        Tools.hidden = editMode;
        EditorApplication.update -= OnUpdate;
        EditorApplication.update += OnUpdate;
    }

    public void OnDisable() {
        Tools.hidden = false;
        EditorApplication.update -= OnUpdate;
    }

    public virtual void OnUpdate() {
        if (constrainToParentArea) {
            if (area2d.parentArea == null) return;
            area2d.transform.position = area2d.parentArea.LocalToWorld(area2d.parentArea.WorldToLocal(area2d.transform.position));
            area2d.transform.localRotation = Quaternion.identity;
        }
    }

    public override void OnInspectorGUI() {
        EditorGUI.BeginChangeCheck();
        editMode = InspectorEditButtonGUI(editMode);
        if (EditorGUI.EndChangeCheck()) {
            Tools.hidden = editMode;
            SceneView.RepaintAll();
        }
        EditorGUI.indentLevel = 0;



        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        OnDrawEditSettings();
        constrainToParentArea = EditorGUILayout.Toggle("Constrain to parent Area2D",constrainToParentArea);

        EditorGUI.indentLevel = 0;
        EditorGUILayout.LabelField("Read-Only info", EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;
        OnDrawReadonlyInfo();
	}

    public virtual void OnDrawEditSettings() {
        //Poly
        SerializedObject serializedObject = new UnityEditor.SerializedObject(area2d);
        SerializedProperty serializedLine = serializedObject.FindProperty("_poly");
        EditorGUILayout.PropertyField(serializedLine);
        serializedObject.ApplyModifiedProperties();
    }
    public virtual void OnDrawReadonlyInfo() {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Total area:");
        EditorGUILayout.SelectableLabel(area2d.poly.area.ToString());
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Direction:");
        EditorGUILayout.SelectableLabel(area2d.poly.isClockwise?"Clockwise":"Counter-Clockwise");
        EditorGUILayout.EndHorizontal();
    }

	public virtual void OnSceneGUI() {
        DrawPolygon(Color.green);
        if (editMode) EditVerts();
        //Handles.Label(Vector3.zero, "ASDF");
    }

    protected void EditVerts() {
        Event e = Event.current;
        Vector3 cursorPos = GetPlanePoint(area2d.plane);
        int controlID = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);

        Handles.color = Color.green;
        //If we are dragging a vertex
        if (draggedVert != -1) {
            Vector3 point = area2d.worldverts[draggedVert];
            Handles.DotCap(0, point, Camera.current.transform.rotation, HandleUtility.GetHandleSize(point) * 0.05f);
            switch (e.type) {
            case EventType.MouseDrag:
                //While we are dragging a vertex
                if (e.button != 0) break;
                if (!recordedDrag) {
                    Undo.RecordObject(area2d, "Area2D point move");
                    EditorUtility.SetDirty(area2d);
                    recordedDrag = true;
                }
                Vector2 localPos = area2d.WorldToLocal(cursorPos);
                Vector2[] verts = area2d.poly.verts;
                area2d.poly.verts[draggedVert] = e.control ? new Vector2(Mathf.Round(localPos.x / gridSize) * gridSize, Mathf.Round(localPos.y / gridSize) * gridSize) : new Vector2(localPos.x, localPos.y);
                area2d.poly = new Polygon2D(verts);

            break;
            case EventType.MouseUp:
                //When we release a vertex
                if (e.button != 0) break;
                draggedVert = -1;
                break; 
            case EventType.ScrollWheel:
                if (e.control) {
                    if (e.delta.y > 0) gridIndex++;
                    else if (e.delta.y < 0) gridIndex--;
                    gridIndex = Mathf.Clamp(gridIndex, 0, gridSizes.Length - 1);
                    e.Use();
                }
                break;
            case EventType.layout:
                HandleUtility.AddDefaultControl(controlID);
            break;
            case EventType.Repaint:
                if (draggedVert != -1) {
                    Vector2 vert = area2d.poly.verts[draggedVert];
                    string text = string.Format("    ({0}, {1})", vert.x, vert.y);
                    if (e.control) text += "\n    Snap: " + gridSize;
                    Handles.Label(area2d.worldverts[draggedVert], text);

                }
                break;
            }
        }
        //If we are not dragging a vertex 
        else {
            //Get hovered vertex
            int hoverVert = GetHoveredVertex(area2d);
            //If we are hovering a vertex
            if (hoverVert != -1) {
                Vector3 point = area2d.worldverts[hoverVert];
                Handles.DotCap(0, point, Camera.current.transform.rotation, HandleUtility.GetHandleSize(point) * 0.05f);
                switch (e.type) {
                    case EventType.MouseDown:
                        recordedDrag = false;
                        //When we click a vertex
                        if (e.button == 0) {
                            draggedVert = hoverVert;
                            e.Use();
                        }
                        break;
                    case EventType.MouseDrag:
                        recordedDrag = true;
                        break;
                    case EventType.MouseUp:
                        if (e.button == 1 && !recordedDrag) {
                            Undo.RecordObject(area2d, "Area2D point remove");
                            List<Vector2> verts = area2d.poly.verts.ToList();
                            verts.RemoveAt(hoverVert);
                            area2d.poly = new Polygon2D(verts.ToArray());
                        }
                        break;
                    case EventType.layout:
                        HandleUtility.AddDefaultControl(controlID);
                        break;
                }
            }
            //If we are not hovering a vertex
            else {
                int hoverEdge = GetHoveredEdge(area2d);
                //If we are hovering an edge
                if (hoverEdge != -1) {
                    Vector3 a, b;
                    GetEdge(hoverEdge, area2d, out a, out b);
                    Handles.DrawAAPolyLine(5f, a, b);
                    switch (e.type) {
                        case EventType.MouseDown:
                            //When we click a vertex
                            if (e.button != 0) break;
                            Undo.RecordObject(area2d, "Area2D point add");
                            Vector2 localPos = area2d.WorldToLocal(cursorPos);
                            List<Vector2> verts = area2d.poly.verts.ToList();
                            verts.Insert(hoverEdge + 1, localPos);
                            area2d.poly = new Polygon2D(verts.ToArray());
                            draggedVert = hoverEdge + 1;
                            break;
                        case EventType.layout:
                            HandleUtility.AddDefaultControl(controlID);
                            break;
                    }
                }
            }
        }
        SceneView.RepaintAll();
    }

    public virtual void DrawPolygon(Color col) {
        Handles.color = col;
        Vector3[] verts = area2d.worldverts;
        int[] tris = area2d.poly.triIndices;
		List<int[]> fixedTris = new List<int[]>();
		for (int i = 0; i < tris.Length; i += 3) {
			int t0 = tris[i];
			int t1 = tris[i+1];
			int t2 = tris[i+2];
			if (!fixedTris.Where(x => (x[0] == t0 && x[1] == t1) || (x[1] == t0 && x[0] == t1)).Any()) fixedTris.Add(new int[] { t0, t1 });
			if (!fixedTris.Where(x => (x[0] == t1 && x[1] == t2) || (x[1] == t1 && x[0] == t2)).Any()) fixedTris.Add(new int[] { t1, t2 });
			if (!fixedTris.Where(x => (x[0] == t2 && x[1] == t0) || (x[1] == t2 && x[0] == t0)).Any()) fixedTris.Add(new int[] { t2, t0 });
		}

		int[] newtris = new int[fixedTris.Count * 2];
		for (int i = 0; i < fixedTris.Count(); i++) {
			newtris[i * 2] = fixedTris[i][0];
			newtris[(i * 2) + 1] = fixedTris[i][1];
		}

		Handles.DrawLines(verts, newtris);
	}

	/// <summary> Gets the index of the vertex closest to the cursorPos </summary>
	protected int GetHoveredVertex(Area2D area2d) {
		int index = -1;
		float closest = Mathf.Infinity;
		for (int i = 0; i < area2d.poly.verts.Length; i++) {
			float dist = Vector2.Distance(Event.current.mousePosition, HandleUtility.WorldToGUIPoint(area2d.worldverts[i]));
			if (dist <= 20f && dist < closest) {
				closest = dist;
				index = i;
			}
		}
		return index;
	}

	/// <summary> Gets the index of the line closest to the cursorPos </summary>
	protected int GetHoveredEdge(Area2D area2d) {
		int index = -1;
		float closest = Mathf.Infinity;
		Vector3 cur, next;
		for (int i = 0; i < area2d.poly.verts.Length; i++) {
			GetEdge(i, area2d, out cur, out next);
			float dist = DistanceToLine(Event.current.mousePosition, HandleUtility.WorldToGUIPoint(cur), HandleUtility.WorldToGUIPoint(next));
			if (dist <= 20f && dist < closest) {
				closest = dist;
				index = i;
			}
		}
		return index;
	}

	protected void GetEdge(int index, Area2D area2d, out Vector3 a, out Vector3 b) {
		a = area2d.worldverts[index];
		if (index != area2d.poly.verts.Length - 1) b = area2d.worldverts[index + 1];
		else b = area2d.worldverts[0];
	}

	float DistanceToLine(Vector2 p, Vector2 a, Vector2 b) {
		Vector2 ab = b - a;
		Vector2 proj = Vector3.Project(p-a, ab);
		//Return if above length
		if (proj.magnitude > ab.magnitude) return Mathf.Infinity;

		Vector2 projb = b - (proj+a);
		if (projb.magnitude > ab.magnitude) return Mathf.Infinity;
		return Vector2.Distance(proj+a, p);
	}

	protected Vector3 GetPlanePoint(Plane plane) {
		float dist = 0f;
		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		if (plane.Raycast(ray, out dist)) {
			return ray.GetPoint(dist);
		} else return Vector3.zero;
	}

    public static bool InspectorEditButtonGUI(bool editing) {
        Rect controlRect = EditorGUILayout.GetControlRect(true, 22f, new GUILayoutOption[0]);
        Rect position = new Rect(controlRect.xMin + EditorGUIUtility.labelWidth, controlRect.yMin, 33f, 23f);
        GUIContent content = new GUIContent("Edit Collider");
        Vector2 vector = GUI.skin.label.CalcSize(content);
        Rect position2 = new Rect(position.xMax + 5f, controlRect.yMin + (controlRect.height - vector.y) * 0.5f, vector.x, controlRect.height);
        GUILayout.Space(2f);
        bool result = GUI.Toggle(position, editing, new GUIContent(EditorGUIUtility.IconContent("EditCollider").image,"Edit polygon vertices.\n\n - Hold Ctrl after clicking control handle to snap to grid.\n - Scroll while snapping to adjust grid size."), "Button");
        GUI.Label(position2, "Edit Polygon");
        return result;
    }
}
