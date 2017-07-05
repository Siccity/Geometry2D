using UnityEngine;
using System.Collections;


[System.Serializable]
public struct Area2DBounds {
	public Vector2 center;
	/// <summary> Size is always twice of the extents </summary>
	public Vector2 size;
	public float angle;
	/// <summary> Extents is always half of the size </summary>
	public Vector2 extents { get { return size * 0.5f; } set { size = value * 2f; } }

	/// <summary> Top, right, btm, left </summary>
	public Line2D[] GetWorldEdges() {
		Vector2 topLeft, topRight, btmLeft, btmRight;
		GetWorldCorners(out topLeft, out topRight, out btmLeft, out btmRight);
		return new Line2D[] {
			new Line2D(topLeft,topRight),
			new Line2D(topRight,btmRight),
			new Line2D(btmRight,btmLeft),
			new Line2D(btmLeft,topLeft),
		};
	}
	public void GetWorldCorners(out Vector2 topLeft, out Vector2 topRight, out Vector2 btmLeft, out Vector2 btmRight) {
		Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
		Vector3 extents1 = rot * new Vector3(extents.x, 0, extents.y);
		Vector3 extents2 = rot * new Vector3(extents.x, 0, -extents.y);
		topLeft = center + new Vector2(extents1.x,extents1.z);
		btmLeft = center + new Vector2(extents2.x, extents2.z);
		topRight = center + -new Vector2(extents2.x, extents2.z);
		btmRight = center + -new Vector2(extents1.x, extents1.z);
	}

	public void GetLocalCorners(out Vector2 topLeft, out Vector2 topRight, out Vector2 btmLeft, out Vector2 btmRight) {
		topLeft = new Vector2(-extents.x, extents.y);
		btmLeft = new Vector2(-extents.x, -extents.y);
		topRight = new Vector2(extents.x, extents.y);
		btmRight = new Vector2(extents.x, -extents.y);
	}

	public void GetWorldCorners(out Vector3 topLeft, out Vector3 topRight, out Vector3 btmLeft, out Vector3 btmRight, float height) {
		Vector2 a, b, c, d;
		GetWorldCorners(out a, out b, out c, out d);
		topLeft = new Vector3(a.x, height, a.y);
		topRight = new Vector3(b.x, height, b.y);
		btmLeft = new Vector3(c.x, height, c.y);
		btmRight = new Vector3(d.x, height, d.y);
	}

	public Polygon2D ToPoly() {
		Vector2 topLeft, topRight, btmLeft, btmRight;
		GetWorldCorners(out topLeft, out topRight, out btmLeft, out btmRight);
		return new Polygon2D(
			topLeft,
			topRight,
			btmRight,
			btmLeft
			);
    }
	public bool IntersectsWith (Area2DBounds other) {
		Vector2[] thiscorners = new Vector2[4];
		GetWorldCorners(out thiscorners[0], out thiscorners[1], out thiscorners[2], out thiscorners[3]);
		Vector2[] othercorners = new Vector2[4];
		other.GetWorldCorners(out othercorners[0], out othercorners[1], out othercorners[2], out othercorners[3]);

		Triangle2D thisTri0 = new Triangle2D(thiscorners[0], thiscorners[1], thiscorners[2]);
		Triangle2D thisTri1 = new Triangle2D(thiscorners[1], thiscorners[2], thiscorners[3]);
		// Return true if this area contains any point from other area
		for (int i = 0; i < 4; i++) {
			if (thisTri0.Contains(othercorners[i])) return true;
			if (thisTri1.Contains(othercorners[i])) return true;
		}
		Triangle2D otherTri0 = new Triangle2D(othercorners[0], othercorners[1], othercorners[2]);
		Triangle2D otherTri1 = new Triangle2D(othercorners[1], othercorners[2], othercorners[3]);
		// Return true if other area contains any point from this area
		for (int i = 0; i < 4; i++) {
			if (otherTri0.Contains(thiscorners[i])) return true;
			if (otherTri1.Contains(thiscorners[i])) return true;
		}
		// Return true if any edge intersects
		for (int i = 0; i < 4; i++) {
			for (int o = 0; o < 4; o++) {
				int t0 = i;
				int t1 = i == 3 ? 0 : i+1;
				int o0 = o;
				int o1 = o == 3 ? 0 : o+1;
				Line2D line0 = new Line2D(thiscorners[t0], thiscorners[t1]);
				Line2D line1 = new Line2D(othercorners[o0], othercorners[o1]);
				if (line0.SegmentsIntersect(line1)) {
					return true;
				}
			}
		}
		return false;
	}

	/// <summary> Constructor </summary>
	public Area2DBounds(Vector2 center, Vector2 size, float angle) {
		this.center = center;
		this.size = size;
		this.angle = angle;
	}

	public Area2DBounds(Area2DBounds original, Transform localTo) {
		Vector3 worldCenter = localTo.TransformPoint(new Vector3(original.center.x, 0f, original.center.y));
		this.center = new Vector2(worldCenter.x, worldCenter.z);
		this.size = original.size;
		this.angle = localTo.eulerAngles.y;
	}

	/// <summary> Does this area contain specified point </summary>
	public bool Contains(Vector2 point) {
		Vector3 centerWorld = new Vector3(center.x, 0, center.y);
		Vector3 pointWorld = new Vector3(point.x, 0, point.y);
		Quaternion areaRot = Quaternion.AngleAxis(angle, Vector3.up);
		Vector3 pointLocal = pointWorld - centerWorld;
		pointLocal = Quaternion.Inverse(areaRot) * pointLocal;
		bool x = pointLocal.x >= -extents.x && pointLocal.x <= extents.x;
		bool y = pointLocal.z >= -extents.y && pointLocal.z <= extents.y;
		return (x && y);
	}

	/// <summary> Check if this Area2DBounds is inside an Area2D. </summary>
	/// <param name="area2d"></param>
	/// <param name="collisionPadding">Used for collision distance threshold </param>
	/// <returns></returns>
	public bool IsInside(Area2D area2d, float tolerance) {
        if (area2d == null) {
            Debug.LogWarning("area2d is null");
            return true;
        }
		Area2DBounds paddedArea2DBounds = new Area2DBounds(center, new Vector2(size.x - tolerance, size.y - tolerance), angle);
		Vector2 topLeft, topRight, btmLeft, btmRight;
		paddedArea2DBounds.GetWorldCorners(out topLeft, out topRight, out btmLeft, out btmRight);
		//Check if any vertices are outside area2D
		if (!area2d.poly.Contains(topLeft) || !area2d.poly.Contains(topRight) || !area2d.poly.Contains(btmLeft) || !area2d.poly.Contains(btmRight)) return false;
		//Check if any lines intersect
		for (int i = 0; i < area2d.poly.verts.Length; i++) {
			Line2D line0 = new Line2D(
				area2d.poly.verts[i],
				(i == area2d.poly.verts.Length - 1) ? area2d.poly.verts[0] : area2d.poly.verts[i + 1]
				);
            //Vector2 intersection = Vector2.zero;
			if (line0.SegmentsIntersect(new Line2D(topLeft, topRight))) return false;
			if (line0.SegmentsIntersect(new Line2D(topRight, btmRight))) return false;
			if (line0.SegmentsIntersect(new Line2D(btmRight, btmLeft))) return false;
			if (line0.SegmentsIntersect(new Line2D(btmLeft, topLeft))) return false;
		}
		return true;
	}

	/// <summary> Returns a position that fits inside Area2D </summary>
	public Vector2 SnapToEdge(Area2D area2d, Vector2 targetPosition) {
		Line2D edge;
		Vector2 intersection;
		edge = area2d.poly.GetNearestEdge(targetPosition, out intersection);
		Vector2 norm = edge.Normal;

		Area2DBounds bounds = this;
		bounds.center = Vector2.zero;
		Vector2 topLeft, topRight, btmLeft, btmRight;
		bounds.GetWorldCorners(out topLeft, out topRight, out btmLeft, out btmRight);

		Vector2[] corners = new Vector2[] { topLeft, topRight, btmLeft, btmRight };

		//Directional components
		float[] directionalComponents = new float[corners.Length];
		for (int i = 0; i < corners.Length; i++) {
			//Component of a vector along a direction. Returns distance along a vector (i think)
			directionalComponents[i] = Geometry2D.DistanceAlongDirection(corners[i], norm);
		} 

		//Snap to edge
		Vector2 output = intersection + (norm * Mathf.Max(directionalComponents));

		Debug.DrawLine(area2d.LocalToWorld(output + corners[0]), area2d.LocalToWorld(output + corners[0]) + Vector3.up * 2);
        for (int i = 0; i < corners.Length; i++) {
            float t = edge.PositionAlongLine(output + corners[i]);
			if (t > 1f) output -= edge.ab * (t - 1f);
			else if (t < 0f) output -= edge.ab * t;
		}

#if UNITY_EDITOR
		//Debug stuff. Is drawn in the centre of the scene while colliding
		for (int i = 0; i < corners.Length; i++) {
			Debug.DrawLine(Vector3.zero, new Vector3(corners[i].x, 0, corners[i].y), Color.Lerp(Color.red, Color.blue, (1 + directionalComponents[i])) * 0.5f);
		}
		Debug.DrawLine(Vector3.zero, new Vector3(norm.x, 0, norm.y));
#endif

		return output;

	}

	public override string ToString() {
		return "Area2DBounds (center: " + center + " size: " + size + ")";
	}

	#region Editor
	public void DrawGizmo(float height = 0f) {
		Vector3 topLeft, btmLeft, topRight, btmRight;
		GetWorldCorners(out topLeft, out topRight, out btmLeft, out btmRight, height);
		Gizmos.DrawLine(topLeft, topRight);
		Gizmos.DrawLine(topRight, btmRight);
		Gizmos.DrawLine(btmRight, btmLeft);
		Gizmos.DrawLine(btmLeft, topLeft);
	}
	#endregion
}