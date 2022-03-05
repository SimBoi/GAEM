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
    public long seed;

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
                landShapeHeightMap[x, z] = Noise2D(x, z, seed, minSurfaceHeight / 2, landShapeFrequency, 1) + minSurfaceHeight / 2;
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
                terrainShapeHeightMap[x, z] = Noise2D(x, z, seed, surfaceRange / 2, terrainShapeFrequency, 3) + surfaceRange / 2;
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
    public int minWormRadius = 1;
    public int maxWormRadius = 4;
    public float wormRadiusFrequency = 0.00824556f;

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
                yRotation += Noise2D(wormStep, 0, seed, maxWormHorizontalRotation, wormHorizontalFrequency, 3);
                xRotation = Noise2D(wormStep, 0, seed + 123, maxWormVerticalRotation, wormVerticalFrequency, 3);
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

                int radius = (int)(Noise2D(wormStep, 0, seed + 456, maxWormRadius - minWormRadius, wormRadiusFrequency, 2) + minWormRadius);
                for (int r = 0; r <= radius; r++)
                    for (float theta = -Mathf.PI/2; theta <= Mathf.PI/2; theta += 0.3f)
                        for (float phi = -Mathf.PI; phi <= Mathf.PI; phi += 0.3f)
                        {
                            Vector3 sphereCoords = new Vector3(
                                Mathf.Sin(phi) * Mathf.Cos(theta),
                                Mathf.Sin(theta),
                                Mathf.Cos(phi) * Mathf.Cos(theta)
                            ) * r;
                            land.AddBlock(wormPosInt + Vector3Int.FloorToInt(sphereCoords), (short)2);
                        }
            }
        }
    }

    public float Noise2D(float x, float y, long seed, float amplitude, float frequency, int octaves)
    {
        float noise = 0;
        float fractalAmplitude = 0;
        for (int i = 1; i <= octaves; i++)
        {
            float currentAmplitude = Mathf.Pow(0.5f, i-1);
            noise += OpenSimplex2S.Noise2(i * seed, frequency * i * x, frequency * i * y) * currentAmplitude;
            fractalAmplitude += currentAmplitude;
        }
        return Mathf.Clamp(noise, -fractalAmplitude, fractalAmplitude) * amplitude / fractalAmplitude;
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