using UnityEngine;

/* 
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/TextureGenerator.cs 
 * under MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
 */

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colorMap, width, height);
    }
}
