using UnityEngine;

public class VoxelChunk
{
    public bool[,,] voxels;
    public float[,] NoiseMap
    {
        get
        {
            float[,] _noiseMap = new float[voxels.GetLength(0), voxels.GetLength(2)];

            for (int x = 0; x < voxels.GetLength(0); x++)
            {
                for (int z = 0; z < voxels.GetLength(2); z++)
                {
                    for (int y = voxels.GetLength(1) - 1; y >= 0; y--)
                    {
                        if (voxels[x, y, z])
                        {
                            _noiseMap[x, z] = y / (voxels.GetLength(1) - 1f);
                            break;
                        }
                    }
                }
            }

            return _noiseMap;
        }
    }

    public VoxelChunk()
    {
        voxels = new bool[0, 0, 0];
    }

    public VoxelChunk(bool[,,] _voxels)
    {
        voxels = _voxels;
    }

    public void GenerateTerrain(MapDimensions dimensions, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset)
    {
        float[,] _noiseMap = Noise.GenerateNoiseMap(dimensions.x, dimensions.z, seed, scale, octaves, persistance, lacunarity, offset);
        voxels = new bool[dimensions.x, dimensions.y, dimensions.z];

        for (int x = 0; x < dimensions.x; x++)
        {
            for (int z = 0; z < dimensions.z; z++)
            {
                float maxValue = _noiseMap[x, z] * dimensions.y;

                for (int y = 0; y < dimensions.y; y++)
                {
                    voxels[x, y, z] = y <= maxValue;
                }
            }
        }
    }
}
