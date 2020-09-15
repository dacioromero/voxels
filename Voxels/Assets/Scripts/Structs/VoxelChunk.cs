using System.Collections.Generic;
using System.Linq;
using MarchingCubes;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct VoxelChunk
{
  Vector3Int dimensions;
  float isoLevel;
  float[,,] voxels;

  public VoxelChunk(Vector3Int dimensions, float[,] noiseMap, float isolevel)
  {
    this.dimensions = dimensions;
    this.isoLevel = isolevel;
    this.voxels = new float[this.dimensions.x, this.dimensions.y, this.dimensions.z];

    for (int x = 0; x < dimensions.x; x++)
    {
      for (int z = 0; z < dimensions.z; z++)
      {
        for (int y = 0; y < dimensions.y; y++)
        {
          voxels[x, y, z] = Mathf.Clamp01(noiseMap[x, z] * dimensions.y - y);
        }
      }
    }
  }

  public Mesh GenerateMesh()
  {
    Vector3Int cubesDimensions = dimensions - Vector3Int.one;
    var cubes = new NativeArray<Cube>(cubesDimensions.x * cubesDimensions.y * cubesDimensions.z, Allocator.TempJob);

    for (int x = 0; x < cubesDimensions.x; x++)
    {
      for (int y = 0; y < cubesDimensions.y; y++)
      {
        for (int z = 0; z < cubesDimensions.z; z++)
        {
          var cube = new Cube(
            new int3(x, y, z),
            voxels[x, y, z],
            voxels[x, y, 1 + z],
            voxels[1 + x, y, 1 + z],
            voxels[1 + x, y, z],
            voxels[x, 1 + y, z],
            voxels[x, 1 + y, 1 + z],
            voxels[1 + x, 1 + y, 1 + z],
            voxels[1 + x, 1 + y, z]
          );

          cubes[x + cubesDimensions.x * (y + cubesDimensions.y * z)] = cube;
        }
      }
    }

    var trianglesQueue = new NativeQueue<Triangle>(Allocator.TempJob);

    new PolygoniseJob()
    {
      cubes = cubes,
      isolevel = isoLevel,
      trianglesQueue = trianglesQueue.AsParallelWriter()
    }.Schedule(cubes.Length, 128).Complete();

    cubes.Dispose();

    var vertexDictionary = new Dictionary<Vector3, int>(trianglesQueue.Count, new Vector3EqualityComparer());
    var triangles = new int[trianglesQueue.Count * 3];

    for (int i = 0; trianglesQueue.TryDequeue(out Triangle triangle); i += 3)
    {
      int Dedupe(Vector3 vertex)
      {
        int vertexIndex;

        if (!vertexDictionary.TryGetValue(vertex, out vertexIndex))
        {
          vertexIndex = vertexDictionary.Count;
          vertexDictionary.Add(vertex, vertexIndex);
        }

        return vertexIndex;
      }

      triangles[i] = Dedupe(triangle.v1);
      triangles[i + 1] = Dedupe(triangle.v2);
      triangles[i + 2] = Dedupe(triangle.v3);
    }

    trianglesQueue.Dispose();

    Vector3[] vertices = vertexDictionary.Keys.ToArray();
    var uv = new Vector2[vertices.Length];

    for (int i = 0; i < uv.Length; i++)
    {
      uv[i] = new Vector2(vertices[i].x / dimensions.x, vertices[i].z / dimensions.z);
    }

    var mesh = new Mesh()
    {
      name = $"Terrain{System.DateTime.UtcNow.ToFileTime().ToString()} Mesh",
      vertices = vertices,
      triangles = triangles,
      uv = uv,
    };

    mesh.RecalculateBounds();
    mesh.RecalculateNormals();
    mesh.RecalculateTangents();
    mesh.Optimize();

    return mesh;
  }

  [BurstCompile]
  struct PolygoniseJob : IJobParallelFor
  {
    [ReadOnly] public NativeArray<Cube> cubes;
    [ReadOnly] public float isolevel;
    [WriteOnly] public NativeQueue<Triangle>.ParallelWriter trianglesQueue;

    public void Execute(int index)
    {
      Cube cube = cubes[index];

      if (!cube.Equals(Cube.one) && !cube.Equals(Cube.zero))
      {
        NativeArray<Triangle> triangles = cube.Polygonise(isolevel);

        for (int i = 0; i < triangles.Length; i++)
        {
          trianglesQueue.Enqueue(triangles[i]);
        }

        triangles.Dispose();
      }
    }
  }
}
