using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary> Immutable representation of a polygon defined by an arbitrary number of points in 2D space </summary>
[Serializable]
public struct Polygon2D {
    /// <summary> Verts in local space. </summary>
    public Vector2[] verts { get { return _verts; } }
    [SerializeField] private Vector2[] _verts;
	/// <summary> Triangle indices </summary>
	public int[] tris { get { return _tris; } }
    [SerializeField] private int[] _tris;
	/// <summary> Triangle count </summary>
	public int triCount { get { return _triCount; } }
    [SerializeField] private int _triCount;
	/// <summary> Area in square units for each triangle </summary>
	public float[] triArea { get { return _triArea; } }
    [SerializeField] private float[] _triArea;
    /// <summary> Get the bounds of the polygon </summary>
    public Bounds2D bounds { get { return _bounds; } }
    [SerializeField] private Bounds2D _bounds;

    #region Constructors
    /// <summary> Returns a centered 2x2 quad </summary>
    public static Polygon2D Quad { get { return new Polygon2D(new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1)); } }

	/// <summary> Returns a centered quad with set height/width </summary>
	public Polygon2D Rect(float width, float height) {
		float halfWidth = width * 0.5f;
		float halfHeight = height * 0.5f;
		return new Polygon2D(new Vector2(-halfWidth, halfHeight), new Vector2(halfWidth, halfHeight), new Vector2(halfWidth, -halfHeight), new Vector2(-halfWidth, -halfHeight));
	}

	/// <summary> Constructs and returns a centered, circular Polygon2D with a set number of segments. </summary>
	/// <param name="segments">3 or more segments recommended. </param>
	public Polygon2D Circle(int segments) {
		Vector2[] verts = new Vector2[segments];
		for (int i = 0; i < segments; i++) {
			float rad = ((float)i / segments) * Mathf.PI * 2;
			verts[i] = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
		}
		return new Polygon2D(verts);
	}

	/// <summary> Constructor </summary>
	public Polygon2D(params Vector2[] verts) {
        if (verts.Length < 3) {
            Debug.LogWarning("You cannot create a polygon with less than 3 verts. Input count: " + verts.Length);
            verts = new Vector2[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1) };
        }
		_verts = verts;
		_tris = Triangulate(verts);
		_triArea = CacheTriArea(verts, _tris);
        _triCount = _tris.Length / 3;
        _bounds = new Bounds2D(verts);
	}
	#endregion

	#region Public methods
	/// <summary> Returns the total area of the Area2D in square units </summary>
	public float GetTotalArea() {
		float area = 0f;
		for (int i = 0; i<triArea.Length; i++) {
			area += triArea[i];
		}
		return area;
	}

    /// <summary> Convert Polygon2D to a mesh </summary>
    public Mesh ToMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = Array.ConvertAll(verts,x => new Vector3(x.x,0,x.y));
        mesh.triangles = tris;
        Vector2[] uvs = new Vector2[verts.Length];
        Array.Copy(verts, uvs, verts.Length);
        for (int i = 0; i < uvs.Length; i++) {
            uvs[i] -= (bounds.center + bounds.extents);
            uvs[i].x /= bounds.size.x;
            uvs[i].y /= bounds.size.y;
        }
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

	public Triangle2D GetTriangle(int index) {
		return new Triangle2D(
			verts[tris[index * 3]],
			verts[tris[index * 3 + 1]],
			verts[tris[index * 3 + 2]]
			);
	}

	/// <summary> Returns a random local point inside the Area2D </summary>
	public Vector2 GetRandomPoint() {
		int tri = GetWeightedTriIndex();
		Triangle2D triangle = GetTriangle(tri);
		return triangle.GetRandomPoint();
		
	}

	/// <summary> Returns true if local point is inside this poly </summary>
	public bool Contains(Vector2 point) {
		for (int i = 0; i < triCount; i++) {
			if (GetTriangle(i).Contains(point)) return true;
		}
		return false;
	}

	/// <summary> Returns true if poly is completely inside this poly </summary>
	public bool Contains(Polygon2D poly) {
		//Check if poly contains all lines
		Line2D[] polyLines = poly.GetEdges();
		return (Contains(polyLines));
	}

	/// <summary> Returns true if poly is completely inside this poly </summary>
	public bool Contains(params Line2D[] lines) {
		if (lines.Length == 0) return true;
		if (!Contains(lines[0].a)) return false;
        //Possible optimization: Check for contains a/b instead of check intersect?
		return (!IntersectEdge(lines));
	}

	/// <summary> Returns true if polys intersect eachother</summary>
	public bool Intersect(Polygon2D poly) {
        //Possible optimization: bounds check?
        //Start by performing a quick bounds test
        //if (!bounds.Intersects(poly.bounds)) return false;
        //Test against all edges
		Line2D[] polyEdges = poly.GetEdges();
        if (Contains(polyEdges[0].Center)) return true;
        if (poly.Contains(GetEdge(0).Center)) return true;
        return (IntersectEdge(polyEdges));
	}

	/// <summary> Return true if any line intersects the edge of this polygon </summary>
	public bool IntersectEdge(params Line2D[] lines) {
        //Possible optimization: Chache edges
		Line2D[] polyEdges = GetEdges();
		for (int i = 0; i < polyEdges.Length; i++) {
			for (int k = 0; k < lines.Length; k++) {
				if (polyEdges[i].SegmentsIntersect(lines[k])) return true;
			}
		}
		return false;
	}

	public Line2D GetNearestEdge(Vector2 point, out Vector2 intersection) {
		Line2D output = new Line2D();
		intersection = Vector3.zero;
		float dist = Mathf.Infinity;
		for (int i = 0; i < verts.Length; i++) {

            Line2D line = new Line2D(
                verts[i],
                (i == verts.Length - 1) ? verts[0] : verts[i + 1]);
			Vector2 newIntersection = line.GetClosestPointSegment(point);
            float newDist = Vector2.Distance(point, newIntersection);
			if (newDist < dist) {
				dist = newDist;
                output = line;
                intersection = newIntersection;
			}
		}
		return output;
	}

	/// <summary> Returns a Polygon2D with its verts reversed </summary>
	public Polygon2D Flip() {
		Vector2[] verts = new Vector2[this.verts.Length];
		for (int i = 0; i < verts.Length; i++) {
			verts[i] = this.verts[(verts.Length-1) - i];
		}
		return new Polygon2D(verts);
	}

    /// <summary> Returns edge by index </summary>
    public Line2D GetEdge(int index) {
        return new Line2D(
            verts[index],
            index < verts.Length - 1 ? verts[index + 1] : verts[0]
            );
    }

	/// <summary> Returns all edges </summary>
	public Line2D[] GetEdges() {
		Line2D[] edges = new Line2D[verts.Length];
		for (int i = 0; i < verts.Length; i++) edges[i] = GetEdge(i);
		return edges;
	}

    public override string ToString() {
        return "Polygon2D["+string.Join(",", Array.ConvertAll(verts, x => x.ToString()))+"]";
    }
    #endregion

    #region Private methods
    /// <summary> Get a random triangle based on relative size of triangles </summary>
    private int GetWeightedTriIndex() {
		float totalArea = GetTotalArea();
		float r = UnityEngine.Random.Range(0, totalArea);
		float area = 0;
		for (int i = 0; i < triArea.Length; i++) {
			area += triArea[i];
			if (r <= area) return i;
		}
		return -1;
	}
	#endregion

	#region Cache
	/// <summary> Calculate area for each triangle </summary>
	private static float[] CacheTriArea(Vector2[] verts, int[] tris) {
		int triCount = tris.Length / 3;
		float[] triAreas = new float[triCount];
		for (int i = 0; i < triAreas.Length; i++) {
			Triangle2D tri = new Triangle2D(
				verts[tris[i * 3]],
				verts[tris[i * 3 + 1]],
				verts[tris[i * 3 + 2]]
			);
			triAreas[i] = tri.GetArea();
		}
		return triAreas;
	}
	#endregion

	#region Triangulator
	private static int[] Triangulate(Vector2[] verts) {
		List<int> indices = new List<int>();

		int n = verts.Length;
		if (n < 3)
			return indices.ToArray();

		int[] V = new int[n];
		if (Area(verts) > 0) {
			for (int v = 0; v < n; v++)
				V[v] = v;
		} else {
			for (int v = 0; v < n; v++)
				V[v] = (n - 1) - v;
		}

		int nv = n;
		int count = 2 * nv;
		for (int m = 0, v = nv - 1; nv > 2;) {
			if ((count--) <= 0)
				return indices.ToArray();

			int u = v;
			if (nv <= u)
				u = 0;
			v = u + 1;
			if (nv <= v)
				v = 0;
			int w = v + 1;
			if (nv <= w)
				w = 0;

			if (Snip(verts, u, v, w, nv, V)) {
				int a, b, c, s, t;
				a = V[u];
				b = V[v];
				c = V[w];
				indices.Add(a);
				indices.Add(b);
				indices.Add(c);
				m++;
				for (s = v, t = v + 1; t < nv; s++, t++)
					V[s] = V[t];
				nv--;
				count = 2 * nv;
			}
		}

		indices.Reverse();
		return indices.ToArray();
	}

	private static float Area(Vector2[] verts) {
		int n = verts.Length;
		float A = 0.0f;
		for (int p = n - 1, q = 0; q < n; p = q++) {
			Vector2 pval = verts[p];
			Vector2 qval = verts[q];
			A += pval.x * qval.y - qval.x * pval.y;
		}
		return (A * 0.5f);
	}

	private static bool Snip(Vector2[] verts, int u, int v, int w, int n, int[] V) {
		int p;
		Triangle2D tri = new Triangle2D( verts[V[u]], verts[V[v]], verts[V[w]]);
		if (Mathf.Epsilon > (((tri.b.x - tri.a.x) * (tri.c.y - tri.a.y)) - ((tri.b.y - tri.a.y) * (tri.c.x - tri.a.x))))
			return false;
		for (p = 0; p < n; p++) {
			if ((p == u) || (p == v) || (p == w))
				continue;
			Vector2 P = verts[V[p]];
			if (tri.Contains(P))
				return false;
		}
		return true;
	}

	#endregion

	#region Editor
	public void DebugDraw(Color col) {
		Line2D[] edges = GetEdges();
        for (int i = 0; i < edges.Length; i++) {
			Debug.DrawLine(
				new Vector3(edges[i].a.x,0,edges[i].a.y), 
				new Vector3(edges[i].b.x,0,edges[i].b.y), 
				col);
		}
	}
	#endregion
}