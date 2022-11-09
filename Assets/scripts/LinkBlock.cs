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

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, VoxelGrid voxelGrid, Vector3Int gridCoords)
    {
        LinkBlock spawnedItem = (LinkBlock)base.PlaceCustomBlock(globalPos, rotation, voxelGrid, gridCoords);

        bool relinkNetwork = false;
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborGridCoords = gridCoords + VoxelGrid.FaceToDirection(face);
            if (spawnedItem.blockID == voxelGrid.GetBlockID(neighborGridCoords))
                relinkNetwork = true;
        }

        if (relinkNetwork)
            spawnedItem.RelinkNetwork(voxelGrid, gridCoords);
        else
            spawnedItem.RelinkNetwork(voxelGrid, gridCoords, CreateNewNetwork());

        return spawnedItem;
    }

    public override bool BreakCustomBlock(Vector3 pos = default, bool spawnItem = false)
    {
        if (!base.BreakCustomBlock(pos, spawnItem))
            return false;
        UnlinkNetwork();
        return true;
    }

    public void RelinkNetwork(VoxelGrid voxelGrid, Vector3Int gridCoords, Network targetNetwork = null)
    {
        if (isDestroyed)
            return;
        if (network != null && ReferenceEquals(targetNetwork, network))
            return;

        if (targetNetwork == null)
        {
            foreach (Faces face in Enum.GetValues(typeof(Faces)))
            {
                Vector3Int neighborGridCoords = gridCoords + VoxelGrid.FaceToDirection(face);
                Block neighborBlock = voxelGrid.GetCustomBlock(neighborGridCoords);
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
            Vector3Int neighborGridCoords = gridCoords + VoxelGrid.FaceToDirection(face);
            Block neighborBlock = voxelGrid.GetCustomBlock(neighborGridCoords);
            if (neighborBlock != null)
            {
                if (blockID == neighborBlock.blockID)
                {
                    ((LinkBlock)neighborBlock).RelinkNetwork(voxelGrid, neighborGridCoords, targetNetwork);
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
        SendMessageUpwards("GetGridRefMsg", message);
        VoxelGrid voxelGrid = (VoxelGrid)message[0];
        Vector3Int gridCoords = Vector3Int.FloorToInt(voxelGrid.transform.InverseTransformPoint(transform.position));

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborGridCoords = gridCoords + VoxelGrid.FaceToDirection(face);
            Block neighborBlock = voxelGrid.GetCustomBlock(neighborGridCoords);
            if (neighborBlock != null)
            {
                if (blockID == neighborBlock.blockID)
                {
                    ((LinkBlock)neighborBlock).RelinkNetwork(voxelGrid, neighborGridCoords, CreateNewNetwork());
                }
                else if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    ((Machine)neighborBlock).UnlinkNetwork(VoxelGrid.GetOppositeFace(face));
                }
            }
        }
    }
}