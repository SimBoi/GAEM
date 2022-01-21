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
        chunks[coords].GetComponent<VoxelChunk>().resolution = resolution;
        chunks[coords].GetComponent<VoxelChunk>().vertexLength = vertexLength;
        chunks[coords].GetComponent<VoxelChunk>().land = this;
        chunks[coords].GetComponent<VoxelChunk>().sizeX = chunkSizeX;
        chunks[coords].GetComponent<VoxelChunk>().sizeY = chunkSizeY;
        chunks[coords].GetComponent<VoxelChunk>().sizeZ = chunkSizeZ;
        chunks[coords].GetComponent<VoxelChunk>().WakeUp();
        return;
    }

    public bool RemoveBlock(Vector3Int coords, bool spawnItem = false)
    {
        if (coords.y > chunkSizeY) return false;
        VoxelChunk chunk = chunks[new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ)].GetComponent<VoxelChunk>();
        return chunk.RemoveBlock(new Vector3Int(coords.x % chunkSizeX, coords.y, coords.z % chunkSizeZ), spawnItem);
    }

    public bool AddBlock(Vector3Int coords, short blockID, Quaternion rotation = default)
    {
        if (coords.y > chunkSizeY) return false;
        Vector2Int chunkIndex = new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
        if (!chunks.ContainsKey(chunkIndex))
            InitChunk(chunkIndex);
        VoxelChunk chunk = chunks[chunkIndex].GetComponent<VoxelChunk>();
        return chunk.AddBlock(new Vector3Int(coords.x % chunkSizeX, coords.y, coords.z % chunkSizeZ), blockID, rotation);
    }

    // message[0] = (Land)return
    public void GetLandRef(object[] message)
    {
        message[0] = this;
    }
}
