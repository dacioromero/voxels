using UnityEditor;
using UnityEngine;
using NativeComponentExtensions;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using MarchingCubes;
using System.Linq;

/*
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/MapGenerator.cs
 * under the MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
public class TerrainGenerator : MonoBehaviour
{
  [SerializeField, Range(0, 1)] private float isolevel = 0.5f, persistence = 0.5f;
  [SerializeField] private float noiseScale = 100, lacunarity = 2;
  [SerializeField] private int octaves = 16, seed;

  [SerializeField] private Vector2 offset;
  [SerializeField] private TerrainType[] regions;
  [SerializeField] private Vector3Int dimensions;

  public void Generate()
  {
    VoxelChunk chunk = VoxelChunk.Generate(dimensions, seed, noiseScale, octaves, persistence, lacunarity, offset);

    GetComponent<MeshFilter>().sharedMesh = GetComponent<MeshCollider>().sharedMesh = GenerateMesh(chunk.voxels);

    GetComponent<MeshRenderer>().material = new Material(Shader.Find("Legacy Shaders/Diffuse"))
    {
      mainTexture = GenerateTexture(chunk.NoiseMap)
    };
  }

  Texture2D GenerateTexture(float[,] noiseMap)
  {
    Color[] colorMap = new Color[dimensions.x * dimensions.z];

    Random.InitState(seed);

    for (int y = 0; y < dimensions.z; y++)
    {
      for (int x = 0; x < dimensions.x; x++)
      {
        for (int i = 0; i < regions.Length; i++)
        {
          if (noiseMap[x, y] <= regions[i].height)
          {
            colorMap[y * dimensions.x + x] = regions[i].color.Evaluate(Random.value);
            break;
          }
        }
      }
    }

    string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();

    AssetDatabase.CreateAsset(TextureGenerator.TextureFromColourMap(colorMap, dimensions.x, dimensions.z), "Assets/Terrain Data/" + id + " Texture.asset");
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    return (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Terrain Data/" + id + " Texture.asset", typeof(Texture2D));
  }

  Mesh GenerateMesh(float[,,] voxels)
  {
    var grids = new NativeList<Gridcell>(Allocator.TempJob);
    var gridOffsets = new NativeList<Vector3Int>(Allocator.TempJob);

    for (int x = 0; x < dimensions.x - 1; x++)
    {
      for (int y = 0; y < dimensions.y; y++)
      {
        for (int z = 0; z < dimensions.z - 1; z++)
        {
          Gridcell gridcell;

          try
          {
            gridcell = new Gridcell
            {
              val1 = voxels[x, y, z],
              val2 = voxels[x, y, 1 + z],
              val3 = voxels[1 + x, y, 1 + z],
              val4 = voxels[1 + x, y, z],
              val5 = voxels[x, 1 + y, z],
              val6 = voxels[x, 1 + y, 1 + z],
              val7 = voxels[1 + x, 1 + y, 1 + z],
              val8 = voxels[1 + x, 1 + y, z],
            };
          }

          catch
          {
            gridcell = new Gridcell
            {
              val1 = voxels[x, y, z],
              val2 = voxels[x, y, 1 + z],
              val3 = voxels[1 + x, y, 1 + z],
              val4 = voxels[1 + x, y, z],
              val5 = 0,
              val6 = 0,
              val7 = 0,
              val8 = 0,
            };
          }

          if (gridcell.Equals(Gridcell.one) || gridcell.Equals(Gridcell.zero)) continue;

          int index = x + dimensions.y * (y + (dimensions.z - 1) * z);

          grids.Add(gridcell);
          gridOffsets.Add(new Vector3Int(x, y, z));
        }
      }
    }

    var trianglesMC = new NativeQueue<Triangle>(Allocator.Persistent);

    new PolygoniseJob()
    {
      grids = grids,
      gridOffsets = gridOffsets,
      isolevel = isolevel,

      trianglesMC = trianglesMC.ToConcurrent(),
    }.Schedule(grids.Length, 64).Complete();

    grids.Dispose();
    gridOffsets.Dispose();

    Dictionary<Vector3, int> vertexDictionary = new Dictionary<Vector3, int>(Vec3EqComparer.c);
    NativeList<int> triangles = new NativeList<int>(Allocator.TempJob);

    while (trianglesMC.TryDequeue(out Triangle triangle))
    {
      foreach (Vector3 vertex in triangle.Verts)
      {
        if (!vertexDictionary.TryGetValue(vertex, out int vertexIndex))
        {
          vertexIndex = vertexDictionary.Count;
          vertexDictionary.Add(vertex, vertexIndex);
        }

        triangles.Add(vertexIndex);
      }
    }

    var vertices = new NativeArray<Vector3>(vertexDictionary.Keys.ToArray(), Allocator.TempJob);

    trianglesMC.Dispose();

    var uv = new NativeArray<Vector2>(vertices.Length, Allocator.TempJob);

    new GetUVJob()
    {
      verts = vertices,
      uv = uv,
      dimensions = dimensions,
    }.Schedule(uv.Length, 64).Complete();

    Mesh terrainMesh = new Mesh
    {
      name = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString() + " Mesh",
      vertices = vertices.ToArray(),
      triangles = triangles.ToArray(),
      uv = uv.ToArray(),
    };

    vertices.Dispose();
    triangles.Dispose();
    uv.Dispose();

    terrainMesh.RecalculateBounds();
    terrainMesh.RecalculateTangents();
    terrainMesh.RecalculateNormals();

    AssetDatabase.CreateAsset(terrainMesh, "Assets/Terrain Data/" + terrainMesh.name + ".asset");
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    return (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Terrain Data/" + terrainMesh.name + ".asset", typeof(Mesh));
  }

  struct PolygoniseJob : IJobParallelFor
  {
    [ReadOnly] public NativeArray<Gridcell> grids;
    [ReadOnly] public NativeArray<Vector3Int> gridOffsets;
    [ReadOnly] public float isolevel;

    [WriteOnly] public NativeQueue<Triangle>.Concurrent trianglesMC;

    public void Execute(int index) => trianglesMC.Enqueue(MarchingCubesCalc.Polygonise(grids[index], gridOffsets[index], isolevel));
  }

  struct GetUVJob : IJobParallelFor
  {
    [ReadOnly] public NativeArray<Vector3> verts;
    [ReadOnly] public Vector3Int dimensions;

    [WriteOnly] public NativeArray<Vector2> uv;

    public void Execute(int index) => uv[index] = new Vector2(verts[index].x / dimensions.x, verts[index].z / dimensions.z);
  }

  void OnValidate()
  {
    if (lacunarity < 1)
      lacunarity = 1;

    if (octaves < 0)
      octaves = 0;

    if (dimensions.x < 1)
      dimensions.x = 1;

    if (dimensions.y < 1)
      dimensions.y = 1;

    if (dimensions.z < 1)
      dimensions.z = 1;
  }
}
