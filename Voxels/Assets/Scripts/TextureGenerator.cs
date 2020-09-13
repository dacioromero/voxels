using UnityEngine;

/*
 * Adapted from https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/Proc%20Gen%20E04/Assets/Scripts/TextureGenerator.cs
 * under the MIT License https://github.com/SebLague/Procedural-Landmass-Generation/blob/2c519dac25f350365f95a83a3f973a9e6d3e1b83/LICENSE.md, retrieved in April 2018
 */

public static class TextureGenerator
{
  public static Texture2D TextureFromColourMap(Color[] colorMap, int width, int height)
  {
    var texture = new Texture2D(width, height)
    {
      filterMode = FilterMode.Point,
      wrapMode = TextureWrapMode.Clamp,
    };

    texture.SetPixels(colorMap);
    texture.Apply();

    return texture;
  }
}
