using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Faces
{
    Up,
    Down,
    Right,
    Left,
    Front,
    Back
}

public class Chunk : MonoBehaviour
{
    public ItemPrefabs itemPrefabs;
    public BlockToItemID blockToItemID;
    public MeshRenderer meshRenderer;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;
    public int sizeX = 16;
    public int sizeY = 16;
    public int sizeZ = 16;
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

    public void WakeUp()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();

        blockIDs = new short[sizeX, sizeY, sizeZ];

        requiresMeshGeneration = true;
    }

    private void Update()
    {
        if (requiresMeshGeneration)
        {
            GenerateMesh();
        }
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
                    if (blockIDs[x, y, z] == 0) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (!itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
                    {
                        Vector3Int offset = new Vector3Int(x, y, z);
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
        if (pos.y + 1 < sizeY && blockIDs[pos.x, pos.y + 1, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y + 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
        vertices.Add(new Vector3(0, 1, 1) + offset);
        vertices.Add(new Vector3(1, 1, 1) + offset);
        vertices.Add(new Vector3(1, 1, 0) + offset);
        vertices.Add(new Vector3(0, 1, 0) + offset);
        vertices.Add(new Vector3(Random.Range(0.2f, 0.8f), Random.Range(0.8f, 1.1f), Random.Range(0.2f, 0.8f)) + offset);

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMax));
        uvs.Add(new Vector2(blockUVs.xMax, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));
        uvs.Add(new Vector2(blockUVs.xMin, blockUVs.yMin));

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 4);

        indices.Add(currentIndex + 1);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 4);

        indices.Add(currentIndex + 4);
        indices.Add(currentIndex + 2);
        indices.Add(currentIndex + 3);

        indices.Add(currentIndex + 0);
        indices.Add(currentIndex + 4);
        indices.Add(currentIndex + 3);
        currentIndex += 5;
    }

    void GenerateBlock_Right(ref int currentIndex, Vector3Int offset, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> indices, Rect blockUVs, Vector3Int pos)
    {
        if (pos.x + 1 < sizeX && blockIDs[pos.x + 1, pos.y, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x + 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.x - 1 >= 0 && blockIDs[pos.x - 1, pos.y, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x - 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.z + 1 < sizeZ && blockIDs[pos.x, pos.y, pos.z + 1] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z + 1])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.z - 1 >= 0 && blockIDs[pos.x, pos.y, pos.z - 1] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z - 1])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.y - 1 >= 0 && blockIDs[pos.x, pos.y - 1, pos.z] != 0 && !itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y - 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
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

    public bool RemoveBlock(Vector3Int pos, bool spawnItem = false)
    {
        if (blockIDs[pos.x, pos.y, pos.z] != 0)
        {
            if (customBlocks.ContainsKey(pos))
            {
                Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
                customBlocks[pos].BreakCustomBlock(spawnPos, spawnItem);
                customBlocks.Remove(pos);
            }
            else
            {
                if (spawnItem == true)
                {
                    GameObject newItem;
                    Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
                    newItem = Instantiate(itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])], spawnPos, default(Quaternion));
                    Item spawnedItem = newItem.GetComponent<Item>();
                    spawnedItem.SetStackSize(1);
                }
                requiresMeshGeneration = true;
            }

            blockIDs[pos.x, pos.y, pos.z] = 0;
            return true;
        }
        return false;
    }

    public bool AddBlock(Vector3Int landPos, short blockID, Quaternion rotation = default, bool generateMesh = true)
    {
        Vector3Int pos = new Vector3Int(landPos.x % sizeX, landPos.y % sizeY, landPos.z % sizeZ);
        if (blockIDs[pos.x, pos.y, pos.z] == 0)
        {
            if (blockID == 0) return true;

            blockIDs[pos.x, pos.y, pos.z] = blockID;
            if (itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
            {
                Vector3 spawnPos = transform.TransformPoint(pos + new Vector3(0.5f, 0.5f, 0.5f));
                Block customBlock = (Block)itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().PlaceCustomBlock(spawnPos, rotation, this, landPos);
                customBlocks.Add(pos, customBlock);
            }
            else if (generateMesh)
            {
                requiresMeshGeneration = true;
            }
            return true;
        }
        return false;
    }

    public float GetStiffness(Vector3Int pos)
    {
        return itemPrefabs.prefabs[blockToItemID.Convert(blockIDs[pos.x, pos.y, pos.z])].GetComponent<Block>().stiffness;
    }

    public Rect GetFaceTexture(short blockID, Faces face)
    {
        return new Rect((float)face * vertexLength / resolution[0], (float)blockID * vertexLength / resolution[1], (float)vertexLength / resolution[0], (float)vertexLength / resolution[1]);
    }

    public Block GetCustomBlock(Vector3Int pos)
    {
        if (customBlocks.ContainsKey(pos))
            return customBlocks[pos];
        else
            return null;
    }
}