using UnityEngine;
using System;

[Serializable]
public struct Bounds2D {

	public Vector2 center { get { return _center; } }
	[SerializeField] private Vector2 _center;
    /// <summary> Size is always twice the extents </summary>
	public Vector2 size { get { return _size; } }
	[SerializeField] private Vector2 _size;
    /// <summary> Extents is always half the size </summary>
	public Vector2 extents { get { return _extents; } }
    [SerializeField] private Vector2 _extents;

    /// <summary> Constructor. </summary>
    /// <param name="size">Size is always twice the extents </param>
	public Bounds2D(Vector2 center, Vector2 size) {
        if (size.x < 0) size.x = -size.x;
        if (size.y < 0) size.y = -size.y;
        _center = center;
		_size = size;
		_extents = size * 0.5f;
	}

    /// <summary> Constructor. </summary>
    /// <param name="verts">Verts contained by this Bounds2D</param>
	public Bounds2D (Vector2[] verts) {
		Vector2 max = verts[0];
		Vector2 min = verts[0];
		for (int i = 0; i < verts.Length; i++) {
			if (verts[i].x > max.x) max.x = verts[i].x;
			if (verts[i].y > max.y) max.y = verts[i].y;
			if (verts[i].x < min.x) min.x = verts[i].x;
			if (verts[i].y < min.y) min.x = verts[i].y;
		}
		_center = (min + max) * 0.5f;
		_size = new Vector2(max.x - min.x, max.y - min.y);
		_extents = _size * 0.5f;
	}

	/// <summary> Does another bounding box intersect with this bounding box? </summary>
	public bool Intersects(Bounds2D bounds) {
		Vector2 maxDelta = extents + bounds.extents;
		Vector2 delta = center - bounds.center;
		return (Mathf.Abs(delta.x) <= Mathf.Abs(maxDelta.x) && Mathf.Abs(delta.y) <= Mathf.Abs(maxDelta.y));
    }

	/// <summary> Does this bounding box contain given point? </summary>
	public bool Contains(Vector2 point) {
		point -= center;
		return Mathf.Abs(point.x) <= extents.x && Mathf.Abs(point.y) <= extents.y;
	}

#region Overrides
    public override string ToString() {
        return "Bounds2D[" + center +", " + size + "]";
    }

    public override bool Equals(System.Object obj) {
        return obj is Bounds2D && this == (Bounds2D)obj;
    }
    public override int GetHashCode() {
        return center.GetHashCode() ^ size.GetHashCode();
    }

    public static bool operator ==(Bounds2D a, Bounds2D b) {
        return (a.center == b.center) && (a.extents == b.extents);
    }

    public static bool operator !=(Bounds2D a, Bounds2D b) {
        return !(a == b);
    }
#endregion
}
