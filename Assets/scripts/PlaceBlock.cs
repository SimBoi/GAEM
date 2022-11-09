using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBlock : ItemEvent
{
    public Block block;
    public float maxDistance = 3;
    public Vector3 prevCoords;

    public override void CustomItemEvent(GameObject eventCaller)
    {
        Transform origin = eventCaller.GetComponent<CharacterController>().eyePosition.transform;
        RaycastHit hitInfo;
        if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, maxDistance))
        {
            object[] message = new object[1]{
                null
            };
            hitInfo.collider.SendMessageUpwards("GetGridRefMsg", message, SendMessageOptions.DontRequireReceiver);
            VoxelGrid voxelGrid = (VoxelGrid)message[0];

            if (voxelGrid != null)
            {
                Vector3Int GridBlockCoords = voxelGrid.GlobalToGridCoords(hitInfo.point + (0.99f * hitInfo.normal));

                //if (voxelGrid.AddBlock(GridBlockCoords, (short)block.blockID, Quaternion.LookRotation(hitInfo.normal)))
                if (voxelGrid.AddBlock(GridBlockCoords, (short)block.blockID))
                {
                    PlayerInventory playerInventory = eventCaller.GetComponent<PlayerInventory>();
                    playerInventory.ConsumeFromStack(1, playerInventory.heldItemIndex);
                }
            }
        }
    }
}