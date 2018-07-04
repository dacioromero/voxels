using System;
using Unity.Collections;
using Unity.Mathematics;

namespace NativeComponentExtensions
{
    public static class Extensions
    {
        public static T[] ToArray<T>(this NativeQueue<T> q) where T : struct
        {
            T[] a = new T[q.Count];

            for (int i = 0; i < a.Length; i++)
            {
                a[i] = q.Dequeue();
                q.Enqueue(a[i]);
            }

            return a;
        }

        public static NativeArray<T> ToNativeArray<T>(this NativeQueue<T> q, Allocator alloc) where T : struct
        {
            NativeArray<T> a = new NativeArray<T>(q.Count, alloc);

            for (int i = 0; i < a.Length; i++)
            {
                a[i] = q.Dequeue();
                q.Enqueue(a[i]);
            }

            return a;
        }

        public static bool Contains<T>(this NativeQueue<T> q, T val, out int index) where T : struct
        {
            index = Array.IndexOf(q.ToArray(), val);
            return index > -1;
        }

        public static void Enqueue<T>(this NativeQueue<T>.Concurrent q, T[] vals) where T : struct
        {
            foreach(T val in vals)
            {
                q.Enqueue(val);
            }
        }

        public static void Enqueue<T>(this NativeQueue<T> q, T[] vals) where T : struct
        {
            q.Enqueue(vals);
        }

        public static int[] Flatten(this int3[] int3s)
        {
            int[] ints = new int[int3s.Length * 3];

            for(int i = 0; i < int3s.Length; i++)
            {
                ints[i    ] = int3s[i].x;
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

            for(int i = 0; i < nativeList.Length; i++)
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