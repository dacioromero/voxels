using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Unity.Jobs;
using Unity.Collections;
using MarchingCubes;

/* 
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/MapGenerator.cs 
 * under MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
public class TerrainGenerator : MonoBehaviour
{
    [SerializeField, Range(0, 1)]
    private float isolevel = 0.5f, persistence = 0.5f, vertDistThreshold = 0.5f;
    [SerializeField]
    private float noiseScale = 100, lacunarity = 2;
    [SerializeField]
    private int octaves = 16, seed;

    [SerializeField]
    private Vector2 offset;
    [SerializeField]
    private TerrainType[] regions;
    [SerializeField]
    private Vector3Int dimensions;

    public void Generate()
    {
        VoxelChunk chunk = VoxelChunk.Generate(dimensions, seed, noiseScale, octaves, persistence, lacunarity, offset);

        GetComponent<MeshFilter>().sharedMesh = GetComponent<MeshCollider>().sharedMesh = GenerateMesh(chunk.voxels);

        Material material = new Material(Shader.Find("Legacy Shaders/Diffuse"))
        {
            mainTexture = GenerateTexture(chunk.NoiseMap)
        };

        GetComponent<MeshRenderer>().material = material;
    }

    Texture2D GenerateTexture(float[,] noiseMap)
    {
        Color[] colorMap = new Color[dimensions.x * dimensions.z];

        Random.InitState(seed);

        for (int y = 0; y < dimensions.z; y++)
        {
            for (int x = 0; x < dimensions.x; x++)
            {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * dimensions.x + x] = regions[i].color.Evaluate(Random.value);
                        break;
                    }
                }
            }
        }

        Texture2D terrainTexture = TextureGenerator.TextureFromColourMap(colorMap, dimensions.x, dimensions.z);

        string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();

        AssetDatabase.CreateAsset(terrainTexture, "Assets/Terrain Data/" + id + " Texture.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Terrain Data/" + id + " Texture.asset", typeof(Texture2D));
    }

    Mesh GenerateMesh(float[,,] voxels)
    {
        List<float> gridVals = new List<float>();
        List<Vector3Int> gridPositions = new List<Vector3Int>();

        for (int x = 0; x < dimensions.x - 1; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z - 1; z++)
                {
                    float[] gridVal;

                    try
                    {
                        gridVal = new float[] {
                            voxels[    x,     y,     z],
                            voxels[    x,     y, 1 + z],
                            voxels[1 + x,     y, 1 + z],
                            voxels[1 + x,     y,     z],

                            voxels[    x, 1 + y,     z],
                            voxels[    x, 1 + y, 1 + z],
                            voxels[1 + x, 1 + y, 1 + z],
                            voxels[1 + x, 1 + y,     z],
                        };
                    }

                    catch
                    {
                        gridVal = new float[] {
                            voxels[    x,     y,     z],
                            voxels[    x,     y, 1 + z],
                            voxels[1 + x,     y, 1 + z],
                            voxels[1 + x,     y,     z],

                            0,
                            0,
                            0,
                            0
                        };
                    }

                    if (Enumerable.SequenceEqual(gridVal, Gridcell.one.vals) || Enumerable.SequenceEqual(gridVal, Gridcell.zero.vals))
                        continue;

                    gridVals.AddRange(gridVal);
                    gridPositions.Add(new Vector3Int(x, y, z));
                }
            }
        }
        
        PolygoniseJob polygoniseJob = new PolygoniseJob()
        {
            gridVals = new NativeArray<float>(gridVals.ToArray(), Allocator.Persistent),
            gridPositions = new NativeArray<Vector3Int>(gridPositions.ToArray(), Allocator.Persistent),
            isolevel = isolevel,

            verts = new NativeList<Vector3>(Allocator.Persistent),
            vertIndices = new NativeList<int>(Allocator.Persistent),
            tris = new NativeList<int>(Allocator.Persistent),
        };

        polygoniseJob.Schedule(gridVals.Count / 4, 64).Complete();
        polygoniseJob.DisposeInput();

        Vector3[] verts = polygoniseJob.verts.ToArray();
        int[] tris = polygoniseJob.tris.ToArray();
        polygoniseJob.DisposeInternal();
        Vector2[] uv = new Vector2[verts.Length];

        for (int i = 0; i < uv.Length; i++)
        {
            uv[i] = new Vector2(verts[i].x / dimensions.x, verts[i].z / dimensions.z);
        }

        string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();

        Mesh terrainMesh = new Mesh
        {
            name = id + " Mesh",
            vertices = verts,
            triangles = tris,
            uv = uv,
        };

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
        [ReadOnly]
        public NativeArray<float> gridVals;
        [ReadOnly]
        public NativeArray<Vector3Int> gridPositions;
        [ReadOnly]
        public float isolevel;

        [WriteOnly]
        public NativeList<int> vertIndices;
        [WriteOnly]
        public NativeList<Vector3> verts;
        [WriteOnly]
        public NativeList<int> tris;

        public void Execute(int index)
        {
            foreach (Triangle triangle in MarchingCubesCalc.Polygonise(gridVals[index * 4], gridVals[index * 4 + 1], gridVals[index * 4 + 2], gridVals[index * 4 + 3], isolevel))
            {
                foreach (Vector3 p in triangle.points)
                {
                    Vector3 vert = p + gridPositions[index];

                    if (verts.Contains(vert))
                    {
                        vertIndices.Add(verts.IndexOf(vert));
                    }
                    else
                    {
                        vertIndices.Add(verts.Length);
                        verts.Add(vert);
                    }
                }
            }
        }

        public void DisposeInternal()
        {
            vertIndices.Dispose();
            verts.Dispose();
            tris.Dispose();
        }

        public void DisposeInput()
        {
            gridVals.Dispose();
            gridPositions.Dispose();
        }
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

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Gradient color;
}
