using UnityEngine;
using System;

/// <summary> Immutable representation of a line defined by two points in 2D space </summary>
[Serializable]
public struct Line2D {

	public Vector2 a { get { return _a; } }
    [SerializeField] private Vector2 _a;
	public Vector2 b { get { return _b; } }
    [SerializeField] private Vector2 _b;
    /// <summary> ab = b-a (cached) </summary>
    public Vector2 ab { get { return _ab; } }
    [SerializeField] private Vector2 _ab;
    /// <summary> ab magnitude (cached) </summary>
    public float length { get { return _length; } }
    [SerializeField] private float _length;
    /// <summary> Get the bounds of the line segment between a and b </summary>
    public Bounds2D bounds {  get { return _bounds; } }
    [SerializeField] private Bounds2D _bounds;
    /// <summary> Calculate normalized ab </summary>
    public Vector2 Direction { get { return ab.normalized; } }
	/// <summary> Calculate normalized vector perpendicular to ab </summary>
	public Vector2 Normal { get { return ab.Rotate90().normalized; } }
	/// <summary> Calculate the center between a and b </summary>
	public Vector2 Center { get { return (a + b) * 0.5f; } }
    
    /// <summary> Constructor </summary>
	public Line2D(Vector2 a, Vector2 b) {
		_a = a;
		_b = b;
		_ab = b - a;
		_length = _ab.magnitude;
        _bounds = new Bounds2D((a + b) * 0.5f, _ab);
	}

    #region Rotating
    /// <summary> Rotates this line around world center </summary>
    /// <param name="rad">Angle in radians</param>
    public Line2D Rotate(float rad) { return new Line2D(a.Rotate(rad), b.Rotate(rad)); }

    /// <summary> Rotates this line around specified pivot </summary>
    /// <param name="rad">Angle in radians</param>
    public Line2D Rotate(float rad, Vector2 pivot) { return new Line2D((a - pivot).Rotate(rad) + pivot, (b - pivot).Rotate(rad) + pivot); }

    /// <summary> Rotates this line 90 degrees around world center </summary>
    public Line2D Rotate90() { return new Line2D(a.Rotate90(), b.Rotate90()); }

    /// <summary> Rotates this line 90 degrees around pivot </summary>
    public Line2D Rotate90(Vector2 pivot) { return new Line2D((a - pivot).Rotate90() + pivot, (b - pivot).Rotate90() + pivot);}

    /// <summary> Rotates this line -90 degrees around world center </summary>
    public Line2D RotateNeg90() { return new Line2D(a.RotateNeg90(), b.RotateNeg90()); }

    /// <summary> Rotates this line -90 degrees around world center </summary>
    public Line2D RotateNeg90(Vector2 pivot) { return new Line2D((a - pivot).RotateNeg90() + pivot, (b - pivot).RotateNeg90() + pivot); }

    /// <summary> Inverts the line. Practically the same as rotating 180 degrees </summary>
    public Line2D Flip() { return new Line2D(-a, -b); }

    /// <summary> Inverts the line. Practically the same as rotating 180 degrees </summary>
    public Line2D Flip(Vector2 pivot) { return new Line2D((-(a-pivot)+pivot), (-(b - pivot) + pivot)); }
    #endregion

    /// <summary> Returns a new Line2D which has offset a and b </summary>
    public Line2D Translate(Vector2 offset) {
		return new Line2D(a + offset, b + offset);
	}

    /// <summary> Returns a new Line2D which has been cropped in both ends. </summary>
    public Line2D Crop(float startDist,float endDist) {
        return new Line2D(LerpDistance(startDist), LerpDistance(length - endDist));
    }


    /// <summary> Returns a point between a (0) and b (1). Unclamped. </summary>
    public Vector2 Lerp(float t) {
		return a + (ab * t);
	}

    /// <summary> Returns a point between a (0) and b (1). Clamped between 0 and 1. </summary>
	public Vector2 LerpClamped(float t) {
        return Lerp(Mathf.Clamp01(t));
    }

    /// <summary> Returns point between a (0) and b (length). Unclamped. </summary>
    public Vector2 LerpDistance(float d) {
        return a + (ab.normalized * d);
    }

    /// <summary> Returns point between a (0) and b (length). Clamped between a and b. </summary>
    public Vector2 LerpDistanceClamped(float d) {
        return a + (ab.normalized * d);
    }

    /// <summary> Project a point onto this line </summary>
	public Vector2 Project(Vector2 p) {
        Vector2 a = ab.normalized;
        p.Normalize();
        return Vector2.Dot(a, p) * p;
    }

    /// <summary> Returns a value where 0 = line start and 1 = line end. Unclamped.</summary>
    public float PositionAlongLine(Vector2 p) {
        Vector2 otherAB = ab.Rotate90();
        float denominator = (ab.y * otherAB.x - ab.x * otherAB.y);
        float t1 =
            ((a.x - p.x) * otherAB.y + (p.y - a.y) * otherAB.x)
                / denominator;
        return t1;
    }

    /// <summary> Return a point on the line closest to p </summary>
    public Vector2 GetClosestPoint(Vector2 p) {
        return a + (ab * PositionAlongLine(p));
    }

    /// <summary> Return a point on the line segment closest to p </summary>
    public Vector2 GetClosestPointSegment(Vector2 p) {
        return a + (ab * Mathf.Clamp01(PositionAlongLine(p)));
    }

    /// <summary> Returns the distance between p and the closest point on the line segment </summary>
    public float DistanceFromPointSegment(Vector2 p) {
		return Vector2.Distance(GetClosestPointSegment(p), p);
	}
    
    /// <summary> Perform an intersection test between two lines and return result </summary>
    public bool Intersect(Line2D other, out Line2DIntersection intersection) {

        float denominator = (ab.y * other.ab.x - ab.x * other.ab.y);

        float t1 =
            ((a.x - other.a.x) * other.ab.y + (other.a.y - a.y) * other.ab.x)
                / denominator;
        if (float.IsInfinity(t1))
        {
            // The lines are parallel (or close enough to it).
            intersection = new Line2DIntersection(false, Vector2.zero, Vector2.zero, Vector2.zero,0,0);
            return false;
        }

        float t2 =
            ((other.a.x - a.x) * ab.y + (a.y - other.a.y) * ab.x)
                / -denominator;

        // Find the point of intersection.
        Vector2 i_point = new Vector2(a.x + ab.x * t1, a.y + ab.y * t1);

        // The segments intersect if t1 and t2 are between 0 and 1.
        bool segments_intersect = 
            ((t1 >= 0) && (t1 <= 1) &&
             (t2 >= 0) && (t2 <= 1));

        float t1cached = t1;
        float t2cached = t2;

        // Find the closest points on the segments.
        if (t1 < 0) t1 = 0;
        else if (t1 > 1) t1 = 1;
        if (t2 < 0) t2 = 0;
        else if (t2 > 1) t2 = 1;

        Vector2 close_p1 = a + (ab * t1);
        Vector2 close_p2 = other.a + (other.ab * t2);
        intersection = new Line2DIntersection(segments_intersect, i_point, close_p1, close_p2, t1cached, t2cached);
        return true;
    }

    /// <summary> Perform an intersection test between a line and a point. The intersection point lies in the point nearest to the line </summary>
    public bool Intersect(Vector2 point, out Line2DIntersection intersection) {
        Line2D other = new Line2D(point, point + Normal);
        return Intersect(other, out intersection);
    }

    /// <summary> Returns true if line segments intersect </summary>
    public bool SegmentsIntersect(Line2D other) {
        Line2DIntersection intersection;
        if (Intersect(other, out intersection)) {
            if (intersection.segments_intersect) return true;
        }
        return false;
    }

	public void DebugDraw(Color color) {
#if UNITY_EDITOR
		Debug.DrawLine(new Vector3(a.x, 0, a.y), new Vector3(b.x, 0, b.y), color);
		Debug.Log(new Vector3(a.x, 0, a.y)+ " "+ new Vector3(b.x, 0, b.y));
#endif
	}

    #region Overloads
    public override string ToString() {
		return "Line2D["+a+","+b+"]";
	}

    public override bool Equals(System.Object obj) {
        return obj is Line2D && this == (Line2D)obj;
    }
    public override int GetHashCode() {
        return a.GetHashCode() ^ b.GetHashCode();
    }

    public static bool operator ==(Line2D a, Line2D b) {
        return (a.a == b.a) && (a.b == b.b);
    }

    public static bool operator !=(Line2D a, Line2D b) {
        return !(a == b);
    }
    #endregion
}

public struct Line2DIntersection {
    /// <summary> True if the lines intersect between a and b </summary>
    public bool segments_intersect { get { return _segments_intersect; } }
    private bool _segments_intersect;
    /// <summary> The point where the lines intersect </summary>
    public Vector2 point { get { return _point; } }
    private Vector2 _point;
    /// <summary> Return the point on the first segment that is closest to the point of intersection </summary>
    public Vector2 closestSeg1 { get { return _closestSeg1; } }
    private Vector2 _closestSeg1;
    /// <summary> Return the point on the second segment that is closest to the point of intersection </summary>
    public Vector2 closestSeg2 { get { return _closestSeg2; } }
    private Vector2 _closestSeg2;
    /// <summary> T value at the intersection on line A </summary>
    public float t1 { get { return _t1; } }
    private float _t1;
    /// <summary> T value at the intersection on line B </summary>
    public float t2 { get { return _t2; } }
    private float _t2;
    /// <summary> Constructor </summary>
    public Line2DIntersection (bool segments_intersect, Vector2 point, Vector2 closestSeg1, Vector2 closestSeg2, float t1, float t2) {
        _segments_intersect = segments_intersect;
        _point = point;
        _closestSeg1 = closestSeg1;
        _closestSeg2 = closestSeg2;
        _t1 = t1;
        _t2 = t2;
    }
}