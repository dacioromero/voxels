using UnityEngine;

// Adapted from http://paulbourke.net/geometry/polygonise/, retreived in April 2018

[System.Serializable]//, Unity.Burst.BurstCompile]
public struct Triangle
{
  public Vector3 v1;
  public Vector3 v2;
  public Vector3 v3;

  public Vector3[] Verts { get => new Vector3[] { v1, v2, v3 }; }

  public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
  {
    this.v1 = v1;
    this.v2 = v2;
    this.v3 = v3;
  }

  public bool Equals(Triangle t) => t.v1.Equals(v1) && t.v2.Equals(v2) && t.v3.Equals(v3);

  public static Triangle operator +(Triangle t, Vector3 o) => new Triangle(t.v1 + o, t.v2 + o, t.v2 + o);
}
