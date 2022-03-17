using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Land : MonoBehaviour
{
    public int vertexLength;
    public Vector2Int resolution;
    public int chunkSizeX = 16;
    public int chunkSizeY = 16;
    public int chunkSizeZ = 16;
    public GameObject chunkPrefab;
    public Dictionary<Vector3Int, GameObject> chunks = new Dictionary<Vector3Int, GameObject>();

    public void InitChunk(Vector3Int coords)
    {
        chunks.Add(coords, Instantiate(chunkPrefab, transform.position + new Vector3Int(coords.x * chunkSizeX, coords.y * chunkSizeY, coords.z * chunkSizeZ), transform.rotation, gameObject.transform));
        chunks[coords].GetComponent<Chunk>().resolution = resolution;
        chunks[coords].GetComponent<Chunk>().vertexLength = vertexLength;
        chunks[coords].GetComponent<Chunk>().sizeX = chunkSizeX;
        chunks[coords].GetComponent<Chunk>().sizeY = chunkSizeY;
        chunks[coords].GetComponent<Chunk>().sizeZ = chunkSizeZ;
        chunks[coords].GetComponent<Chunk>().parentLand = this;
        chunks[coords].GetComponent<Chunk>().WakeUp();
        return;
    }

    public bool RemoveBlock(Vector3Int coords, bool spawnItem = false)
    {
        Vector3Int chunkIndex = new Vector3Int(CDiv(coords.x , chunkSizeX), CDiv(coords.y , chunkSizeY), CDiv(coords.z , chunkSizeZ));
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();
        bool result = chunk.RemoveBlock(LandToChunkCoords(coords), spawnItem);
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborBlockCoords = Chunk.FaceToDirection(face) + coords;
            Vector3Int neighborChunkIndex = new Vector3Int(CDiv(neighborBlockCoords.x, chunkSizeX), CDiv(neighborBlockCoords.y, chunkSizeY), CDiv(neighborBlockCoords.z, chunkSizeZ));
            if (neighborChunkIndex == chunkIndex) continue;
            Chunk neighborChunk = chunks[neighborChunkIndex].GetComponent<Chunk>();
            neighborChunk.requiresMeshGeneration = true;
        }

        return result;
    }

    public bool AddBlock(Vector3Int coords, short blockID, Quaternion rotation = default, bool generateMesh = true)
    {
        Vector3Int chunkIndex = new Vector3Int(CDiv(coords.x, chunkSizeX), CDiv(coords.y, chunkSizeY), CDiv(coords.z, chunkSizeZ));
        if (!chunks.ContainsKey(chunkIndex))
            InitChunk(chunkIndex);
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();

        bool result = chunk.AddBlock(coords, blockID, rotation, generateMesh);
        if (generateMesh)
        {
            foreach (Faces face in Enum.GetValues(typeof(Faces)))
            {
                Vector3Int neighborBlockCoords = Chunk.FaceToDirection(face) + coords;
                Vector3Int neighborChunkIndex = new Vector3Int(CDiv(neighborBlockCoords.x, chunkSizeX), CDiv(neighborBlockCoords.y, chunkSizeY), CDiv(neighborBlockCoords.z, chunkSizeZ));
                if (neighborChunkIndex == chunkIndex) continue;
                Chunk neighborChunk = chunks[neighborChunkIndex].GetComponent<Chunk>();
                neighborChunk.requiresMeshGeneration = true;
            }
        }
        return result;
    }

    public void regenerateMesh()
    {
        foreach (KeyValuePair<Vector3Int, GameObject> chunk in chunks)
        {
            chunk.Value.GetComponent<Chunk>().requiresMeshGeneration = true;
        }
    }

   // message[0] = (Land)return
    public void GetLandRefMsg(object[] message)
    {
        message[0] = this;
    }

    public short GetBlockID(Vector3Int coords)
    {
        Vector3Int chunkIndex = new Vector3Int(CDiv(coords.x, chunkSizeX), CDiv(coords.y, chunkSizeY), CDiv(coords.z, chunkSizeZ));
        if (!chunks.ContainsKey(chunkIndex))
            return 0;
        Chunk chunk = chunks[chunkIndex].GetComponent<Chunk>();

        Vector3Int chunkPos = LandToChunkCoords(coords);
        return chunk.blockIDs[chunkPos.x, chunkPos.y, chunkPos.z];
    }

    public Block GetCustomBlock(Vector3Int coords)
    {
        if (coords.y > chunkSizeY) return null;

        Vector3Int chunkIndex = new Vector3Int(CDiv(coords.x, chunkSizeX), CDiv(coords.y, chunkSizeY), CDiv(coords.z, chunkSizeZ));
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
        return new Vector3Int(CMod(coords.x, chunkSizeX), CMod(coords.y, chunkSizeY), CMod(coords.z, chunkSizeZ));
    }

    int CMod(int x, int m)
    {
        return (x % m + m) % m;
    }

    int CDiv(int x, int m)
    {
        int result = x / m;
        if (x * m < 0)
        {
            result -= 1;
        }
        return result;
    }
}