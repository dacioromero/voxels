using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
public class TerrainGenerator : MonoBehaviour
{
    [Range(0, 1)]
    public float isolevel = 1;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistence;
    public float lacunarity;

    public int seed;
    public Vector2 offset;
    public TerrainType[] regions;

    public MapDimensions dimensions;

    public void Generate()
    {
        Debug.Log("Generating");

        VoxelChunk chunk = new VoxelChunk();
        chunk.GenerateTerrain(dimensions, seed, noiseScale, octaves, persistence, lacunarity, offset);

        GetComponent<MeshFilter>().sharedMesh = GetComponent<MeshCollider>().sharedMesh = GenerateMesh(chunk.voxels);

        Material material = new Material(Shader.Find("Default-Diffuse"))
        {
            mainTexture = GenerateTexture(chunk.NoiseMap)
        };

        GetComponent<MeshRenderer>().material = material;
    }

    Texture2D GenerateTexture(float[,] noiseMap)
    {
        string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();
        Color[] colorMap = new Color[dimensions.x * dimensions.z];

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

        return (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/Terrain Data/" + id + " Texture.asset", typeof(Texture2D));
    }

    Mesh GenerateMesh(float[,,] voxels)
    {
        string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();
        Dictionary<Vector3, int> vertexDictionary = new Dictionary<Vector3, int>();
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
                        gridcell.val = new float[] {
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
                        gridcell.val = new float[] {
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

                    GetTriangles(gridcell, new Vector3(x, y, z), ref vertexDictionary, ref tris);
                }
            }
        }

        Vector3[] verts = vertexDictionary.Keys.ToArray();
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
        terrainMesh.RecalculateNormals();

        AssetDatabase.CreateAsset(terrainMesh, "Assets/Terrain Data/" + terrainMesh.name + ".asset");
        AssetDatabase.SaveAssets();

        return (Mesh)AssetDatabase.LoadAssetAtPath("Assets/Terrain Data/" + terrainMesh.name + ".asset", typeof(Mesh));
    }

    void GetTriangles(MarchingCubes.Gridcell gridcell, Vector3 offset, ref Dictionary<Vector3, int> vertexDictionary, ref List<int> tris)
    {
        MarchingCubes.Triangle[] triangles = MarchingCubes.Polygonise(gridcell, isolevel);

        if (triangles.Length > 0)
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                for (int j = 0; j < triangles[i].p.Length; j++)
                {
                    Vector3 vertex = offset + triangles[i].p[j];

                    int vertexIndex;

                    if (!vertexDictionary.TryGetValue(vertex, out vertexIndex))
                    {
                        vertexIndex = vertexDictionary.Count;
                        vertexDictionary.Add(vertex, vertexIndex);
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

    public MapDimensions(int _x, int _y, int _z)
    {
        x = _x;
        y = _y;
        z = _z;
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
