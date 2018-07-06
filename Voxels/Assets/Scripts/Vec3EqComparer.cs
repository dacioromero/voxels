using UnityEngine;
using System.Collections.Generic;

// Default Equals method is more accurate == operator is more lenient
public class Vec3EqComparer : IEqualityComparer<Vector3>
{
    static public Vec3EqComparer c = new Vec3EqComparer();

    public bool Equals(Vector3 x, Vector3 y) => x == y;

    public int GetHashCode(Vector3 obj) => obj.GetHashCode();
}