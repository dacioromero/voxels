using UnityEngine;

public class VoxelChunk
{
    public float[,,] voxels;
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
                        if (voxels[x, y, z] > 0)
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
        voxels = new float[0, 0, 0];
    }

    public VoxelChunk(float[,,] voxels)
    {
        this.voxels = voxels;
    }

    public static VoxelChunk Generate(Vector3Int dimensions, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        VoxelChunk chunk = new VoxelChunk();

        float[,] noiseMap = Noise.GenerateNoiseMap(dimensions.x, dimensions.z, seed, scale, octaves, persistence, lacunarity, offset);
        chunk.voxels = new float[dimensions.x, dimensions.y, dimensions.z];

        for (int x = 0; x < dimensions.x; x++)
        {
            for (int z = 0; z < dimensions.z; z++)
            {
                float surfaceHeight = noiseMap[x, z] * dimensions.y;
        
                for (int y = 0; y < dimensions.y; y++)
                {
                    chunk.voxels[x, y, z] = Mathf.Clamp01(surfaceHeight - y);
                }
            }
        }

        return chunk;
    }
}
