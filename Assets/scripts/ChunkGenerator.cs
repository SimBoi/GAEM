using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ChunkGenerator : MonoBehaviour
{
    public Land land;
    public int landSize;
    public bool Gen3d;

    private void Start()
    {
        Generate();
    }
    /*
    public void GenerateChunks()
    {
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                land.AddBlock(new Vector3Int(x, 0, z), (short)2);
            }
        }
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                land.AddBlock(new Vector3Int(x, 1, z), (short)2);
            }
        }
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                if (Random.Range(0, 2) == 1) land.RemoveBlock(new Vector3Int(x, 1, z));
            }
        }

        return;
    }
    */

    public float minDistanceFromEdgePercetage;
    public float landShapeFrequencyPercentage;
    public float terrainShapeFrequency;
    public int maxSurfaceHeight;
    public int surfaceRange;
    public int seaLevel;

    public async void Generate()
    {
        int blockSize = landSize * land.chunkSizeX;

        float[,] landShapeHeightMap = new float[blockSize, blockSize];
        float[,] terrainShapeHeightMap = new float[blockSize, blockSize];
        float[,] resultHeightMap = new float[blockSize, blockSize];

        int minDistanceFromEdge = (int)(minDistanceFromEdgePercetage * blockSize);

        var calcLandShapeHeightMap = CalcLandShapeHeightMap(blockSize, landShapeHeightMap);
        var calcTerrainShapeHeightMap = CalcTerrainShapeHeightMap(blockSize, terrainShapeHeightMap);
        await Task.WhenAll(calcLandShapeHeightMap, calcTerrainShapeHeightMap);

        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                float distanceFromEdge = Mathf.Min(
                    blockSize - x,
                    blockSize - z,
                    x,
                    z
                );
                resultHeightMap[x, z] = landShapeHeightMap[x, z] + terrainShapeHeightMap[x, z];
                if (distanceFromEdge < minDistanceFromEdge)
                {
                    resultHeightMap[x, z] *= Mathf.Sqrt(distanceFromEdge / minDistanceFromEdge);
                }

                if (resultHeightMap[x, z] > seaLevel)
                    land.AddBlock(Vector3Int.FloorToInt(new Vector3(x, resultHeightMap[x, z], z)), (short)1);
                else
                    land.AddBlock(Vector3Int.FloorToInt(new Vector3(x, resultHeightMap[x, z], z)), (short)2);

            }
        }
    }

    public async Task CalcLandShapeHeightMap(int blockSize, float[,] landShapeHeightMap)
    {
        int seed = Random.Range(1000, 10000);
        float landShapeFrequency = landShapeFrequencyPercentage * 5 / blockSize;
        int minSurfaceHeight = maxSurfaceHeight - surfaceRange;
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                float landShapeNoise = Mathf.Clamp(Mathf.PerlinNoise((float)(x + seed) * landShapeFrequency, (float)(z + seed) * landShapeFrequency), 0, 1);
                landShapeHeightMap[x, z] = landShapeNoise * minSurfaceHeight;
            }
        }
    }

    public async Task CalcTerrainShapeHeightMap(int blockSize, float[,] terrainShapeHeightMap)
    {
        float terrainShapeFrequency = this.terrainShapeFrequency / 100;
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                float terrainShapeNoise = Mathf.Clamp(Mathf.PerlinNoise((float)x * terrainShapeFrequency, (float)z * terrainShapeFrequency), 0, 1);
                terrainShapeHeightMap[x, z] = terrainShapeNoise * (float)surfaceRange;
            }
        }
    }

/*    public void Generate3d()
    {
        if (randomizeNoiseOffset)
        {
            perlinOffset = new Vector3(Random.Range(0, 9999), Random.Range(0, 9999), Random.Range(0, 9999));
        }
        for (int x = 0; x < blockSize; x++)
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
*/}