using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum Faces
{
    Up,
    Down,
    Right,
    Left,
    Front,
    Back
}

public class Chunk : NetworkBehaviour
{
    public BlockToItemID blockToItemID;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public int sizeX = 16;
    public int sizeY = 16;
    public int sizeZ = 16;
    public Vector2Int chunkIndex;
    public short[,,] blockIDs;
    public bool requiresMeshGeneration = false;
    public int vertexLength;
    public Vector2Int resolution;
    public Dictionary<Vector3Int, Block> customBlocks = new Dictionary<Vector3Int, Block>();

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

    public void Initialize(Vector2Int resolution, int vertexLength, int sizeX, int sizeY, int sizeZ, Vector2Int chunkIndex)
    {
        this.resolution = resolution;
        this.vertexLength = vertexLength;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        this.chunkIndex = chunkIndex;
        this.meshRenderer = GetComponent<MeshRenderer>();
        this.meshCollider = GetComponent<MeshCollider>();
        this.meshFilter = GetComponent<MeshFilter>();
        this.blockIDs = new short[sizeX, sizeY, sizeZ];
    }

    public void NetworkSpawn()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        GetComponent<NetworkObject>().Spawn();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            InitializeClientRpc(resolution, vertexLength, sizeX, sizeY, sizeZ, chunkIndex);
        }
    }

    public void OnClientConnected(ulong clientID)
    {
        InitializeClientRpc(resolution, vertexLength, sizeX, sizeY, sizeZ, chunkIndex, SerializeBlockIDs(blockIDs, sizeX, sizeY, sizeZ), GetCustomBlockRefs());
    }

    [ClientRpc]
    public void InitializeClientRpc(Vector2 resolutionFloats, int vertexLength, int sizeX, int sizeY, int sizeZ, Vector2 chunkIndexFloats, short[] serializedBlockIDs = null, NetworkBehaviourReference[] customBlockRefs = null)
    {
        if (IsServer) return;
        Initialize(Vector2Int.FloorToInt(resolutionFloats), vertexLength, sizeX, sizeY, sizeZ, Vector2Int.FloorToInt(chunkIndexFloats));
        if (serializedBlockIDs != null) OverrideChunkLocal(DeserializeBlockIDs(serializedBlockIDs, sizeX, sizeY, sizeZ), customBlockRefs);
    }

    private void Update()
    {
        if (requiresMeshGeneration) GenerateMesh();
    }

    void GenerateMesh()
    {
        Mesh newMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        int currentIndex = 0;

        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    if (blockIDs[x, y, z] == 0) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (!Item.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
                    {
                        GenerateBlock_Top(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Up), pos);
                        GenerateBlock_Right(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Right), pos);
                        GenerateBlock_Left(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Left), pos);
                        GenerateBlock_Forward(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Front), pos);
                        GenerateBlock_Back(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Back), pos);
                        GenerateBlock_Bottom(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(blockIDs[x, y, z], Faces.Down), pos);
                    }
                }
            }
        }

        newMesh.SetVertices(vertices);
        newMesh.SetNormals(normals);
        newMesh.SetUVs(0, uvs);
        newMesh.SetIndices(indices, MeshTopology.Triangles, 0);
        newMesh.RecalculateTangents();

        meshFilter.mesh = newMesh;
        meshCollider.sharedMesh = newMesh;
        // Set Texture

        requiresMeshGeneration = false;
    }

    void GenerateBlock_Top(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        //manyake
        if (pos.y + 1 < sizeY && blockIDs[pos.x, pos.y + 1, pos.z] != 0 && !Item.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y + 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 1, 1) + offset);
        vertices.Add(new Vector3(1, 1, 1) + offset);
        vertices.Add(new Vector3(1, 1, 0) + offset);
        vertices.Add(new Vector3(0, 1, 0) + offset);

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Right(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        if (pos.x + 1 < sizeX && blockIDs[pos.x + 1, pos.y, pos.z] != 0 && !Item.prefabs[blockToItemID.Convert(blockIDs[pos.x + 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(1, 1, 0) + offset);
        vertices.Add(new Vector3(1, 1, 1) + offset);
        vertices.Add(new Vector3(1, 0, 1) + offset);
        vertices.Add(new Vector3(1, 0, 0) + offset);

        normals.Add(Vector3.right);
        normals.Add(Vector3.right);
        normals.Add(Vector3.right);
        normals.Add(Vector3.right);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Left(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        if (pos.x - 1 >= 0 && blockIDs[pos.x - 1, pos.y, pos.z] != 0 && !Item.prefabs[blockToItemID.Convert(blockIDs[pos.x - 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 1, 1) + offset);
        vertices.Add(new Vector3(0, 1, 0) + offset);
        vertices.Add(new Vector3(0, 0, 0) + offset);
        vertices.Add(new Vector3(0, 0, 1) + offset);

        normals.Add(Vector3.left);
        normals.Add(Vector3.left);
        normals.Add(Vector3.left);
        normals.Add(Vector3.left);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Forward(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        if (pos.z + 1 < sizeZ && blockIDs[pos.x, pos.y, pos.z + 1] != 0 && !Item.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z + 1])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(1, 1, 1) + offset);
        vertices.Add(new Vector3(0, 1, 1) + offset);
        vertices.Add(new Vector3(0, 0, 1) + offset);
        vertices.Add(new Vector3(1, 0, 1) + offset);

        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);
        normals.Add(Vector3.forward);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Back(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        if (pos.z - 1 >= 0 && blockIDs[pos.x, pos.y, pos.z - 1] != 0 && !Item.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z - 1])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 1, 0) + offset);
        vertices.Add(new Vector3(1, 1, 0) + offset);
        vertices.Add(new Vector3(1, 0, 0) + offset);
        vertices.Add(new Vector3(0, 0, 0) + offset);

        normals.Add(Vector3.back);
        normals.Add(Vector3.back);
        normals.Add(Vector3.back);
        normals.Add(Vector3.back);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    void GenerateBlock_Bottom(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        if (pos.y - 1 >= 0 && blockIDs[pos.x, pos.y - 1, pos.z] != 0 && !Item.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y - 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 0, 0) + offset);
        vertices.Add(new Vector3(1, 0, 0) + offset);
        vertices.Add(new Vector3(1, 0, 1) + offset);
        vertices.Add(new Vector3(0, 0, 1) + offset);

        normals.Add(Vector3.down);
        normals.Add(Vector3.down);
        normals.Add(Vector3.down);
        normals.Add(Vector3.down);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);
        currentIndex += 4;
    }

    public void OverrideChunkLocal(short[,,] blockIDs, NetworkBehaviourReference[] customBlockRefs = null)
    {
        Vector3Int chunkPos = new Vector3Int(chunkIndex.x * sizeX, 0, chunkIndex.y * sizeZ);
        int i = 0;
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (IsServer)
                    {
                        OverrideBlockLocal(chunkPos + pos, blockIDs[x, y, z]);
                    }
                    else
                    {
                        Block block = blockIDs[x, y, z] == 0 ? null : Item.prefabs[blockToItemID.Convert(blockIDs[x, y, z])].GetComponent<Block>();
                        Block customBlock = null;
                        // use the NetworkBehaviourReference passed by the server to find the custom block that the server spawned
                        if (block != null && block.hasCustomMesh) customBlockRefs[i++].TryGet(out customBlock);
                        OverrideBlockLocal(chunkPos + pos, blockIDs[x, y, z], customBlock);
                    }
                }
            }
        }
    }

    // should only be called on the server
    public void OverrideChunk(short[,,] blockIDs)
    {
        OverrideChunkLocal(blockIDs);
        if (IsSpawned) SyncChunkClientRpc(SerializeBlockIDs(blockIDs, sizeX, sizeY, sizeZ), GetCustomBlockRefs());
    }

    [ServerRpc(RequireOwnership = false)]
    public void SyncChunkServerRpc(ulong clientID)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientID }
            }
        };
        SyncChunkClientRpc(SerializeBlockIDs(blockIDs, sizeX, sizeY, sizeZ), GetCustomBlockRefs(), clientRpcParams);
    }

    [ClientRpc]
    public void SyncChunkClientRpc(short[] serializedBlockIDs, NetworkBehaviourReference[] customBlockRefs, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        OverrideChunkLocal(DeserializeBlockIDs(serializedBlockIDs, sizeX, sizeY, sizeZ), customBlockRefs);
    }

    public void OverrideBlockLocal(Vector3Int landPos, short blockID, bool spawnItem = false, Block customBlock = null, Quaternion rotation = default)
    {
        Vector3Int pos = new Vector3Int(landPos.x % sizeX, landPos.y % sizeY, landPos.z % sizeZ);
        short prevID = blockIDs[pos.x, pos.y, pos.z];
        blockIDs[pos.x, pos.y, pos.z] = blockID;
        Block block = blockID == 0 ? null : Item.prefabs[blockToItemID.Convert(blockID)].GetComponent<Block>();

        // determine if we have to regenerate the mesh
        if (!((prevID == 0 || customBlocks.ContainsKey(pos)) && (blockID == 0 || block.hasCustomMesh))) requiresMeshGeneration = true;

        // remove prevoius custom block if it exists and spawn it
        if (customBlocks.ContainsKey(pos))
        {
            if (IsServer)
            {
                Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
                customBlocks[pos].BreakCustomBlock(out Block spawnedItem, spawnPos, spawnItem);
                if (spawnItem) spawnedItem.NetworkSpawn();
            }
            customBlocks.Remove(pos);
        }
        // spawn previous normal Item if it exists
        else if (prevID != 0 && spawnItem == true)
        {
            GameObject newItem;
            Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
            newItem = Instantiate(Item.prefabs[blockToItemID.Convert(prevID)], spawnPos, default(Quaternion));
            Item spawnedItem = newItem.GetComponent<Item>();
            spawnedItem.SetStackSize(1);
            spawnedItem.NetworkSpawn();
        }

        // spawn the new custom block if on the server, use the passed custom block if on a client
        if (blockID != 0 && block.hasCustomMesh)
        {
            Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
            if (IsServer) customBlock = (Block)block.PlaceCustomBlock(spawnPos, rotation, this, landPos);
            Debug.Log(customBlock.transform.name);
            customBlock.InitializeCustomBlock(spawnPos, rotation, this, landPos);
            customBlocks.Add(pos, customBlock);
        }
    }

    // should only be called on the server
    public bool AddBlock(Vector3Int landPos, short blockID, Quaternion rotation = default)
    {
        Vector3Int pos = new Vector3Int(landPos.x % sizeX, landPos.y % sizeY, landPos.z % sizeZ);
        if (blockIDs[pos.x, pos.y, pos.z] != 0) return false;
        OverrideBlockLocal(landPos, blockID, false, null, rotation);
        NetworkBehaviourReference customBlockRef = customBlocks.ContainsKey(pos) ? customBlocks[pos] : default(NetworkBehaviourReference);
        SyncBlockClientRpc(landPos, blockID, customBlockRef, rotation);
        return true;
    }

    // should only be called on the server
    public bool RemoveBlock(Vector3Int pos, bool spawnItem = false)
    {
        if (blockIDs[pos.x, pos.y, pos.z] == 0) return false;
        Vector3Int landPos = new Vector3Int(chunkIndex.x * sizeX + pos.x, pos.y, chunkIndex.y * sizeZ + pos.z);
        OverrideBlockLocal(landPos, 0, spawnItem);
        SyncBlockClientRpc(landPos, 0);
        return true;
    }

    [ClientRpc]
    public void SyncBlockClientRpc(Vector3 landPosFloats, short blockID, NetworkBehaviourReference customBlockRef = default, Quaternion rotation = default)
    {
        if (IsServer) return;
        Vector3Int landPos = Vector3Int.FloorToInt(landPosFloats);
        Block block = blockID == 0 ? null : Item.prefabs[blockToItemID.Convert(blockID)].GetComponent<Block>();
        Block customBlock = null;
        // use the NetworkBehaviourReference passed by the server to find the custom block that the server spawned
        if (blockID != 0 && block.hasCustomMesh) customBlockRef.TryGet(out customBlock);
        OverrideBlockLocal(landPos, blockID, false, customBlock, rotation);
    }

    public float GetStiffness(Vector3Int pos)
    {
        return Item.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().stiffness;
    }

    public Rect GetFaceTexture(short blockID, Faces face)
    {
        return new Rect((float)face * vertexLength / resolution[0], (float)blockID * vertexLength / resolution[1], (float)vertexLength / resolution[0], (float)vertexLength / resolution[1]);
    }

    public Block GetCustomBlock(Vector3Int pos)
    {
        return customBlocks.ContainsKey(pos) ? customBlocks[pos] : null;
    }

    public NetworkBehaviourReference[] GetCustomBlockRefs()
    {
        List<NetworkBehaviourReference> customBlockRefs = new List<NetworkBehaviourReference>();
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (customBlocks.ContainsKey(pos)) customBlockRefs.Add(customBlocks[pos]);
                }
            }
        }
        return customBlockRefs.ToArray();
    }

    public short[] SerializeBlockIDs(short[,,] blockIDs, int sizeX, int sizeY, int sizeZ)
    {
        short[] serializedBlockIDs = new short[sizeX * sizeY * sizeZ];
        int i = 0;
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    serializedBlockIDs[i++] = blockIDs[x, y, z];
                }
            }
        }
        return serializedBlockIDs;
    }

    public short[,,] DeserializeBlockIDs(short[] serializedBlockIDs, int sizeX, int sizeY, int sizeZ)
    {
        short[,,] blockIDs = new short[sizeX, sizeY, sizeZ];
        int i = 0;
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    blockIDs[x, y, z] = serializedBlockIDs[i++];
                }
            }
        }
        return blockIDs;
    }
}