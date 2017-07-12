using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;
[AddComponentMenu("Geometry/Area2D")]
public class Area2D : MonoBehaviour {
    //To-do:
    public Area2D parentArea {
        get {
            if (lastParent != transform.parent) {
                _parentArea = transform.parent.GetComponentInParent<Area2D>();
                lastParent = transform.parent;
            }
            return _parentArea;
        }
        set {
            lastParent = parentArea.transform;
            _parentArea = value;
        }
    }
    private Area2D _parentArea;
    private Transform lastParent;
    public float rotation;
    public Vector2 position;

    public Polygon2D poly { get { return _poly; } set { _poly = value; CacheWorldVerts(); } }
    [SerializeField] private Polygon2D _poly = Polygon2D.Quad;

	/// <summary> Verts in world space. </summary>
	public Vector3[] worldverts { get {
            if (
                _worldverts == null || 
                _worldverts.Length != poly.verts.Length ||
                cachedTRS != Matrix4x4.TRS(transform.position,transform.rotation,transform.lossyScale))
                CacheWorldVerts();
            return _worldverts;
        }
    }
	[SerializeField] private Vector3[] _worldverts;
    private Matrix4x4 cachedTRS = new Matrix4x4();
	/// <summary> Get the plane that this Area2D exists in </summary>
	public Plane plane { get { return new Plane(transform.up, transform.position); } }

#if UNITY_EDITOR
    [ContextMenu("Save as Mesh...")]
    private void SaveAsMesh() {
        string path = UnityEditor.EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
        if (string.IsNullOrEmpty(path)) return;
        path = UnityEditor.FileUtil.GetProjectRelativePath(path);
        Mesh mesh = poly.ToMesh();
        UnityEditor.AssetDatabase.CreateAsset(mesh, path);
    }

    [ContextMenu("Load from Mesh...")]
    private void LoadFromMesh() {
        string path = UnityEditor.EditorUtility.OpenFilePanel("Save Separate Mesh Asset", "Assets/", "asset");
        if (string.IsNullOrEmpty(path)) return;
        path = UnityEditor.FileUtil.GetProjectRelativePath(path);
        Mesh mesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null) return;
        poly = new Polygon2D(Array.ConvertAll(mesh.vertices,x => new Vector2(x.x,x.z)));
    }

#endif


    #region Public methods


    /// <summary> Transform a local 2D position to 3D world </summary>
    public Vector3 LocalToWorld(Vector2 localPos) {
		Vector3 world = new Vector3(localPos.x, 0, localPos.y);
		return transform.TransformPoint(world);
	}

	/// <summary> Transform a world 3D position to 2D local </summary>
	public Vector2 WorldToLocal(Vector3 worldPos) {
		Vector3 local = transform.InverseTransformPoint(worldPos);
		return new Vector2(local.x, local.z);
	}

	/// <summary> Project an area2D onto this one, and return the poly </summary>
	public Polygon2D Project(Area2D area2d) {
		Vector2[] verts = new Vector2[area2d.poly.verts.Length];
		for (int i = 0; i < verts.Length; i++) {
			verts[i] = WorldToLocal(area2d.LocalToWorld(area2d.poly.verts[i]));
		}
		return new Polygon2D(verts);
	}

	/// <summary> Check if this Area2D completely contains another Area2D. </summary>
	/// <param name="area2d"></param>
	/// <param name="collisionPadding">Used for collision distance threshold </param>
	/// <returns></returns>
	public bool Contains(Area2D area2d, float tolerance) {
		Polygon2D other = Project(area2d);
		return poly.Contains(other);
	}

    /// <summary> Move this transform so that touches but does not intersect with edge </summary>
    /*public void SnapToEdge(Line2D edge) {
        Vector2 edgeNormal = edge.Normal;

        Vector2 intersection;

        Vector2 furthestVert;
        float furthestVertDist = float.PositiveInfinity;
        for (int i = 0; i < poly.verts; i++) {

        }
        edge.GetClosestPoint(;

        //Directional components
        float[] directionalComponents = new float[poly.verts.Length];
        for (int i = 0; i < poly.verts.Length; i++) {
            //Component of a vector along a direction. Returns distance along a vector (i think)
            directionalComponents[i] = Geometry2D.DistanceAlongDirection(poly.verts[i], edgeNormal);
        }

        //Snap to edge
        Vector2 output = intersection + (norm * Mathf.Max(directionalComponents));

        Debug.DrawLine(area2d.LocalToWorld(output + corners[0]), area2d.LocalToWorld(output + corners[0]) + Vector3.up * 2);
        for (int i = 0; i < corners.Length; i++) {
            float t = edge.PositionAlongLine(output + corners[i]);
            if (t > 1f) output -= edge.ab * (t - 1f);
            else if (t < 0f) output -= edge.ab * t;
        }
    }*/

    /// <summary> Returns true if raycast inside Area2D </summary>
    public bool Raycast(Ray ray, out float dist) {
		if (plane.Raycast(ray, out dist)) {
			Vector2 localPoint = WorldToLocal(ray.GetPoint(dist));
			return poly.Contains(localPoint);
		} else {
			return false;
		}
	}
	#endregion

	private void CacheWorldVerts() {
        cachedTRS = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		//Cache world verts
		_worldverts = new Vector3[poly.verts.Length];
		for (int i = 0; i < worldverts.Length; i++) {
			worldverts[i] = transform.TransformPoint(new Vector3(poly.verts[i].x, 0, poly.verts[i].y));
		}
	}
}

public static class Area2DExtensions {
    /// <summary> Returns the first Area2D that the ray passes through, if any. Otherwise returns closest Area2D </summary>
	public static Area2D GetBestMatch(this IList<Area2D> area2dArray, Ray ray) {
		bool directHit = false;
       
		float dist = 0;
		float closestDist = float.PositiveInfinity;
		Area2D closestArea2d = null;

		for (int i = 0; i < area2dArray.Count; i++) {
			if (area2dArray[i].plane.Raycast(ray, out dist)) {
				Vector2 localHitPoint = area2dArray[i].WorldToLocal(ray.GetPoint(dist));
				//If direct hit
				if (area2dArray[i].poly.Contains(localHitPoint)) {
					//If first direct hit
					if (!directHit) {
						directHit = true;
						closestDist = dist;
						closestArea2d = area2dArray[i];
					} else if (dist < closestDist) {
						closestDist = dist;
						closestArea2d = area2dArray[i];
					}
				}
				//If indirect hit (and havent had any direct hits yet)
				else if (!directHit) {
					Vector2 intersection;
					area2dArray[i].poly.GetNearestEdge(localHitPoint, out intersection);
					float distance = Vector2.Distance(localHitPoint, intersection);
					if (distance < closestDist) {
						closestDist = distance;
						closestArea2d = area2dArray[i];
					}
				}
			}
		}
		return closestArea2d;
	}
}