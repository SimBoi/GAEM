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

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, VoxelGrid voxelGrid, Vector3Int landPos)
    {
        LinkBlock spawnedItem = (LinkBlock)base.PlaceCustomBlock(globalPos, rotation, voxelGrid, landPos);

        bool relinkNetwork = false;
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborLandPos = landPos + VoxelGrid.FaceToDirection(face);
            if (spawnedItem.blockID == voxelGrid.GetBlockID(neighborLandPos))
                relinkNetwork = true;
        }

        if (relinkNetwork)
            spawnedItem.RelinkNetwork(voxelGrid, landPos);
        else
            spawnedItem.RelinkNetwork(voxelGrid, landPos, CreateNewNetwork());

        return spawnedItem;
    }

    public override bool BreakCustomBlock(Vector3 pos = default, bool spawnItem = false)
    {
        if (!base.BreakCustomBlock(pos, spawnItem))
            return false;
        UnlinkNetwork();
        return true;
    }

    public void RelinkNetwork(VoxelGrid land, Vector3Int landPos, Network targetNetwork = null)
    {
        if (isDestroyed)
            return;
        if (network != null && ReferenceEquals(targetNetwork, network))
            return;

        if (targetNetwork == null)
        {
            foreach (Faces face in Enum.GetValues(typeof(Faces)))
            {
                Vector3Int neighborLandPos = landPos + VoxelGrid.FaceToDirection(face);
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
            Vector3Int neighborLandPos = landPos + VoxelGrid.FaceToDirection(face);
            Block neighborBlock = land.GetCustomBlock(neighborLandPos);
            if (neighborBlock != null)
            {
                if (blockID == neighborBlock.blockID)
                {
                    ((LinkBlock)neighborBlock).RelinkNetwork(land, neighborLandPos, targetNetwork);
                }
                else if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    ((Machine)neighborBlock).TryLinkNetwork(VoxelGrid.GetOppositeFace(face), targetNetwork);
                }
            }
        }
    }

    public void UnlinkNetwork()
    {
        object[] message = new object[1]{
                null
            };
        SendMessageUpwards("GetLandRefMsg", message);
        VoxelGrid land = (VoxelGrid)message[0];
        Vector3Int landPos = Vector3Int.FloorToInt(land.transform.InverseTransformPoint(transform.position));

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborLandPos = landPos + VoxelGrid.FaceToDirection(face);
            Block neighborBlock = land.GetCustomBlock(neighborLandPos);
            if (neighborBlock != null)
            {
                if (blockID == neighborBlock.blockID)
                {
                    ((LinkBlock)neighborBlock).RelinkNetwork(land, neighborLandPos, CreateNewNetwork());
                }
                else if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    ((Machine)neighborBlock).UnlinkNetwork(VoxelGrid.GetOppositeFace(face));
                }
            }
        }
    }
}