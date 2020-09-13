using UnityEngine;

/*
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/Noise.cs
 * under the MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
 */

public static class Noise
{
  public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
  {
    var noiseMap = new float[mapWidth, mapHeight];
    var prng = new System.Random(seed);
    var octaveOffsets = new Vector2[octaves];

    for (int i = 0; i < octaves; i++)
    {
      float offsetX = prng.Next(-100000, 100000) + offset.x;
      float offsetY = prng.Next(-100000, 100000) + offset.y;
      octaveOffsets[i] = new Vector2(offsetX, offsetY);
    }

    if (scale <= 0)
      scale = 0.0001f;

    float maxNoiseHeight = float.MinValue;
    float minNoiseHeight = float.MaxValue;

    float halfWidth = mapWidth / 2f;
    float halfHeight = mapHeight / 2f;

    for (int y = 0; y < mapHeight; y++)
    {
      for (int x = 0; x < mapWidth; x++)
      {

        float amplitude = 1;
        float frequency = 1;
        float noiseHeight = 0;

        for (int i = 0; i < octaves; i++)
        {
          float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
          float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;

          float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
          noiseHeight += perlinValue * amplitude;

          amplitude *= persistence;
          frequency *= lacunarity;
        }

        if (noiseHeight > maxNoiseHeight)
          maxNoiseHeight = noiseHeight;

        else if (noiseHeight < minNoiseHeight)
          minNoiseHeight = noiseHeight;

        noiseMap[x, y] = noiseHeight;
      }
    }

    for (int y = 0; y < mapHeight; y++)
    {
      for (int x = 0; x < mapWidth; x++)
      {
        noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
      }
    }

    return noiseMap;
  }
}
