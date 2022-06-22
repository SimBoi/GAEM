using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class ChunkGenerator : NetworkBehaviour
{
    public Land land;
    public int landSize;
    private float timer = 0;

    private void Start()
    {
        /*if (IsServer)
        {
            //GenerateChunks();
            land.AddBlock(new Vector3Int(0,0,0), (short)2);
            land.AddBlock(new Vector3Int(20,0,0), (short)2);
            land.AddBlock(new Vector3Int(20,0,20), (short)2);
            land.AddBlock(new Vector3Int(0,0,20), (short)2);
        }*/
    }

    private void Update()
    {
        if (IsServer && timer < 8)
        {
            timer += Time.deltaTime;
            if (timer >= 8)
            {
                GenerateChunks();
            }
        }
    }

    public void GenerateChunks()
    {
        short[,,] blockIDs = new short[landSize * land.chunkSizeX, land.chunkSizeY, landSize * land.chunkSizeZ];
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
            for (int y = 0; y < land.chunkSizeY; y++)
                for (int z = 0; z < landSize * land.chunkSizeZ; z++)
                    blockIDs[x, y, z] = 0;
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                blockIDs[x, 0, z] = 2;
            }
        }
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                blockIDs[x, 1, z] = 2;
            }
        }
        for (int x = 0; x < landSize * land.chunkSizeX; x++)
        {
            for (int z = 0; z < landSize * land.chunkSizeZ; z++)
            {
                if (Random.Range(0, 2) == 1) blockIDs[x, 1, z] = 0;
            }
        }
        land.SetLand(new Vector2Int(0, 0), blockIDs, landSize * land.chunkSizeX, land.chunkSizeY, landSize * land.chunkSizeZ);
        land.RemoveBlock(new Vector3Int(1, 1, 1), true);
        land.RemoveBlock(new Vector3Int(1, 0, 1), true);
        land.RemoveBlock(new Vector3Int(1, 1, 2), true);
        land.AddBlock(new Vector3Int(2, 1, 1), 2);
        land.AddBlock(new Vector3Int(2, 2, 1), 2);
        land.AddBlock(new Vector3Int(2, 3, 1), 2);
    }
}