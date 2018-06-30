using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

/* 
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/MapGenerator.cs 
 * under MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
public class TerrainGenerator : MonoBehaviour
{
    // [SerializeField]
    // private float vertDistThreshold = 0.25f;

    [SerializeField, Range(0, 1)]
    private float isolevel = 0.5f, persistence;
    [SerializeField]
    private float noiseScale, lacunarity;
    [SerializeField]
    private int octaves, seed;

    [SerializeField]
    private Vector2 offset;
    [SerializeField]
    private TerrainType[] regions;
    [SerializeField]
    private MapDimensions dimensions;

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
        string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();
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
                        float random = Random.Range(0.95f, 1.05f);
                        colorMap[y * dimensions.x + x] = regions[i].color * random;
                        break;
                    }
                }
            }
        }

        Texture2D terrainTexture = TextureGenerator.TextureFromColourMap(colorMap, dimensions.x, dimensions.z);

        AssetDatabase.CreateAsset(terrainTexture, "Assets/Terrain Data/" + id + " Texture.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Terrain Data/" + id + " Texture.asset", typeof(Texture2D));
    }

    Mesh GenerateMesh(float[,,] voxels)
    {
        double gtTime = 0;
        string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();

        Dictionary<Vector3, int> vertIndexDictionary = new Dictionary<Vector3, int>(dimensions.Volume / 10);//, new Vector3Comparer(vertDistThreshold * vertDistThreshold));
        List<int> tris = new List<int>();

        for (int x = 0; x < dimensions.x - 1; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z - 1; z++)
                {
                    MarchingCubes.Gridcell gridcell = new MarchingCubes.Gridcell();

                    try
                    {
                        gridcell.vals = new float[] {
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
                        gridcell.vals = new float[] {
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

                    double GetTrianglesStart = EditorApplication.timeSinceStartup;
                    GetTriangles(gridcell, new Vector3(x, y, z), ref vertIndexDictionary, ref tris);
                    gtTime += EditorApplication.timeSinceStartup - GetTrianglesStart;
                }
            }
        }

        Vector3[] verts = vertIndexDictionary.Keys.ToArray();
        Vector2[] uv = new Vector2[verts.Length];

        for (int i = 0; i < uv.Length; i++)
        {
            uv[i] = new Vector2(verts[i].x / dimensions.x, verts[i].z / dimensions.z);
        }

        Mesh terrainMesh = new Mesh
        {
            name = id + " Mesh",
            vertices = verts,
            triangles = tris.ToArray(),
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

    void GetTriangles(MarchingCubes.Gridcell gridcell, Vector3 offset, ref Dictionary<Vector3, int> vertIndexDictionary, ref List<int> tris)
    {
        MarchingCubes.Triangle[] triangles = MarchingCubes.Polygonise(gridcell, isolevel);

        if (triangles.Length > 0)
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                for (int j = 0; j < triangles[i].points.Length; j++)
                {
                    Vector3 vertex = offset + triangles[i].points[j];

                    int vertexIndex;

                    if (!vertIndexDictionary.TryGetValue(vertex, out vertexIndex))
                    {
                        vertexIndex = vertIndexDictionary.Count;
                        vertIndexDictionary.Add(vertex, vertexIndex);
                    }

                    tris.Add(vertexIndex);
                }
            }
        }
    }

    void OnValidate()
    {
        if (lacunarity < 1)
        {
            lacunarity = 1;
        }

        if (octaves < 0)
        {
            octaves = 0;
        }
    }
}

[System.Serializable]
public struct MapDimensions
{
    public int x;
    public int y;
    public int z;

    public int Volume
    {
        get
        {
            return x * y * z;
        }
    }

    public MapDimensions(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static MapDimensions operator *(MapDimensions a, int b)
    {
        return new MapDimensions(a.x * b, a.y * b, a.z * b);
    }
}


[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

class Vector3Comparer : IEqualityComparer<Vector3>
{
    float sqrThreshold;

    public Vector3Comparer(float sqrThreshold)
    {
        this.sqrThreshold = sqrThreshold;
    }

    public bool Equals(Vector3 x, Vector3 y)
    {
        return (x - y).sqrMagnitude < sqrThreshold;
    }

    public int GetHashCode(Vector3 codeh)
    {
        return 0;
    }
}