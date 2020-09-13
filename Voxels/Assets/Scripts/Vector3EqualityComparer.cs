using UnityEngine;
using System.Collections.Generic;

// Default Equals method is more accurate == operator is more lenient
public class Vector3EqualityComparer : IEqualityComparer<Vector3>
{
  public bool Equals(Vector3 x, Vector3 y) => x == y;

  public int GetHashCode(Vector3 obj) => obj.GetHashCode();
}
