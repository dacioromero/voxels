using Unity.Collections;
using Unity.Mathematics;

namespace NativeComponentExtensions
{
  public static class Extensions
  {
    public static void Enqueue<T>(this NativeQueue<T>.Concurrent q, T[] vals) where T : struct
    {
      foreach (T val in vals)
        q.Enqueue(val);
    }

    public static void Enqueue<T>(this NativeQueue<T> q, T[] vals) where T : struct => q.Enqueue(vals);
  }
}
