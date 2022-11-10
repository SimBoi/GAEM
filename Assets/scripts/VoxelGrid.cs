using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using System.Xml;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public enum Faces
{
    Up,
    Down,
    Right,
    Left,
    Front,
    Back
}

public class VoxelGrid : MonoBehaviour
{
    public int vertexLength;
    public Vector2Int resolution;
    public int chunkSizeX = 16;
    public int chunkSizeY = 16;
    public int chunkSizeZ = 16;
    public GameObject chunkPrefab;
    public ItemPrefabs itemPrefabs;
    public BlockToItemID blockToItemID;
    public Dictionary<Vector2Int, short[,,]> blockIDs = new Dictionary<Vector2Int, short[,,]>();
    public Dictionary<Vector2Int, Dictionary<Vector3Int, Block>> customBlocks = new Dictionary<Vector2Int, Dictionary<Vector3Int, Block>>();

    private ChunkPool chunkPool;
    private int renderDistance;

    private void Awake()
    {
        GameObject.Find("GameManager").GetComponent<GameManager>().voxelGrids.Add(this);
        renderDistance = 0;
        chunkPool = new ChunkPool(this, chunkPrefab, 0);
    }

    public void SetRenderDistance(int renderDistance, Vector3 position)
    {
        this.renderDistance = renderDistance;

        int chunkPoolSize = 0;
        for (int x = -renderDistance; x < renderDistance; x++)
        {
            int zRange = (int)Mathf.Sqrt(Mathf.Pow(renderDistance, 2) - Mathf.Pow(x, 2));
            for (int z = -zRange; z < zRange; z++) chunkPoolSize++;
        }
        chunkPool.ReInitialize(this, chunkPrefab, chunkPoolSize);

        RenderSurroundingChunks(GridToChunkIndex(GlobalToGridCoords(position)));
    }

    private void RenderChunk(Vector2Int chunkIndex)
    {
        if (chunkPool.IsInstantiated(chunkIndex)) return;
        Chunk chunk = chunkPool.Instantiate(chunkIndex, new Vector3Int(chunkIndex.x * chunkSizeX, 0, chunkIndex.y * chunkSizeZ), transform.rotation);
        chunk.chunkIndex = chunkIndex;
        chunk.requiresMeshGeneration = true;
    }

    private void UnrenderChunk(Vector2Int chunkIndex)
    {
        if (!chunkPool.IsInstantiated(chunkIndex)) return;
        chunkPool.Destroy(chunkIndex);
    }

    public void RenderSurroundingChunks(Vector2Int centerIndex)
    {
        HashSet<Vector2Int> surroundingChunks = new HashSet<Vector2Int>();

        // calculate indexes of surrounding chunks
        for (int x = centerIndex.x - renderDistance; x < centerIndex.x + renderDistance; x++)
        {
            int zRange = (int)Mathf.Sqrt(Mathf.Pow(renderDistance, 2) - Mathf.Pow(centerIndex.x - x, 2));
            for (int z = centerIndex.y - zRange; z < centerIndex.y + zRange; z++) surroundingChunks.Add(new Vector2Int(x, z));
        }

        // destroy rendered chunks outside the range
        for (int i = 0; i < chunkPool.size; i++)
        {
            if (!surroundingChunks.Contains(chunkPool[i].chunkIndex)) UnrenderChunk(chunkPool[i].chunkIndex);
        }

        // instantiate missing chunks inside the range
        foreach (Vector2Int chunkIndex in surroundingChunks)
        {
            RenderChunk(chunkIndex);
        }
    }

    private void InitChunkRegion(Vector2Int chunkIndex)
    {
        blockIDs.Add(chunkIndex, new short[chunkSizeX, chunkSizeY, chunkSizeZ]);
        customBlocks.Add(chunkIndex, new Dictionary<Vector3Int, Block>());
        for (int x = 0; x < chunkSizeX; x++)
        {
            for (int y = 0; y < chunkSizeY; y++)
            {
                for (int z = 0; z < chunkSizeZ; z++)
                {
                    blockIDs[chunkIndex][x, y, z] = 0;
                }
            }
        }
    }

    public bool AddBlock(Vector3Int gridCoords, short blockID, Quaternion rotation = default)
    {
        if (gridCoords.y > chunkSizeY) return false;

        Vector2Int chunkIndex = GridToChunkIndex(gridCoords);
        Vector3Int chunkCoords = GridToChunkCoords(gridCoords);

        // initialize new chunk region
        if (!blockIDs.ContainsKey(chunkIndex)) InitChunkRegion(chunkIndex);

        if (blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z] == 0)
        {
            blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z] = blockID;
            if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z])].GetComponent<Block>().hasCustomMesh)
            {
                Vector3 spawnPos = transform.TransformPoint(gridCoords + new Vector3(0.5f, 0.5f, 0.5f));
                Block customBlock = (Block)itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z])].GetComponent<Block>().PlaceCustomBlock(spawnPos, rotation, this, gridCoords);
                customBlocks[chunkIndex].Add(chunkCoords, customBlock);
            }
            else if (chunkPool.IsInstantiated(chunkIndex))
            {
                chunkPool[chunkIndex].requiresMeshGeneration = true;
            }
            return true;
        }
        return false;
    }

    public bool RemoveBlock(Vector3Int gridCoords, bool spawnItem = false)
    {
        if (gridCoords.y > chunkSizeY) return false;

        Vector2Int chunkIndex = GridToChunkIndex(gridCoords);
        Vector3Int chunkCoords = GridToChunkCoords(GridToChunkCoords(gridCoords));
        
        if (blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z] != 0)
        {
            if (customBlocks[chunkIndex].ContainsKey(chunkCoords))
            {
                Vector3 spawnPos = transform.TransformPoint(chunkCoords + new Vector3(0.5f, 0.5f, 0.5f));
                customBlocks[chunkIndex][chunkCoords].BreakCustomBlock(spawnPos, spawnItem);
                customBlocks[chunkIndex].Remove(chunkCoords);
            }
            else
            {
                if (spawnItem == true)
                {
                    GameObject newItem;
                    Vector3 spawnPos = transform.TransformPoint(gridCoords + new Vector3(0.5f, 0.5f, 0.5f));
                    newItem = Instantiate(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z])], spawnPos, default(Quaternion));
                    Item spawnedItem = newItem.GetComponent<Item>();
                    spawnedItem.SetStackSize(1);
                }

                if (chunkPool.IsInstantiated(chunkIndex)) chunkPool[chunkIndex].requiresMeshGeneration = true;
            }

            blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z] = 0;
            return true;
        }
        return false;
    }

    // message[0] = (VoxelGrid)return
    public void GetGridRefMsg(object[] message)
    {
        message[0] = this;
    }

    public short GetBlockID(Vector3Int gridCoords)
    {
        if (gridCoords.y > chunkSizeY) return 0;

        Vector2Int chunkIndex = GridToChunkIndex(gridCoords);
        if (!chunkPool.IsInstantiated(chunkIndex)) return 0;

        Vector3Int chunkCoords = GridToChunkCoords(gridCoords);
        return blockIDs.ContainsKey(chunkIndex) ? blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z] : (short)0;
    }

    public float GetStiffness(Vector3Int gridCoords)
    {
        Vector3Int chunkCoords = GridToChunkCoords(gridCoords);
        Vector2Int chunkIndex = GridToChunkIndex(gridCoords);
        return itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[chunkIndex][chunkCoords.x, chunkCoords.y, chunkCoords.z])].GetComponent<Block>().stiffness;
    }

    public Block GetCustomBlock(Vector3Int gridCoords)
    {
        if (gridCoords.y > chunkSizeY) return null;

        Vector2Int chunkIndex = GridToChunkIndex(gridCoords);
        if (!customBlocks.ContainsKey(chunkIndex)) return null;

        Vector3Int chunkCoords = GridToChunkCoords(gridCoords);
        return customBlocks[chunkIndex].ContainsKey(chunkCoords) ? customBlocks[chunkIndex][chunkCoords] : null;
    }

    public Vector3Int GlobalToGridCoords(Vector3 globalCoords)
    {
        return Vector3Int.FloorToInt(transform.InverseTransformPoint(globalCoords));
    }

    public Vector3Int GridToChunkCoords(Vector3Int gridCoords)
    {
        int x = gridCoords.x % chunkSizeX;
        x = x < 0 ? x + chunkSizeX : x;
        int y = gridCoords.y % chunkSizeY;
        y = y < 0 ? y + chunkSizeY : y;
        int z = gridCoords.z % chunkSizeZ;
        z = z < 0 ? z + chunkSizeZ : z;
        return new Vector3Int(x, y, z);
    }

    public Vector2Int GridToChunkIndex(Vector3Int gridCoords)
    {
        int x = Mathf.FloorToInt(gridCoords.x / (float)chunkSizeX);
        int z = Mathf.FloorToInt(gridCoords.z / (float)chunkSizeZ);
        return new Vector2Int(x, z);
    }

    static public Vector3Int FaceToDirection(Faces face)
    {
        switch (face)
        {
            case Faces.Up: return Vector3Int.up;
            case Faces.Down: return Vector3Int.down;
            case Faces.Right: return Vector3Int.right;
            case Faces.Left: return Vector3Int.left;
            case Faces.Front: return Vector3Int.forward;
        }
        return Vector3Int.back;
    }

    static public Faces GetOppositeFace(Faces face)
    {
        switch (face)
        {
            case Faces.Up: return Faces.Down;
            case Faces.Down: return Faces.Up;
            case Faces.Right: return Faces.Left;
            case Faces.Left: return Faces.Right;
            case Faces.Front: return Faces.Back;
        }
        return Faces.Front;
    }
}

public class ChunkPool
{
    private Chunk[] pool;
    private Queue<int> reuseQueue = new Queue<int>();
    private Dictionary<Vector2Int, int> inUse = new Dictionary<Vector2Int, int>();

    public int size
    {
        get => pool.Length;
    }

    public int available
    {
        get => reuseQueue.Count;
    }

    public Chunk this[Vector2Int chunkIndex]
    {
        get => pool[inUse[chunkIndex]];
    }

    public Chunk this[int i]
    {
        get => pool[i];
    }

    public ChunkPool(VoxelGrid voxelGrid, GameObject chunkPrefab, int size)
    {
        pool = new Chunk[size];
        InitializePool(voxelGrid, chunkPrefab);
    }

    public void ReInitialize(VoxelGrid voxelGrid, GameObject chunkPrefab, int newSize)
    {
        for (int i = 0; i < size; i++) if (pool[i] != null) GameObject.Destroy(pool[i].gameObject);
        pool = new Chunk[newSize];
        reuseQueue.Clear();
        inUse.Clear();
        InitializePool(voxelGrid, chunkPrefab);
    }

    private void InitializePool(VoxelGrid voxelGrid, GameObject chunkPrefab)
    {
        for (int i = 0; i < size; i++)
        {
            pool[i] = GameObject.Instantiate(chunkPrefab).GetComponent<Chunk>();
            pool[i].voxelGrid = voxelGrid;
            pool[i].transform.SetParent(voxelGrid.transform);
            pool[i].gameObject.SetActive(false);
            reuseQueue.Enqueue(i);
        }
    }

    public bool IsInstantiated(Vector2Int chunkIndex)
    {
        return inUse.ContainsKey(chunkIndex);
    }

    // replacement for GameObject.Instantiate
    // should only be called after making sure a chunk is available for reuse
    public Chunk Instantiate(Vector2Int chunkIndex, Vector3 position = default, Quaternion rotation = default)
    {
        int i = reuseQueue.Dequeue();
        inUse.Add(chunkIndex, i);

        pool[i].gameObject.SetActive(true);
        pool[i].transform.position = position;
        pool[i].transform.rotation = rotation;

        return pool[i];
    }

    // replacement for GameObject.Destroy
    public void Destroy(Vector2Int chunkIndex)
    {
        int i = inUse[chunkIndex];

        pool[i].gameObject.SetActive(false);

        reuseQueue.Enqueue(i);
        inUse.Remove(chunkIndex);
    }
}