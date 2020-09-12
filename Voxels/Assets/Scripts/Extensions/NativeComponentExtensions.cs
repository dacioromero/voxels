using Unity.Collections;

namespace NativeComponentExtensions
{
  public static class Extensions
  {
    public static void Enqueue<T>(this NativeQueue<T>.ParallelWriter q, T[] vals) where T : struct
    {
      foreach (T val in vals)
        q.Enqueue(val);
    }

    public static void Enqueue<T>(this NativeQueue<T> q, T[] vals) where T : struct => q.Enqueue(vals);
  }
}
