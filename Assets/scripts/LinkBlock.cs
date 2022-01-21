using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkBlock : Block
{
    public Network network = null;

    public virtual Network LinkNewNetwork()
    {
        return new Network();
    }

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int chunkPos)
    {
        LinkBlock spawnedItem = (LinkBlock)base.PlaceCustomBlock(globalPos, rotation, parentChunk, chunkPos);

        bool relinkNetwork = false;
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborPos = chunkPos + Chunk.FaceToDirection(face);
            if (spawnedItem.blockID == parentChunk.blockIDs[neighborPos.x, neighborPos.y, neighborPos.z])
                relinkNetwork = true;
        }

        if (relinkNetwork)
            spawnedItem.RelinkNetwork(chunkPos);
        else
            spawnedItem.RelinkNetwork(chunkPos, LinkNewNetwork());

        return spawnedItem;
    }

    public void RelinkNetwork(Vector3Int chunkPos, Network targetNetwork = null)
    {
        if (network != null && ReferenceEquals(targetNetwork, network))
            return;
        if (targetNetwork != null)
            network = targetNetwork;

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborPos = chunkPos + Chunk.FaceToDirection(face);
            Block neighborBlock = parentChunk.GetCustomBlock(neighborPos);
            if (neighborBlock != null)
            {
                if (blockID == neighborBlock.blockID)
                {
                    if (targetNetwork == null)
                    {
                        targetNetwork = ((LinkBlock)neighborBlock).network;
                        network = targetNetwork;
                    }
                    else
                    {
                        ((LinkBlock)neighborBlock).RelinkNetwork(neighborPos, targetNetwork);
                    }
                }
                else if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    ((Machine)neighborBlock).TryLinkNetwork(Chunk.GetOppositeFace(face), targetNetwork);
                }
            }
        }
    }
}