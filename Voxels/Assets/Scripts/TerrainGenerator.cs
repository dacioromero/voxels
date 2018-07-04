using UnityEditor;
using UnityEngine;
using Unity.Entities;
using NativeComponentExtensions;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using MarchingCubes;
using Unity.Mathematics;

/* 
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/MapGenerator.cs 
 * under the MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
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

        UnityEngine.Random.InitState(seed);

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
        List<Gridcell> grids = new List<Gridcell>();
        List<Vector3Int> gridOffsets = new List<Vector3Int>();

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

                    if (gridcell.Equals(Gridcell.one) || gridcell.Equals(Gridcell.zero))
                        continue;

                    grids.Add(gridcell);
                    gridOffsets.Add(new Vector3Int(x, y, z));
                }
            }
        }

        NativeQueue<Vector3> verticesNQ = new NativeQueue<Vector3>(Allocator.Persistent);

        PolygoniseJob polygonise = new PolygoniseJob(new NativeArray<Gridcell>(grids.ToArray(), Allocator.Persistent),
                                                     new NativeArray<Vector3Int>(gridOffsets.ToArray(), Allocator.Persistent),
                                                     isolevel,
                                                     verticesNQ);

        polygonise.Schedule(grids.Count, 64).Complete();

        // Dispose inputs
        polygonise.grids.Dispose();
        polygonise.gridOffsets.Dispose();

        // Output corresponding mesh points and triangle arrays
        MeshifyJob meshify = new MeshifyJob(verticesNQ.ToNativeArray(Allocator.Persistent));
        
        verticesNQ.Dispose();

        meshify.Schedule(meshify.vertices_out.Count(), 2).Complete();
        
        meshify.vertices_in.Dispose();
        meshify.input_handled1.Dispose();
        meshify.input_handled2.Dispose();

        // Convert outputs to standard arrays and dispose
        int[] triangles = meshify.triangles_out.ToArray();
        meshify.triangles_out.Dispose();

        Vector3[] vertices = meshify.vertices_out.ToArray();
        meshify.vertices_out.Dispose();

        // Get UV based on position
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < uv.Length; i++)
        {
            uv[i] = new Vector2(vertices[i].x / dimensions.x, vertices[i].z / dimensions.z);
        }

        Mesh terrainMesh = new Mesh
        {
            name = "Terrain" + System.DateTime.UtcNow.ToFileTime().ToString() + " Mesh",
            vertices = vertices,
            triangles = triangles,
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
        public NativeArray<Gridcell> grids;
        [ReadOnly]
        public NativeArray<Vector3Int> gridOffsets;
        [ReadOnly]
        public float isolevel;

        [WriteOnly]
        public NativeQueue<Vector3>.Concurrent verts;

        public PolygoniseJob(NativeArray<Gridcell> grids, NativeArray<Vector3Int> gridOffsets, float isolevel, NativeQueue<Vector3>.Concurrent verts)
        {
            this.grids = grids;
            this.gridOffsets = gridOffsets;
            this.isolevel = isolevel;

            this.verts = verts;
        }

        public void Execute(int index)
        {
            foreach (Triangle t in MarchingCubesCalc.Polygonise(grids[index], isolevel))
            {
                Vector3[] triangle = new Vector3[3];

                for(int i = 0; i < 3; i++)
                {
                    triangle[i] = t.Verts[i] + gridOffsets[index];
                }

                // Enqueue triangle points at the same time to keep them together
                verts.Enqueue(triangle);
            }
        }
    }

    struct MeshifyJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<Vector3> vertices_in;

        // Using bytes because booleans aren't blittable
        public NativeArray<byte> input_handled1;
        public NativeArray<byte> input_handled2;

        public NativeArray<Vector3> vertices_out;
        public NativeArray<int> triangles_out;


        public MeshifyJob(NativeArray<Vector3> vertices_in)
        {
            this.vertices_in = vertices_in;

            input_handled1 = new NativeArray<byte>(vertices_in.Length, Allocator.Persistent);
            input_handled2 = new NativeArray<byte>(vertices_in.Length, Allocator.Persistent);

            // Set triangle output size of the input and vertex output to number of distinct inputs
            triangles_out = new NativeArray<int>(vertices_in.Length, Allocator.Persistent);
            vertices_out = new NativeArray<Vector3>(vertices_in.Distinct(Vec3EqComparer.c).Count(), Allocator.Persistent);
        }

        public void Execute(int triIndex)
        {
            // Set our vert index to the index of triangle index
            int vertIndex = triIndex;

            // While another thread is handling or has handled this vertex
            while (IsInputHandled(vertIndex))
            {
                // Look for another unless there is another then exit
                if (vertIndex == vertices_in.Length)
                    return;
            }

            // Set our vertex
            Vector3 vertex = vertices_in[vertIndex];

            // Add this vertex to the output at the index the triangle index and add triangle index to the output at the index of this vertex
            vertices_out[triIndex] = vertex;
            triangles_out[vertIndex] = triIndex;

            // For the next vertices in the input
            for (int i = vertIndex + 1; i < vertices_in.Length; i++)
            {
                // Skip if handled or isn't equal to our vertex
                if (IsInputHandled(i) || vertex != vertices_in[i])
                    continue;

                // Mark this input as handled and at the index of this vertex add the index of the reference vertex
                SetInputHandled(i, 1);
                triangles_out[i] = triIndex;
            }
        }

        bool IsInputHandled(int index)
        {
            bool handled = false;

            try { handled |= input_handled1[index] == 1; }
            catch { }
            try { handled |= input_handled2[index] == 1; }
            catch { }

            return handled;
        }

        void SetInputHandled(int index, byte handled)
        {
            try { input_handled1[index] = handled; }
            catch { }
            try { input_handled2[index] = handled; }
            catch { }
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

// Default Equals method is more accurate == operator is more lenient
public class Vec3EqComparer : IEqualityComparer<Vector3>
{
    static public Vec3EqComparer c = new Vec3EqComparer();

    public bool Equals(Vector3 x, Vector3 y)
    {
        return x == y;
    }

    public int GetHashCode(Vector3 obj)
    {
        return obj.GetHashCode();
    }
}