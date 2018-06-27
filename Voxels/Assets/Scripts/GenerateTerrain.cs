using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer)), DisallowMultipleComponent]
public class GenerateTerrain : MonoBehaviour
{
    public float isolevel = 1;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;
    public TerrainType[] regions;

    public MapDimensions dimensions;

    // Credit https://github.com/SebLague/Procedural-Landmass-Generation

    [ContextMenu("Generate")]
    public void Generate()
    {
        Debug.Log("Generating");

        string id = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString();

        VoxelChunk chunk = new VoxelChunk();
        chunk.GenerateTerrain(dimensions, seed, noiseScale, octaves, persistance, lacunarity, offset);
        bool[,,] voxels = chunk.voxels;
        float[,] noiseMap = chunk.NoiseMap;

        Color[] colourMap = new Color[dimensions.x * dimensions.z];

        for (int y = 0; y < dimensions.z; y++)
        {
            for (int x = 0; x < dimensions.x; x++)
            {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * dimensions.x + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        Texture2D terrainTexture = TextureGenerator.TextureFromColourMap(colourMap, dimensions.x, dimensions.z);

        AssetDatabase.CreateAsset(terrainTexture, "Assets/" + id + " Texture.asset");
        AssetDatabase.SaveAssets();

        GetComponent<MeshRenderer>().sharedMaterial.mainTexture = (Texture2D) AssetDatabase.LoadAssetAtPath("Assets/" + id + " Texture.asset", typeof(Texture2D));

        Dictionary<Vector3, int> vertexDictionary = new Dictionary<Vector3, int>();
        List<int> tris = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        MarchingCubes.Gridcell gridcell = new MarchingCubes.Gridcell();
        MarchingCubes.Triangle[] triangles = new MarchingCubes.Triangle[] { new MarchingCubes.Triangle(), new MarchingCubes.Triangle(), new MarchingCubes.Triangle(), new MarchingCubes.Triangle(), new MarchingCubes.Triangle() };
        Vector3 currentOffset;
        short triangleCount = 0;

        for (int x = 0; x < dimensions.x - 1; x++)
        {
            for (int y = 0; y < dimensions.y; y++)
            {
                for (int z = 0; z < dimensions.z - 1; z++)
                {
                    try {
                        gridcell.val = new float[] {
                            voxels[    x,     y,     z] ? 1 : 0,
                            voxels[    x,     y, 1 + z] ? 1 : 0,
                            voxels[1 + x,     y, 1 + z] ? 1 : 0,
                            voxels[1 + x,     y,     z] ? 1 : 0,

                            voxels[    x, 1 + y,     z] ? 1 : 0,
                            voxels[    x, 1 + y, 1 + z] ? 1 : 0,
                            voxels[1 + x, 1 + y, 1 + z] ? 1 : 0,
                            voxels[1 + x, 1 + y,     z] ? 1 : 0,
                        };
                    }

                    catch
                    {
                        gridcell.val = new float[] {
                            voxels[    x,     y,     z] ? 1 : 0,
                            voxels[    x,     y, 1 + z] ? 1 : 0,
                            voxels[1 + x,     y, 1 + z] ? 1 : 0,
                            voxels[1 + x,     y,     z] ? 1 : 0,

                            0,
                            0,
                            0,
                            0
                        };
                    }

                    triangleCount = MarchingCubes.Polygonise(gridcell, isolevel, ref triangles);

                    if (triangleCount > 0)
                    {
                        currentOffset = new Vector3(x, y, z);

                        for (short i = 0; i < triangleCount; i++)
                        {
                            for (short j = 0; j < triangles[i].p.Length; j++)
                            {
                                Vector3 vertex = currentOffset + triangles[i].p[j];

                                int vertexIndex;

                                if (!vertexDictionary.TryGetValue(vertex, out vertexIndex))
                                {
                                    vertexIndex = vertexDictionary.Count;
                                    vertexDictionary.Add(vertex, vertexIndex);
                                    uv.Add(new Vector2(vertex.x / dimensions.x, vertex.z / dimensions.z));
                                }

                                tris.Add(vertexIndex);
                            }
                        }
                    }
                }
            }
        }

        Mesh terrainMesh = new Mesh
        {
            name = id + " Mesh",
            vertices = vertexDictionary.Keys.ToArray(),
            triangles = tris.ToArray(),
            uv = uv.ToArray(),
        };

        terrainMesh.RecalculateBounds();
        terrainMesh.RecalculateNormals();

        AssetDatabase.CreateAsset(terrainMesh, "Assets/" + terrainMesh.name + ".asset");
        AssetDatabase.SaveAssets();

        GetComponent<MeshFilter>().sharedMesh = (Mesh) AssetDatabase.LoadAssetAtPath("Assets/" + terrainMesh.name + ".asset", typeof(Mesh));
        MeshCollider collider = GetComponent<MeshCollider>();
        collider.sharedMesh = terrainMesh;
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
    public Color colour;
}
