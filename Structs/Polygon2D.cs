using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary> Immutable representation of a polygon defined by an arbitrary number of points in 2D space </summary>
[Serializable]
public struct Polygon2D {
    /// <summary> Verts in local space. </summary>
    public Vector2[] verts { get { return _verts; } }
    [SerializeField] private Vector2[] _verts;
    /// <summary> Total polygon area. </summary>
    public float area { get { return _area; } }
    [SerializeField] private float _area;
    /// <summary> Triangle indices </summary>
    public int[] triIndices { get { return _triIndices; } }
    [SerializeField] private int[] _triIndices;
	/// <summary> Triangle count </summary>
	public int triCount { get { return _triCount; } }
    [SerializeField] private int _triCount;
	/// <summary> Area in square units for each triangle </summary>
	public Triangle2D[] tris { get { return _tris; } }
    [SerializeField] private Triangle2D[] _tris;
    /// <summary> Get the bounds of the polygon </summary>
    public Bounds2D bounds { get { return _bounds; } }
    [SerializeField] private Bounds2D _bounds;
    /// <summary> Get the bounds of the polygon </summary>
    public Line2D[] edges { get { return _edges; } }
    [SerializeField] private Line2D[] _edges;
    /// <summary> Returns true if polygon verts are in clockwise order </summary>
    public bool isClockwise { get { return _isClockwise; } }
    [SerializeField] private bool _isClockwise;

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
	/// <param name="segments">4 or more segments recommended. </param>
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
        if (verts.Length < 4) {
            Debug.LogWarning("You cannot create a polygon with less than 4 verts. Input count: " + verts.Length);
            verts = new Vector2[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, -1) };
        }
		_verts = verts;
		_triIndices = Triangulate(verts);
        float area = Geometry2D.ShoelaceFormula(_verts);
        _area = Mathf.Abs(area);
        _triCount = _triIndices.Length / 3;
        _bounds = new Bounds2D(verts);
        _edges = GetEdges(_verts);
        _tris = GetTriangles(_verts, _triIndices);
        _isClockwise = area < 0;
	}
	#endregion

	#region Public methods
    /// <summary> Convert Polygon2D to a mesh </summary>
    public Mesh ToMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = Array.ConvertAll(verts,x => new Vector3(x.x,0,x.y));
        mesh.triangles = triIndices;
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

	/// <summary> Returns a random local point inside the Area2D </summary>
	public Vector2 GetRandomPoint() {
		int tri = GetWeightedTriIndex();
		return tris[tri].GetRandomPoint();
		
	}

	/// <summary> Returns true if local point is inside this poly </summary>
	public bool Contains(Vector2 point) {
		for (int i = 0; i < triCount; i++) {
			if (tris[i].Contains(point)) return true;
		}
		return false;
	}

	/// <summary> Returns true if poly is completely inside this poly </summary>
	public bool Contains(Polygon2D poly) {
		//Check if poly contains all lines
		return (Contains(poly.edges));
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
        if (Contains(poly.edges[0].Center)) return true;
        if (poly.Contains(edges[0].Center)) return true;
        return (IntersectEdge(poly.edges));
	}

	/// <summary> Return true if any line intersects the edge of this polygon </summary>
	public bool IntersectEdge(params Line2D[] lines) {
		for (int i = 0; i < edges.Length; i++) {
			for (int k = 0; k < lines.Length; k++) {
				if (edges[i].SegmentsIntersect(lines[k])) return true;
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
    #endregion

    #region Overrides
    public override string ToString() {
        return "Polygon2D[" + string.Join(",", Array.ConvertAll(verts, x => x.ToString())) + "]";
    }

    public override bool Equals(System.Object obj) {
        return obj is Polygon2D && this == (Polygon2D)obj;
    }
    public override int GetHashCode() {
        int hash = 0;
        for (int i = 0; i < _verts.Length; i++) {
            hash ^= _verts[i].GetHashCode();
        }
        return hash;
    }

    public static bool operator ==(Polygon2D a, Polygon2D b) {
        if (a._verts.Length != b._verts.Length) return false;
        for (int i = 0; i < a._verts.Length; i++) {
            if (a._verts[i] != b._verts[i]) return false;
        }
        return true;
    }

    public static bool operator !=(Polygon2D a, Polygon2D b) {
        return !(a == b);
    }
    #endregion

    #region Private methods
    /// <summary> Get a random triangle based on relative size of triangles </summary>
    private int GetWeightedTriIndex() {
		float r = UnityEngine.Random.Range(0, this.area);
		float area = 0;
		for (int i = 0; i < tris.Length; i++) {
            area += tris[i].area;
			if (r <= area) return i;
		}
		return -1;
	}

    /// <summary> Returns all edges </summary>
    private static Line2D[] GetEdges(Vector2[] verts) {
        Line2D[] edges = new Line2D[verts.Length];
        for (int i = 0; i < edges.Length; i++) edges[i] = new Line2D(
            verts[i],
            i < verts.Length - 1 ? verts[i + 1] : verts[0]
            );
        return edges;
    }

    private static Triangle2D[] GetTriangles(Vector2[] verts, int[] triIndices) {
        Triangle2D[] tris = new Triangle2D[triIndices.Length / 3];
        for (int i = 0; i < tris.Length; i++) tris[i] = new Triangle2D(
            verts[triIndices[i * 3]],
            verts[triIndices[i * 3 + 1]],
            verts[triIndices[i * 3 + 2]]
            );
        return tris;
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
        for (int i = 0; i < edges.Length; i++) {
			Debug.DrawLine(
				new Vector3(edges[i].a.x,0,edges[i].a.y), 
				new Vector3(edges[i].b.x,0,edges[i].b.y), 
				col);
		}
	}
	#endregion
}