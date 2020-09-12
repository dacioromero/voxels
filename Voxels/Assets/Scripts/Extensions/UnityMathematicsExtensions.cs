using System.Numerics;
using Unity.Mathematics;

namespace UnityMathematicsExtensions
{
  public static class Extensions
  {
    public static bool Equals(this float3 f1, float3 f2) => f1.x == f2.x && f1.y == f2.y && f1.z == f2.z;

    public static int GetMyHashCode(this float3 f)
    {
      unchecked
      {
        int hash = 67;

        hash = hash * 199 + math.round(f.y).GetHashCode();
        hash = hash * 199 + math.round(f.z).GetHashCode();
        hash = hash * 199 + math.round(f.x).GetHashCode();

        return hash;
      }
    }
  }
}
