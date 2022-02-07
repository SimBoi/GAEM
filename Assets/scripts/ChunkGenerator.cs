using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChunkGenerator : MonoBehaviour
{
    public Land land;
    public int landSize;
    public bool Gen3d;

    private void Start()
    {
        GenerateChunks();
    }
    /*
    public void GenerateChunks()
    {
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                land.AddBlock(new Vector3Int(x, 0, z), (short)2);
            }
        }
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                land.AddBlock(new Vector3Int(x, 1, z), (short)2);
            }
        }
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                if (Random.Range(0, 2) == 1) land.RemoveBlock(new Vector3Int(x, 1, z));
            }
        }

        return;
    }
    */

    public void GenerateChunks()
    {
        if (Gen3d)
            Generate3d();
        else
            Generate();
    }
  
    public bool randomizeNoiseOffset;
    public Vector3 perlinOffset;
    public float noiseScale = 1f;

    public void Generate()
    { 
        if (randomizeNoiseOffset)
        {
            perlinOffset = new Vector3(Random.Range(0, 256), Random.Range(0, 256), Random.Range(0, 256));
        }
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                float generated = 10 * Mathf.PerlinNoise(((float)x + perlinOffset.x) * noiseScale, ((float)z + perlinOffset.z) * noiseScale);
                land.AddBlock(Vector3Int.FloorToInt(new Vector3(x, generated % (land.chunkSizeY - 1), z)), (short)2);
            }
        }
    }

    public void Generate3d()
    {
        if (randomizeNoiseOffset)
        {
            perlinOffset = new Vector3(Random.Range(0, 9999), Random.Range(0, 9999), Random.Range(0, 9999));
        }
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int y = 0; y < landSize * land.chunkSizeY; y++)
            {
                for (int z = 0; z < landSize * land.chunkSizeZ; z++)
                {
                    float generated = GenerateNoise(((float)x + perlinOffset.x) * noiseScale, ((float)y + perlinOffset.y)* noiseScale, ((float)z + perlinOffset.z) * noiseScale);
                    if (generated > 0.5f) land.AddBlock(new Vector3Int(x,y,z), (short)2);
                }
            }
        }
    }

    public float GenerateNoise(float x, float y, float z)
    {
        float XY = Mathf.PerlinNoise(x, y);
        float YZ = Mathf.PerlinNoise(y, z);
        float ZX = Mathf.PerlinNoise(z, x);

        float YX = Mathf.PerlinNoise(y, z);
        float ZY = Mathf.PerlinNoise(z, y);
        float XZ = Mathf.PerlinNoise(x, z);

        float val = (XY + YZ + ZX + YX + ZY + XZ)/6f;
        return val;
    }
}