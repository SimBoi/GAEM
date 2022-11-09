using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    public VoxelGrid voxelGrid;
    public int gridSize;

    private void Start()
    {
        GenerateChunks();
    }

    public void GenerateChunks()
    {
        for (int x = 0; x < gridSize * voxelGrid.chunkSizeX; x++)
        {
            for (int z = 0; z < gridSize * voxelGrid.chunkSizeZ; z++)
            {
                voxelGrid.AddBlock(new Vector3Int(x, 0, z), (short)2);
            }
        }
        for (int x = 0; x < gridSize * voxelGrid.chunkSizeX; x++)
        {
            for (int z = 0; z < gridSize * voxelGrid.chunkSizeZ; z++)
            {
                voxelGrid.AddBlock(new Vector3Int(x, 1, z), (short)2);
            }
        }
        for (int x = 0; x < gridSize * voxelGrid.chunkSizeX; x++)
        {
            for (int z = 0; z < gridSize * voxelGrid.chunkSizeZ; z++)
            {
                if (Random.Range(0, 2) == 1) voxelGrid.RemoveBlock(new Vector3Int(x, 1, z));
            }
        }

        return;
    }
}