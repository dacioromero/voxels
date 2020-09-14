using System.Runtime.InteropServices;
using Unity.Mathematics;

[StructLayout(LayoutKind.Sequential)]
public struct Triangle
{
  public float3 v1;
  public float3 v2;
  public float3 v3;

  public Triangle(float3 vertex1, float3 vertex2, float3 vertex3)
  {
    this.v1 = vertex1;
    this.v2 = vertex2;
    this.v3 = vertex3;
  }

  public static Triangle operator +(Triangle triangle, int3 offset)
  {
    return new Triangle
    (
      triangle.v1 + offset,
      triangle.v2 + offset,
      triangle.v3 + offset
    );
  }
}
