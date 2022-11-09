using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public class Land : MonoBehaviour
{
    public Vector3Int LandPosition = new Vector3Int(0, 0, 0);
    public int vertexLength;
    public Vector2Int resolution;
    public int chunkSizeX = 16;
    public int chunkSizeY = 16;
    public int chunkSizeZ = 16;
    public GameObject chunkPrefab;

    private ChunkPool chunkPool;
    private int _renderDistance;
    private int chunkPoolSize;

    public int renderDistance
    {
        get
        {
            return _renderDistance;
        }
        set
        {
            _renderDistance = value;
            chunkPoolSize = 2 * value;
            chunkPool = new ChunkPool(chunkPrefab, chunkPoolSize);
        }
    }

    private void Awake()
    {
        transform.position = LandPosition;
        chunkPool = new ChunkPool(chunkPrefab, chunkPoolSize);
    }

    public void RenderChunk(Vector2Int coords)
    {
        GameObject newChunk = chunkPool.Instantiate(coords, new Vector3Int(coords.x * chunkSizeX, 0, coords.y * chunkSizeZ), transform.rotation, gameObject.transform);
        newChunk.GetComponent<Chunk>().resolution = resolution;
        newChunk.GetComponent<Chunk>().vertexLength = vertexLength;
        newChunk.GetComponent<Chunk>().sizeX = chunkSizeX;
        newChunk.GetComponent<Chunk>().sizeY = chunkSizeY;
        newChunk.GetComponent<Chunk>().sizeZ = chunkSizeZ;
        newChunk.GetComponent<Chunk>().WakeUp();
    }

    public void RenderSurroundingChunks(Vector3 coords)
    {
        Vector3Int landCoords = GlobalToLandCoords(coords);
        Vector2Int centerIndex = LandToChunkIndex(landCoords);
        for (int x = centerIndex.x - renderDistance; x < centerIndex.x + renderDistance; x++)
        {
            int zRange = (int)Mathf.Sqrt(Mathf.Pow(renderDistance, 2) - Mathf.Pow(x, 2));
            for (int z = centerIndex.y - zRange; z < centerIndex.y + zRange; z++)
            {
                Vector2Int chunkIndex = new Vector2Int(x, z);
                if (chunkPool.IsInstantiated(chunkIndex)) continue;
                RenderChunk(chunkIndex);
            }
        }
    }

    public bool RemoveBlock(Vector3Int coords, bool spawnItem = false)
    {
        if (coords.y > chunkSizeY) return false;
        Chunk chunk = chunkPool[LandToChunkIndex(coords)].GetComponent<Chunk>();
        return chunk.RemoveBlock(LandToChunkCoords(coords), spawnItem);
    }

    public bool AddBlock(Vector3Int coords, short blockID, Quaternion rotation = default)
    {
        if (coords.y > chunkSizeY) return false;

        Vector2Int chunkIndex = LandToChunkIndex(coords);
        if (!chunkPool.IsInstantiated(chunkIndex)) RenderChunk(chunkIndex);
        Chunk chunk = chunkPool[chunkIndex].GetComponent<Chunk>();

        return chunk.AddBlock(coords, blockID, rotation);
    }

    // message[0] = (Land)return
    public void GetLandRefMsg(object[] message)
    {
        message[0] = this;
    }

    public GameObject GetChunk(Vector2Int chunkIndex)
    {
        return chunkPool[chunkIndex];
    }

    public short GetBlockID(Vector3Int coords)
    {
        if (coords.y > chunkSizeY) return 0;

        Vector2Int chunkIndex = LandToChunkIndex(coords);
        if (!chunkPool.IsInstantiated(chunkIndex)) return 0;
        Chunk chunk = chunkPool[chunkIndex].GetComponent<Chunk>();

        Vector3Int chunkPos = LandToChunkCoords(coords);
        return chunk.blockIDs[chunkPos.x, chunkPos.y, chunkPos.z];
    }

    public Block GetCustomBlock(Vector3Int coords)
    {
        if (coords.y > chunkSizeY) return null;

        Vector2Int chunkIndex = LandToChunkIndex(coords);
        if (!chunkPool.IsInstantiated(chunkIndex)) return null;
        Chunk chunk = chunkPool[chunkIndex].GetComponent<Chunk>();

        Vector3Int chunkPos = LandToChunkCoords(coords);
        return chunk.GetCustomBlock(chunkPos);
    }

    public Vector3Int GlobalToLandCoords(Vector3 coords)
    {
        return Vector3Int.FloorToInt(transform.InverseTransformPoint(coords));
    }

    public Vector3Int LandToChunkCoords(Vector3Int coords)
    {
        return new Vector3Int(coords.x % chunkSizeX, coords.y % chunkSizeY, coords.z % chunkSizeZ);
    }

    public Vector2Int LandToChunkIndex(Vector3Int coords)
    {
        return new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
    }    
}

public class ChunkPool
{
    private GameObject[] pool;
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

    public GameObject this[Vector2Int chunkIndex]
    {
        get => pool[inUse[chunkIndex]];
    }

    public ChunkPool(GameObject chunkPrefab, int size)
    {
        pool = new GameObject[size];
        for (int i = 0; i < size; i++)
        {
            pool[i] = GameObject.Instantiate(chunkPrefab);
            pool[i].SetActive(false);
            reuseQueue.Enqueue(i);
        }
    }

    ~ChunkPool()
    {
        for (int i = 0; i < size; i++)
        {
            if (pool[i] != null) GameObject.Destroy(pool[i]);
        }
    }

    public bool IsInstantiated(Vector2Int chunkIndex)
    {
        return inUse.ContainsKey(chunkIndex);
    }

    // should only be called after making sure a chunk is available for reuse
    public GameObject Instantiate(Vector2Int chunkIndex, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
    {
        int i = reuseQueue.Dequeue();
        inUse.Add(chunkIndex, i);

        pool[i].SetActive(true);
        pool[i].transform.position = position;
        pool[i].transform.rotation = rotation;
        pool[i].transform.parent = parent;

        return pool[i];
    }

    public void Destroy(Vector2Int chunkIndex)
    {
        int i = inUse[chunkIndex];

        pool[i].SetActive(false);

        reuseQueue.Enqueue(i);
        inUse.Remove(chunkIndex);
    }
}