using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakBlock : ItemEvent
{
    public float maxDistance = 3;
    public float timer = 0;    
    public float efficiency = 1;
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
                Vector3Int gridBlockCoords = voxelGrid.GlobalToGridCoords(hitInfo.point - (0.01f * hitInfo.normal));
                if (gridBlockCoords != prevCoords) timer = 0;

                float blockStiffness = voxelGrid.GetStiffness(gridBlockCoords);
                if (timer >= blockStiffness / efficiency)
                {
                    voxelGrid.RemoveBlock(gridBlockCoords, true);
                    timer = 0;
                }
                prevCoords = gridBlockCoords;
            }
            else
            {
                timer = 0;
            }
        }
        timer += Time.deltaTime;
    }

    public override void CustomItemEventExit(GameObject eventCaller)
    {
        timer = 0;
    }
}