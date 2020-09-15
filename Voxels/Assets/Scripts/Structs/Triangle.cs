using Unity.Mathematics;

public struct Triangle
{
  public float3 v1;
  public float3 v2;
  public float3 v3;

  public Triangle(float3 v1, float3 v2, float3 v3)
  {
    this.v1 = v1;
    this.v2 = v2;
    this.v3 = v3;
  }
}
