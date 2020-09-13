using UnityEditor;
using UnityEngine;

/*
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/MapGenerator.cs
 * under the MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
 */

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer)), DisallowMultipleComponent, ExecuteInEditMode]
public class TerrainGenerator : MonoBehaviour
{
  [SerializeField, Range(0, 1)] private float isolevel = 0.5f, persistence = 0.5f;
  [SerializeField] private float noiseScale = 100, lacunarity = 2;
  [SerializeField] private int octaves = 16, seed;

  [SerializeField] private Vector2 offset;
  [SerializeField] private TerrainType[] regions;
  [SerializeField] private Vector3Int dimensions;

  private MeshFilter meshFilter;
  private MeshCollider meshCollider;
  private MeshRenderer meshRenderer;

  public void Generate()
  {
    float[,] noiseMap = Noise.GenerateNoiseMap(dimensions.x, dimensions.z, seed, noiseScale, octaves, persistence, lacunarity, offset);

    meshFilter.sharedMesh = meshCollider.sharedMesh = new VoxelChunk(dimensions, noiseMap, isolevel).GenerateMesh();

    meshRenderer.material = new Material(Shader.Find("Legacy Shaders/Diffuse"))
    {
      mainTexture = GenerateTexture(noiseMap)
    };
  }

  void Awake()
  {
    meshFilter = GetComponent<MeshFilter>();
    meshCollider = GetComponent<MeshCollider>();
    meshRenderer = GetComponent<MeshRenderer>();
  }

  void Start()
  {
    if (Application.isPlaying)
      Generate();
  }

  Texture2D GenerateTexture(float[,] noiseMap)
  {
    var colorMap = new Color[dimensions.x * dimensions.z];

    UnityEngine.Random.InitState(seed);

    for (int y = 0; y < dimensions.z; y++)
    {
      for (int x = 0; x < dimensions.x; x++)
      {
        for (int i = 0; i < regions.Length; i++)
        {
          if (noiseMap[x, y] <= regions[i].height)
          {
            colorMap[y * dimensions.x + x] = regions[i].color.Evaluate(UnityEngine.Random.value);
            break;
          }
        }
      }
    }

    Texture2D texture = TextureGenerator.TextureFromColourMap(colorMap, dimensions.x, dimensions.z);
    texture.name = $"Terrain{System.DateTime.UtcNow.ToFileTime().ToString()} Texture";

    return texture;
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

    Generate();
  }
}
