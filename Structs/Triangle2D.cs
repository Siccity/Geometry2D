using UnityEngine;
using System;

/// <summary> Immutable representation of a triangle defined by three points in 2D space </summary>
[Serializable]
public struct Triangle2D {

	public Vector2 a { get { return _a; } }
    [SerializeField] private Vector2 _a;
	public Vector2 b { get { return _b; } }
    [SerializeField] private Vector2 _b;
	public Vector2 c { get { return _c; } }
    [SerializeField] private Vector2 _c;
    /// <summary> Side length between b and c </summary>
    public float A { get { return _A; } }
    [SerializeField] private float _A;
    /// <summary> Side length between a and c </summary>
    public float B { get { return _B; } }
    [SerializeField] private float _B;
    /// <summary> Side length between a and b </summary>
    public float C { get { return _C; } }
    [SerializeField] private float _C;

    /// <summary> Constructor </summary>
    public Triangle2D (Vector2 a, Vector2 b, Vector2 c) {
		_a = a;
		_b = b;
		_c = c;
		_A = Vector2.Distance(b, c);
		_B = Vector2.Distance(a, c);
		_C = Vector2.Distance(a, c);
	}

	/// <summary> Returns the area of this triangle </summary>
	public float GetArea() {
		//Herons formula
		float p = (A + B + C) / 2f;
		return Mathf.Sqrt(p * (p - A) * (p - B) * (p - C));
	}

	/// <summary> Return true if point is inside triangle </summary>
	public bool Contains(Vector2 p) {
		bool b1, b2, b3;
		b1 = sign(p, a, b) < 0.0f;
		b2 = sign(p, b, c) < 0.0f;
		b3 = sign(p, c, a) < 0.0f;

		return ((b1 == b2) && (b2 == b3));
	}

	public float sign(Vector2 p1, Vector2 p2, Vector2 p3) {
		return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
	}

    public override string ToString() {
        return "Triangle2D[" + a + "," + b + "," + c + "]";
    }

    /// <summary> Return random point inside triangle </summary>
    public Vector2 GetRandomPoint() {
		float r1 = UnityEngine.Random.value;
		float r2 = UnityEngine.Random.value;
		return (1 - Mathf.Sqrt(r1)) * a + (Mathf.Sqrt(r1) * (1 - r2)) * b + (Mathf.Sqrt(r1) * r2) * c;
	}

}
