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
    public float seed;

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
        float landShapeFrequency = landShapeFrequencyPercentage * 5 / blockSize;
        int minSurfaceHeight = maxSurfaceHeight - surfaceRange;
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                float landShapeNoise = Mathf.Clamp(Mathf.PerlinNoise(seed + x * landShapeFrequency, seed + z * landShapeFrequency), 0, 1);
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
                float terrainShapeNoise = Mathf.Clamp(Mathf.PerlinNoise(x * terrainShapeFrequency, z * terrainShapeFrequency), 0, 1);
                terrainShapeHeightMap[x, z] = terrainShapeNoise * (float)surfaceRange;
            }
        }
    }

    public int worms = 1;
    public int wormMinLength = 25;
    public int wormMaxLength = 50;
    public int wormStepLength = 1;
    public float wormHorizontalFrequency = 0.00824556f;
    public float wormVerticalFrequency = 0.00824556f;
    public float flatWormChance = 2;
    public float maxWormHorizontalRotation = 0.15f;
    public float maxWormVerticalRotation = 0.15f;

    //public void GenerateCave(Vector3Int chunkStartPosition, int chunkWidth, int maxHeight)
    public void GenerateCave()
    {
        int blockSize = landSize * land.chunkSizeX;

        for (int wormCount = worms; wormCount > 0; wormCount--)
        {
            Vector3 wormPos = new Vector3(Random.Range(0, blockSize), Random.Range(0, land.chunkSizeY), Random.Range(0, blockSize));
            land.AddBlock(Vector3Int.FloorToInt(wormPos), (short)1);
            int wormLength = Random.Range(wormMinLength, wormMaxLength);

            float yRotationSeed = Random.Range(0f, 10000f);
            float xRotationSeed = Random.Range(0f, 10000f);
            float yRotation = Random.Range(-Mathf.PI, Mathf.PI);
            float xRotation = 0;//Random.Range(-Mathf.PI / 2, Mathf.PI / 2);
            for (int wormStep = 0; wormStep < wormLength; wormStep += wormStepLength)
            {
                yRotation += PerlinNoise(wormStep, 0, seed, maxWormHorizontalRotation, wormHorizontalFrequency, 3) - PerlinNoise(wormStep, 0, seed + 123.45f, maxWormHorizontalRotation, wormHorizontalFrequency, 3);
                xRotation = PerlinNoise(wormStep, 0, seed + 678.91f, maxWormVerticalRotation, wormVerticalFrequency, 3) - PerlinNoise(wormStep, 0, seed + 234.56f, maxWormVerticalRotation, wormVerticalFrequency, 3);
                xRotation = Mathf.Pow(Mathf.Abs(xRotation), flatWormChance) * Mathf.Sign(xRotation);
                Vector3 stepDirection = new Vector3(
                    Mathf.Sin(yRotation) * Mathf.Cos(xRotation),
                    Mathf.Sin(xRotation),
                    Mathf.Cos(yRotation) * Mathf.Cos(xRotation)
                );
                wormPos += stepDirection.normalized * wormStepLength;

                Vector3Int wormPosInt = Vector3Int.FloorToInt(wormPos);
                if (wormPosInt.y >= land.chunkSizeY || wormPosInt.x >= blockSize || wormPosInt.z >= blockSize || wormPosInt.y < 0 || wormPosInt.x < 0 || wormPosInt.z < 0)
                    break;
                land.AddBlock(wormPosInt, (short)2);
            }
        }
    }

    public float PerlinNoise(float x, float y, float seed, float amplitude, float frequency, int octaves)
    {
        float noise = 0;
        for (int i = 1; i <= octaves; i++)
        {
            noise += Mathf.PerlinNoise(frequency * i * x + i * seed, frequency * i * y + i * seed) / i;
        }
        return Mathf.Clamp(noise, 0, 1) * amplitude;
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
            float XY = PerlinNoise(x, y);
            float YZ = PerlinNoise(y, z);
            float ZX = PerlinNoise(z, x);

            float YX = PerlinNoise(y, z);
            float ZY = PerlinNoise(z, y);
            float XZ = PerlinNoise(x, z);

            float val = (XY + YZ + ZX + YX + ZY + XZ)/6f;
            return val;
        }
    */
}