using System.Collections;
using System.Collections.Generic;
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
    public Dictionary<Vector2Int, GameObject> chunks = new Dictionary<Vector2Int, GameObject>();

    private void Start()
    {
        transform.position = LandPosition;
    }

    public void InitChunk(Vector2Int coords)
    {
        chunks.Add(coords, Instantiate(chunkPrefab, new Vector3Int(coords.x * chunkSizeX, 0, coords.y * chunkSizeZ), transform.rotation, gameObject.transform));
        chunks[coords].GetComponent<Chunk>().resolution = resolution;
        chunks[coords].GetComponent<Chunk>().vertexLength = vertexLength;
        chunks[coords].GetComponent<Chunk>().sizeX = chunkSizeX;
        chunks[coords].GetComponent<Chunk>().sizeY = chunkSizeY;
        chunks[coords].GetComponent<Chunk>().sizeZ = chunkSizeZ;
        chunks[coords].GetComponent<Chunk>().WakeUp();
        return;
    }

    public bool RemoveBlock(Vector3Int coords, bool spawnItem = false)
    {
        if (coords.y > chunkSizeY) return false;
        Chunk chunk = chunks[new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ)].GetComponent<Chunk>();
        return chunk.RemoveBlock(LandToChunkCoords(coords), spawnItem);
    }

    public bool AddBlock(Vector3Int coords, short blockID, Quaternion rotation = default)
    {
        if (coords.y > chunkSizeY) return false;

        Vector2Int chunkIndex = new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
        if (!chunks.ContainsKey(chunkIndex))
            InitChunk(chunkIndex);
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();

        return chunk.AddBlock(coords, blockID, rotation);
    }

    // message[0] = (Land)return
    public void GetLandRef(object[] message)
    {
        message[0] = this;
    }

    public short GetBlockID(Vector3Int coords)
    {
        if (coords.y > chunkSizeY) return 0;

        Vector2Int chunkIndex = new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
        if (!chunks.ContainsKey(chunkIndex))
            return 0;
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();

        Vector3Int chunkPos = LandToChunkCoords(coords);
        return chunk.blockIDs[chunkPos.x, chunkPos.y, chunkPos.z];
    }

    public Block GetCustomBlock(Vector3Int coords)
    {
        if (coords.y > chunkSizeY) return null;

        Vector2Int chunkIndex = new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
        if (!chunks.ContainsKey(chunkIndex))
            return null;
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();

        Vector3Int chunkPos = LandToChunkCoords(coords);
        return chunk.GetCustomBlock(chunkPos);
    }

    public Vector3Int ConvertToLandCoords(Vector3 coords)
    {
        return Vector3Int.FloorToInt(transform.InverseTransformPoint(coords));
    }

    public Vector3Int LandToChunkCoords(Vector3Int coords)
    {
        return new Vector3Int(coords.x % chunkSizeX, coords.y % chunkSizeY, coords.z % chunkSizeZ);
    }
}
