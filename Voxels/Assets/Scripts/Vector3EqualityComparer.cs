using UnityEngine;
using System.Collections.Generic;

// Default Equals method is more accurate == operator is more lenient
public class Vector3EqualityComparer : IEqualityComparer<Vector3>
{
  public bool Equals(Vector3 x, Vector3 y) => x == y;

  // public int GetHashCode(Vector3 v) => v.GetHashCode();

  public int GetHashCode(Vector3 v)
  {
    var vInt = new Vector3Int(
      Mathf.RoundToInt(v.x),
      Mathf.RoundToInt(v.y),
      Mathf.RoundToInt(v.z)
    );

    return vInt.GetHashCode();
  }
}
