using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinkBlock : Block
{
    public Network network = null;

    public virtual Network CreateNewNetwork()
    {
        return Network.CreateNewNetwork();
    }

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int landPos)
    {
        LinkBlock spawnedItem = (LinkBlock)base.PlaceCustomBlock(globalPos, rotation, parentChunk, landPos);

        object[] message = new object[1] { null };
        parentChunk.SendMessageUpwards("GetLandRefMsg", message);
        Land land = (Land)message[0];

        bool relinkNetwork = false;
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborLandPos = landPos + Chunk.FaceToDirection(face);
            if (spawnedItem.blockID == land.GetBlockID(neighborLandPos)) relinkNetwork = true;
        }

        if (relinkNetwork) spawnedItem.RelinkNetwork(land, landPos);
        else spawnedItem.RelinkNetwork(land, landPos, CreateNewNetwork());

        return spawnedItem;
    }

    public override bool BreakCustomBlock(out Block spawnedItem, Vector3 pos = default, bool spawnItem = false)
    {
        if (!base.BreakCustomBlock(out spawnedItem, pos, spawnItem)) return false;
        UnlinkNetwork();
        return true;
    }

    public void RelinkNetwork(Land land, Vector3Int landPos, Network targetNetwork = null)
    {
        if (isDestroyed) return;
        if (network != null && ReferenceEquals(targetNetwork, network)) return;

        if (targetNetwork == null)
        {
            foreach (Faces face in Enum.GetValues(typeof(Faces)))
            {
                Vector3Int neighborLandPos = landPos + Chunk.FaceToDirection(face);
                Block neighborBlock = land.GetCustomBlock(neighborLandPos);
                if (neighborBlock != null && blockID == neighborBlock.blockID)
                {
                    if (blockID == neighborBlock.blockID)
                    {
                        targetNetwork = ((LinkBlock)neighborBlock).network;
                        break;
                    }
                }
            }
        }
        network = targetNetwork;

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborLandPos = landPos + Chunk.FaceToDirection(face);
            Block neighborBlock = land.GetCustomBlock(neighborLandPos);
            if (neighborBlock != null)
            {
                if (blockID == neighborBlock.blockID)
                {
                    ((LinkBlock)neighborBlock).RelinkNetwork(land, neighborLandPos, targetNetwork);
                }
                else if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    ((Machine)neighborBlock).TryLinkNetwork(Chunk.GetOppositeFace(face), targetNetwork);
                }
            }
        }
    }

    public void UnlinkNetwork()
    {
        object[] message = new object[1] { null };
        SendMessageUpwards("GetLandRefMsg", message);
        Land land = (Land)message[0];
        Vector3Int landPos = Vector3Int.FloorToInt(land.transform.InverseTransformPoint(transform.position));

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborLandPos = landPos + Chunk.FaceToDirection(face);
            Block neighborBlock = land.GetCustomBlock(neighborLandPos);
            if (neighborBlock != null)
            {
                if (blockID == neighborBlock.blockID)
                {
                    ((LinkBlock)neighborBlock).RelinkNetwork(land, neighborLandPos, CreateNewNetwork());
                }
                else if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    ((Machine)neighborBlock).UnlinkNetwork(Chunk.GetOppositeFace(face));
                }
            }
        }
    }
}