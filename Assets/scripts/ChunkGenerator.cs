using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ChunkGenerator : MonoBehaviour
{
    public Land land;
    public int landSize;
    public bool GenCave;

    private void Start()
    {
        if (!GenCave) Generate();
        else GenerateCave();
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

    public int worms = 1;
    public int wormLength = 50;
    public float wormNoiseFrequency = 1;
    public int wormScale = 1;

    //public void GenerateCave(Vector3Int chunkStartPosition, int chunkWidth, int maxHeight)
    public void GenerateCave()
    {
        int blockSize = landSize * land.chunkSizeX;
        
        for (int wormCount = worms; wormCount > 0; wormCount--)
        {
            int x = (int)Random.Range(0, blockSize);
            int y = (int)Random.Range(0, blockSize);
            int z = (int)Random.Range(0, blockSize);

            float wx = Mathf.Clamp(Mathf.PerlinNoise(x * wormNoiseFrequency, x * wormNoiseFrequency), 0, 1);
            float wy = Mathf.Clamp(Mathf.PerlinNoise(y * wormNoiseFrequency, y * wormNoiseFrequency), 0, 1);
            float wz = Mathf.Clamp(Mathf.PerlinNoise(z * wormNoiseFrequency, z * wormNoiseFrequency), 0, 1);

            for (int wormLengthCount = wormLength; wormLengthCount > 0; wormLengthCount--)
            {
                Debug.Log(new Vector3Int(x, y, z));

                wx = Mathf.Clamp(Mathf.PerlinNoise(x * wx * wormNoiseFrequency, x * (1 / wx) * wormNoiseFrequency), 0, 1);
                wy = Mathf.Clamp(Mathf.PerlinNoise(y * wy * wormNoiseFrequency, y * (1 / wy) * wormNoiseFrequency), 0, 1);
                wz = Mathf.Clamp(Mathf.PerlinNoise(z * wz * wormNoiseFrequency, z * (1 / wz) * wormNoiseFrequency), 0, 1);

                x = Mathf.Clamp(WormMovement(wx, x), 0, blockSize);
                y = Mathf.Clamp(WormMovement(wy, y), 0, 256);
                z = Mathf.Clamp(WormMovement(wz, z), 0, blockSize);

                for (int xcord = x - (int)wormScale / 2; xcord <= x + wormScale / 2; xcord++)
                {
                    for (int ycord = y - (int)wormScale / 2; ycord <= y + wormScale / 2; ycord++)
                    {
                        for (int zcord = z - (int)wormScale / 2; zcord <= z + wormScale / 2; zcord++)
                        {
                            Vector3Int coordVector = new Vector3Int(Mathf.Clamp(xcord,0, blockSize), Mathf.Clamp(ycord, 0, blockSize), Mathf.Clamp(zcord, 0, blockSize));
                            land.AddBlock(coordVector, (short)2);
                        }
                    }
                }
            }
        }
    }

    public int WormMovement(float noise , int currPos)
    {
        if (noise > 0.5) return currPos + 1;
        if (noise <= 0.5) return currPos - 1;
        return currPos - 1;
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
        */
    }