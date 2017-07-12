using UnityEngine;

public static class Geometry2D {

	/// <summary> Returns true if point is between lineA and lineB (AABB test) </summary>
	public static bool PointOnLineSegment(Vector2 lineA, Vector2 lineB, Vector2 point) {
		//AABB test
		if (point.x > Mathf.Max(lineA.x, lineB.x)) return false;
		if (point.x < Mathf.Min(lineA.x, lineB.x)) return false;
		if (point.y > Mathf.Max(lineA.y, lineB.y)) return false;
		if (point.y < Mathf.Min(lineA.y, lineB.y)) return false;
		return true;
	}

	public static Vector2 LineIntersection(Vector2 ps1, Vector2 pe1, Vector2 ps2, Vector2 pe2) {
		// Get A,B,C of first line - points : ps1 to pe1
		float A1 = pe1.y - ps1.y;
		float B1 = ps1.x - pe1.x;
		float C1 = A1 * ps1.x + B1 * ps1.y;

		// Get A,B,C of second line - points : ps2 to pe2
		float A2 = pe2.y - ps2.y;
		float B2 = ps2.x - pe2.x;
		float C2 = A2 * ps2.x + B2 * ps2.y;

		// Get delta and check if the lines are parallel
		float delta = A1 * B2 - A2 * B1;
		if (delta == 0) return Vector3.zero;

		// now return the Vector2 intersection point
		return new Vector2(
			(B2 * C1 - B1 * C2) / delta,
			(A1 * C2 - A2 * C1) / delta
		);
	}

	/// <summary> Component of a vector along a direction. Returns distance along a vector (i think) </summary>
	public static float DistanceAlongDirection(Vector2 a, Vector2 dir) {
		return Vector2.Dot(a, dir) / dir.magnitude;
	}

	public static float sign(Vector2 p1, Vector2 p2, Vector2 p3) {
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}

	/// <summary> Return a vector that is rotated 90 degrees </summary>
	public static Vector2 Rotate90(this Vector2 direction) {
		return new Vector2(direction.y, -direction.x);
	}

	/// <summary> Return a vector that is rotated -90 degrees </summary>
	public static Vector2 RotateNeg90(this Vector2 direction) {
		return new Vector2(-direction.y, direction.x);
	}

    /// <summary> Returns a rotated vector </summary>
    /// <param name="rad">Degrees in radians</param>
	public static Vector2 Rotate(this Vector2 v, float rad) {
		float sin = Mathf.Sin(rad);
		float cos = Mathf.Cos(rad);

		float tx = v.x;
		float ty = v.y;
		v.x = (cos * tx) - (sin * ty);
		v.y = (sin * tx) + (cos * ty);
		return v;
	}

	/// <summary> Returns angle in degress of a vector </summary>
	public static float Angle(this Vector2 v) {
		return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
	}

    /// <summary> 
    /// Gets area of a polygon using 'shoelace-formula' https://en.wikipedia.org/wiki/Shoelace_formula.
    /// Output is negative if verts are in counter-clockwise order.
    /// </summary>
    public static float ShoelaceFormula(params Vector2[] verts) {
        float sum = 0;
        for (int i = 0; i < verts.Length; i++) {
            if (i == verts.Length - 1) sum += (verts[0].x - verts[i].x) * (verts[0].y + verts[i].y);
            else sum += (verts[i + 1].x - verts[i].x) * (verts[i + 1].y + verts[i].y);
        }
        return sum * 0.5f;
    }
}
