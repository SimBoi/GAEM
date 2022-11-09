using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public VoxelGrid voxelGrid;
    public Vector2Int chunkIndex;
    public bool requiresMeshGeneration = false;

    //private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private void Awake()
    {
        //meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshFilter = GetComponent<MeshFilter>();

        requiresMeshGeneration = true;
    }

    private void LateUpdate()
    {
        if (requiresMeshGeneration) GenerateMesh();
    }

    void GenerateMesh()
    {
        if (!voxelGrid.blockIDs.ContainsKey(chunkIndex))
        {
            meshFilter.mesh = null;
            meshCollider.sharedMesh = null;
            requiresMeshGeneration = false;
            return;
        }

        Mesh newMesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> indices = new List<int>();

        int currentIndex = 0;

        for (int x = 0; x < voxelGrid.chunkSizeX; x++)
        {
            for (int y = 0; y < voxelGrid.chunkSizeY; y++)
            {
                for (int z = 0; z < voxelGrid.chunkSizeZ; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    if (voxelGrid.blockIDs[chunkIndex][x, y, z] == 0) continue;

                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (!voxelGrid.itemPrefabs.prefabs[voxelGrid.blockToItemID.Convert(voxelGrid.blockIDs[chunkIndex][pos.x, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh)
                    {
                        GenerateBlock_Top(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(voxelGrid.blockIDs[chunkIndex][x, y, z], Faces.Up), pos);
                        GenerateBlock_Right(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(voxelGrid.blockIDs[chunkIndex][x, y, z], Faces.Right), pos);
                        GenerateBlock_Left(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(voxelGrid.blockIDs[chunkIndex][x, y, z], Faces.Left), pos);
                        GenerateBlock_Forward(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(voxelGrid.blockIDs[chunkIndex][x, y, z], Faces.Front), pos);
                        GenerateBlock_Back(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(voxelGrid.blockIDs[chunkIndex][x, y, z], Faces.Back), pos);
                        GenerateBlock_Bottom(ref currentIndex, offset, vertices, normals, uvs, indices, GetFaceTexture(voxelGrid.blockIDs[chunkIndex][x, y, z], Faces.Down), pos);
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
        if (pos.y + 1 < voxelGrid.chunkSizeY && voxelGrid.blockIDs[chunkIndex][pos.x, pos.y + 1, pos.z] != 0 && !voxelGrid.itemPrefabs.prefabs[voxelGrid.blockToItemID.Convert(voxelGrid.blockIDs[chunkIndex][pos.x, pos.y + 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.x + 1 < voxelGrid.chunkSizeX && voxelGrid.blockIDs[chunkIndex][pos.x + 1, pos.y, pos.z] != 0 && !voxelGrid.itemPrefabs.prefabs[voxelGrid.blockToItemID.Convert(voxelGrid.blockIDs[chunkIndex][pos.x + 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.x - 1 >= 0 && voxelGrid.blockIDs[chunkIndex][pos.x - 1, pos.y, pos.z] != 0 && !voxelGrid.itemPrefabs.prefabs[voxelGrid.blockToItemID.Convert(voxelGrid.blockIDs[chunkIndex][pos.x - 1, pos.y, pos.z])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.z + 1 < voxelGrid.chunkSizeZ && voxelGrid.blockIDs[chunkIndex][pos.x, pos.y, pos.z + 1] != 0 && !voxelGrid.itemPrefabs.prefabs[voxelGrid.blockToItemID.Convert(voxelGrid.blockIDs[chunkIndex][pos.x, pos.y, pos.z + 1])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.z - 1 >= 0 && voxelGrid.blockIDs[chunkIndex][pos.x, pos.y, pos.z - 1] != 0 && !voxelGrid.itemPrefabs.prefabs[voxelGrid.blockToItemID.Convert(voxelGrid.blockIDs[chunkIndex][pos.x, pos.y, pos.z - 1])].GetComponent<Block>().hasCustomMesh) return;
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
        if (pos.y - 1 >= 0 && voxelGrid.blockIDs[chunkIndex][pos.x, pos.y - 1, pos.z] != 0 && !voxelGrid.itemPrefabs.prefabs[voxelGrid.blockToItemID.Convert(voxelGrid.blockIDs[chunkIndex][pos.x, pos.y - 1, pos.z])].GetComponent<Block>().hasCustomMesh) return;
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

    private Rect GetFaceTexture(short blockID, Faces face)
    {
        return new Rect((float)face * voxelGrid.vertexLength / voxelGrid.resolution[0], (float)blockID * voxelGrid.vertexLength / voxelGrid.resolution[1], (float)voxelGrid.vertexLength / voxelGrid.resolution[0], (float)voxelGrid.vertexLength / voxelGrid.resolution[1]);
    }
}