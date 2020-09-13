using Unity.Mathematics;

public struct Triangle
{
  public float3 vertex1;
  public float3 vertex2;
  public float3 vertex3;

  public float3[] vertices
  {
    get => new float3[] { vertex1, vertex2, vertex3 };
  }

  public Triangle(float3 vertex1, float3 vertex2, float3 vertex3)
  {
    this.vertex1 = vertex1;
    this.vertex2 = vertex2;
    this.vertex3 = vertex3;
  }
}
