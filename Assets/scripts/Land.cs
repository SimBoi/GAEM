using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class Land : NetworkBehaviour
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
    
    // should only be called on the server
    public void InitChunk(Vector2Int coords)
    {
        Chunk chunk = Instantiate(chunkPrefab, new Vector3Int(coords.x * chunkSizeX, 0, coords.y * chunkSizeZ), transform.rotation).GetComponent<Chunk>();
        chunk.Initialize(resolution, vertexLength, chunkSizeX, chunkSizeY, chunkSizeZ, coords);
        chunk.NetworkSpawn();
        chunks.Add(coords, chunk.gameObject);
        chunks[coords].transform.SetParent(transform);
        InitChunkClientRpc(coords, chunk.gameObject);
    }

    [ClientRpc]
    public void InitChunkClientRpc(Vector2 coordsFloats, NetworkObjectReference chunkRef, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        Vector2Int coords = Vector2Int.FloorToInt(coordsFloats);
        chunkRef.TryGet(out NetworkObject chunk);
        chunks.Add(coords, chunk.gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer) NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    public void OnClientConnected(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };
        foreach (var entry in chunks) InitChunkClientRpc(entry.Key, entry.Value, clientRpcParams);
    }

    // should only be called on the server
    public bool RemoveBlock(Vector3Int coords, bool spawnItem = false)
    {
        if (coords.y > chunkSizeY) return false;
        Chunk chunk = chunks[new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ)].GetComponent<Chunk>();
        return chunk.RemoveBlock(LandToChunkCoords(coords), spawnItem);
    }

    // should only be called on the server
    public bool AddBlock(Vector3Int coords, short blockID, Quaternion rotation = default)
    {
        if (!IsServer || coords.y > chunkSizeY) return false;

        Vector2Int chunkIndex = new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
        if (!chunks.ContainsKey(chunkIndex)) InitChunk(chunkIndex);
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();

        return chunk.AddBlock(coords, blockID, rotation);
    }

    // should only be called on the server
    public void SetLand(Vector2Int startChunk, short[,,] blockIDs, int sizeX, int sizeY, int sizeZ)
    {
        if (!IsServer || sizeX % chunkSizeX != 0 || sizeY % chunkSizeY != 0 || sizeZ % chunkSizeZ != 0) return;

        int chunksX = sizeX / chunkSizeX;
        int chunksY = sizeY / chunkSizeY;
        int chunksZ = sizeZ / chunkSizeZ;
        
        for (int x = 0; x < chunksX; x++)
        for (int y = 0; y < chunksY; y++)
        for (int z = 0; z < chunksZ; z++)
        {
            short[,,] chunkBlockIDs = new short[chunkSizeX, chunkSizeY, chunkSizeZ];
            for (int i = 0; i < chunkSizeX; i++)
            for (int j = 0; j < chunkSizeY; j++)
            for (int k = 0; k < chunkSizeZ; k++)
            {
                chunkBlockIDs[i, j, k] = blockIDs[x * chunkSizeX + i, y * chunkSizeY + j, z * chunkSizeZ + k];
            }
            Vector2Int chunkIndex = new Vector2Int(startChunk.x + x, startChunk.y + z);
            if (!chunks.ContainsKey(chunkIndex)) InitChunk(chunkIndex);
            chunks[chunkIndex].GetComponent<Chunk>().OverrideChunk(chunkBlockIDs);
        }
    }

    // message[0] = (Land)return
    public void GetLandRefMsg(object[] message)
    {
        message[0] = this;
    }

    public short GetBlockID(Vector3Int coords)
    {
        if (coords.y > chunkSizeY) return 0;

        Vector2Int chunkIndex = new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
        if (!chunks.ContainsKey(chunkIndex)) return 0;
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();

        Vector3Int chunkPos = LandToChunkCoords(coords);
        return chunk.blockIDs[chunkPos.x, chunkPos.y, chunkPos.z];
    }

    public Block GetCustomBlock(Vector3Int coords)
    {
        if (coords.y > chunkSizeY) return null;

        Vector2Int chunkIndex = new Vector2Int(coords.x / chunkSizeX, coords.z / chunkSizeZ);
        if (!chunks.ContainsKey(chunkIndex)) return null;
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
