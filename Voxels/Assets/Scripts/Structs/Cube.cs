using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace MarchingCubes
{
  public struct Cube
  {
    public static readonly Cube one = new Cube(int3.zero, 1, 1, 1, 1, 1, 1, 1, 1);
    public static readonly Cube zero = new Cube(int3.zero, 0, 0, 0, 0, 0, 0, 0, 0);

    [DllImport("marching_cubes")]
    unsafe static extern int polygonise(void* values, float isovalue, void* triangles);

    public float value1;
    public float value2;
    public float value3;
    public float value4;
    public float value5;
    public float value6;
    public float value7;
    public float value8;
    public int3 offset;

    public Cube(int3 offset, float value1, float value2, float value3, float value4, float value5, float value6, float value7, float value8)
    {
      this.value1 = value1;
      this.value2 = value2;
      this.value3 = value3;
      this.value4 = value4;
      this.value5 = value5;
      this.value6 = value6;
      this.value7 = value7;
      this.value8 = value8;
      this.offset = offset;
    }

    public bool Equals(Cube g)
    {
      return (
        g.value1 == value1 &&
        g.value2 == value2 &&
        g.value3 == value3 &&
        g.value4 == value4 &&
        g.value5 == value5 &&
        g.value6 == value6 &&
        g.value7 == value7 &&
        g.value8 == value8
      );
    }

    unsafe public int Polygonise(float isolevel, NativeArray<Triangle> triangles)
    {
      var values = new NativeArray<float>(8, Allocator.Temp);

      values[0] = value1;
      values[1] = value2;
      values[2] = value3;
      values[3] = value4;
      values[4] = value5;
      values[5] = value6;
      values[6] = value7;
      values[7] = value8;

      int ntriang = polygonise(values.GetUnsafePtr(), isolevel, triangles.GetUnsafePtr());

      for (int i = 0; i < ntriang; i++)
      {
        triangles[i] += offset;
      }

      values.Dispose();

      return ntriang;
    }
  }
}
