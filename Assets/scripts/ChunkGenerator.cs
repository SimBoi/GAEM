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

    public float surface_max = 1;
    public float surface_min = 1;
    public bool randomizeNoiseOffset;
    public Vector3 perlinOffset;
    public float noiseScale = 1f;
    public float landRadiusInChunks = 100;

    public void Generate()
    {
        if (randomizeNoiseOffset)
        {
            perlinOffset = new Vector3(Random.Range(0, 256), Random.Range(0, 256), Random.Range(0, 256));
        }
        
        float[,] distanceMap = new float[landSize * land.chunkSizeX, landSize * land.chunkSizeZ];
        float landSizeInBlocks = landSize * land.chunkSizeX;

        for (int x = 0; x < landSizeInBlocks; x++)
        {
            for (int z = 0; z < landSizeInBlocks; z++)
            {
                distanceMap[x, z] = Mathf.Min(
                    Mathf.Abs(x - land.transform.position.x),
                    Mathf.Abs(z - land.transform.position.z),
                    Mathf.Abs(x - landSize * land.chunkSizeX),
                    Mathf.Abs(z - landSize * land.chunkSizeZ)
                    );
            }
        }
        
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                float generated = Mathf.Abs(Mathf.PerlinNoise(((float)x + perlinOffset.x) * noiseScale, ((float)z + perlinOffset.z) * noiseScale));
                generated = surface_min + (surface_max - surface_min) * generated;
                generated = generated * distanceMap[x, z]/ (landSizeInBlocks/2);
                generated = Mathf.Min(generated, land.chunkSizeY);
                generated = Mathf.Max(generated, 0);    
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