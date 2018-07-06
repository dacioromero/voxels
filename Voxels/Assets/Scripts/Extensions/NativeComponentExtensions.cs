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

        public static int[] Flatten(this int3[] int3s)
        {
            int[] ints = new int[int3s.Length * 3];

            for (int i = 0; i < int3s.Length; i++)
            {
                ints[i] = int3s[i].x;
                ints[i + 1] = int3s[i].y;
                ints[i + 2] = int3s[i].z;
            }

            return ints;
        }

        public static NativeArray<int> Flatten(this NativeArray<int3> int3s, Allocator a)
        {
            NativeArray<int> ints = new NativeArray<int>(int3s.Length * 3, a);

            for (int i = 0; i < int3s.Length; i++)
            {
                ints[i] = int3s[i].x;
                ints[i + 1] = int3s[i].y;
                ints[i + 2] = int3s[i].z;
            }

            return ints;
        }

        public static bool Contains<T>(this NativeList<T> nativeList, T obj, out int index) where T : struct
        {
            index = -1;

            for (int i = 0; i < nativeList.Length; i++)
            {
                if (nativeList[i].Equals(obj))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }
    }
}
