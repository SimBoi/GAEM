using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkBlock : Block
{
    public LinkNetwork network;

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, VoxelChunk parentChunk, Vector3Int chunkPos)
    {
        LinkBlock spawnedItem = (LinkBlock)base.PlaceCustomBlock(globalPos, rotation, parentChunk, chunkPos);

        bool relinkNetwork = false;
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborPos = chunkPos + Vector3Int.FloorToInt(parentChunk.FaceToDirection(face));
            if (spawnedItem.blockID == parentChunk.blockIDs[neighborPos.x, neighborPos.y, neighborPos.z])
                relinkNetwork = true;
        }

        if (relinkNetwork)
            spawnedItem.RelinkNetwork(chunkPos);
        else
            spawnedItem.network = new LinkNetwork();

        return spawnedItem;
    }

    public void RelinkNetwork(Vector3Int chunkPos, LinkNetwork targetNetwork = null)
    {
        if (network != null && ReferenceEquals(targetNetwork, network))
            return;
        if (targetNetwork != null)
        {
            network = targetNetwork;
            Debug.Log("changed Networks for" + chunkPos.x + " " + chunkPos.y + " " + chunkPos.z);
        }

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborPos = chunkPos + Vector3Int.FloorToInt(parentChunk.FaceToDirection(face));
            Block neighborBlock = parentChunk.GetCustomBlock(neighborPos);
            if (neighborBlock != null && blockID == neighborBlock.blockID)
            {
                if (targetNetwork == null)
                {
                    targetNetwork = ((LinkBlock)neighborBlock).network;
                }
                else
                {
                    ((LinkBlock)neighborBlock).RelinkNetwork(neighborPos, targetNetwork);
                }
            }
        }
    }
}