using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ChunkGenerator : MonoBehaviour
{
    public Land land;
    public int landSize;
    public int landHeight;

    public short[,,] generatedBlockIDs;

    public float minDistanceFromEdgePercetage;
    public float landShapeFrequencyPercentage;
    public float terrainShapeFrequency;
    public int terrainShapeOctaves;
    public int maxSurfaceHeight;
    public int surfaceRange;
    public int seaLevel;
    public long seed;

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

    private async void Start()
    {
        int blockSize = landSize * land.chunkSizeX;

        generatedBlockIDs = new short[blockSize, landHeight, blockSize];
        Random.InitState((int)seed);

        await GenerateSurfaceAsync();
        FillBelowSurface();

        Task[] tasks = new Task[worms];
        for (int i = 0; i < worms; i++)
        {
            Vector3 wormPos = new Vector3(Random.Range(0, blockSize), Random.Range(0, landHeight), Random.Range(0, blockSize));
            int wormLength = Random.Range(wormMinLength, wormMaxLength);
            int rSeed = Random.Range(0, 10000);
            int ySeed = Random.Range(0, 10000);
            int xSeed = Random.Range(0, 10000);
            float yInitialRotation = Random.Range(-Mathf.PI, Mathf.PI);
            float xInitialRotation = 0;
            tasks[i] = Task.Run(() => GenerateCave(wormPos, wormLength, rSeed, ySeed, xSeed, yInitialRotation, xInitialRotation));
        }
        await Task.WhenAll(tasks);

        GenerateMesh();
    }

    public async Task GenerateSurfaceAsync()
    {
        int blockSize = landSize * land.chunkSizeX;

        float[,] landShapeHeightMap = new float[blockSize, blockSize];
        float[,] terrainShapeHeightMap = new float[blockSize, blockSize];
        float[,] resultHeightMap = new float[blockSize, blockSize];

        int minDistanceFromEdge = (int)(minDistanceFromEdgePercetage * blockSize);

        Task calcLandShapeHeightMap = Task.Run(() => CalcHeightMap(blockSize, seed, maxSurfaceHeight - surfaceRange, landShapeFrequencyPercentage * 5 / blockSize, landShapeHeightMap));
        Task calcTerrainShapeHeightMap = FractalHeightMapAsync(seed, blockSize, surfaceRange, terrainShapeFrequency / 100, terrainShapeOctaves, terrainShapeHeightMap);
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

                Vector3Int blockCoords = Vector3Int.FloorToInt(new Vector3(x, resultHeightMap[x, z], z));
                if (resultHeightMap[x, z] > seaLevel)
                    generatedBlockIDs[blockCoords.x, blockCoords.y, blockCoords.z] = (short)1;
                else
                    generatedBlockIDs[blockCoords.x, blockCoords.y, blockCoords.z] = (short)2;
            }
        }
    }

    public void CalcHeightMap(int blockSize, long seed, float amplitude, float frequency, float[,] terrainShapeHeightMap)
    {
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                terrainShapeHeightMap[x, z] = Noise2D(x, z, seed, amplitude / 2, frequency, 1) + amplitude / 2;
            }
        }
    }

    public async Task FractalHeightMapAsync(long seed, int blockSize, float amplitude, float frequency, int octaves, float[,] terrainShapeHeightMap)
    {
        float fractalAmplitude = 0;
        List<float[,]> terrainShapeOctaves = new List<float[,]>();
        for (int i = 0; i < octaves; i++)
            terrainShapeOctaves.Add(new float[blockSize, blockSize]);

        Task[] tasks = new Task[octaves];
        for (int i = 0; i < octaves; i++)
        {
            float currentAmplitude = Mathf.Pow(0.5f, i);
            var tmp = i;
            tasks[tmp] = Task.Run(() => CalcHeightMap(blockSize, tmp * seed, currentAmplitude, frequency * Mathf.Pow(2, tmp), terrainShapeOctaves[tmp]));
            fractalAmplitude += currentAmplitude;
        }
        await Task.WhenAll(tasks);

        float amplitudeFactor = amplitude / fractalAmplitude;
        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                for (int i = 0; i < octaves; i++)
                {
                    terrainShapeHeightMap[x, z] += terrainShapeOctaves[i][x, z];
                }
                terrainShapeHeightMap[x, z] *= amplitudeFactor;
            }
        }
    }

    public void FillBelowSurface()
    {
        int blockSize = landSize * land.chunkSizeX;

        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                for (int y = landHeight - 2; y >= 0; y--)
                {
                    if (generatedBlockIDs[x, y + 1, z] != 0)
                        generatedBlockIDs[x, y, z] = (short)1;
                }
            }
        }
    }

    public void GenerateCave(Vector3 wormPos, int wormLength, int rSeed, int ySeed, int xSeed, float yRotation, float xRotation)
    {
        int blockSize = landSize * land.chunkSizeX;

        for (int wormStep = 0; wormStep < wormLength; wormStep += wormStepLength)
        {
            yRotation += Noise2D(wormStep, 0, ySeed, maxWormHorizontalRotation, wormHorizontalFrequency, 3);
            xRotation = Noise2D(wormStep, 0, xSeed, maxWormVerticalRotation, wormVerticalFrequency, 3);
            xRotation = Mathf.Pow(Mathf.Abs(xRotation), flatWormChance) * Mathf.Sign(xRotation);
            Vector3 stepDirection = new Vector3(
                Mathf.Sin(yRotation) * Mathf.Cos(xRotation),
                Mathf.Sin(xRotation),
                Mathf.Cos(yRotation) * Mathf.Cos(xRotation)
            );
            wormPos += stepDirection.normalized * wormStepLength;

            Vector3Int wormPosInt = Vector3Int.FloorToInt(wormPos);
            if (wormPosInt.y >= landHeight || wormPosInt.x >= blockSize || wormPosInt.z >= blockSize || wormPosInt.y < 0 || wormPosInt.x < 0 || wormPosInt.z < 0)
                break;

            int radius = (int)(Noise2D(wormStep, 0, rSeed, maxWormRadius - minWormRadius, wormRadiusFrequency, 2) + minWormRadius);
            for (int r = 0; r <= radius; r++)
            {
                for (float theta = -Mathf.PI / 2; theta <= Mathf.PI / 2; theta += 0.3f)
                {
                    for (float phi = -Mathf.PI; phi <= Mathf.PI; phi += 0.3f)
                    {
                        Vector3 sphereCoords = new Vector3(
                            Mathf.Sin(phi) * Mathf.Cos(theta),
                            Mathf.Sin(theta),
                            Mathf.Cos(phi) * Mathf.Cos(theta)
                        ) * r;
                        Vector3Int blockCoords = wormPosInt + Vector3Int.FloorToInt(sphereCoords);
                        if (blockCoords.y >= landHeight || blockCoords.x >= blockSize || blockCoords.z >= blockSize || blockCoords.y < 0 || blockCoords.x < 0 || blockCoords.z < 0)
                            continue;
                        generatedBlockIDs[blockCoords.x, blockCoords.y, blockCoords.z] = (short)0;
                    }
                }
            }
        }
    }

    public void GenerateMesh()
    {
        int blockSize = landSize * land.chunkSizeX;

        for (int x = 0; x < blockSize; x++)
        {
            for (int z = 0; z < blockSize; z++)
            {
                for (int y = 0; y < landHeight; y++)
                {
                    land.AddBlock(new Vector3Int(x, y, z), generatedBlockIDs[x, y, z], default(Quaternion), false);
                }
            }
        }

        land.regenerateMesh();
    }

    public float Noise2D(float x, float y, long seed, float amplitude, float frequency, int octaves)
    {
        float noise = 0;
        float fractalAmplitude = 0;
        for (int i = 1; i <= octaves; i++)
        {
            float currentAmplitude = Mathf.Pow(0.5f, i-1);
            float currentFrequency = frequency * Mathf.Pow(2, i-1);
            noise += OpenSimplex2S.Noise2(i * seed, currentFrequency * x, currentFrequency * y) * currentAmplitude;
            fractalAmplitude += currentAmplitude;
        }
        return Mathf.Clamp(noise, -fractalAmplitude, fractalAmplitude) * amplitude / fractalAmplitude;
    }
}