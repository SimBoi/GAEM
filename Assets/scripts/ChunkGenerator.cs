using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    public Land land;
    public int landSize;

    private void Start()
    {
        GenerateChunks();
    }

    public void GenerateChunks()
    {
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                land.AddBlock(new Vector3Int(x, 0, z), (short)1);
            }
        }
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                land.AddBlock(new Vector3Int(x, 1, z), (short)1);
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
}