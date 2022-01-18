using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceBlock : ItemEvent
{
    public Block block;
    public float maxDistance = 3;
    public Vector3 prevCoords;

    public override void CustomEvent(GameObject eventCaller)
    {
        Transform origin = eventCaller.GetComponent<CharacterController>().eyePosition.transform;
        RaycastHit hitInfo;
        Debug.DrawRay(origin.position, origin.TransformDirection(Vector3.forward) * 20, Color.green);

        if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, maxDistance))
        {
            Land land = null;
            if (hitInfo.transform.GetComponent<VoxelChunk>() != null)
            {
                land = hitInfo.transform.GetComponent<VoxelChunk>().land;
            }
            else if (hitInfo.transform.parent != null && hitInfo.transform.parent.GetComponent<Block>() != null)
            {
                land = hitInfo.transform.parent.GetComponent<Block>().parentChunk.land;
            }

            if (land != null)
            {
                Vector3 globalHitCoords = hitInfo.point + (0.001f * hitInfo.normal);
                Vector3 landHitCoords = land.transform.InverseTransformPoint(globalHitCoords);
                Vector3Int landBlockCoords = Vector3Int.FloorToInt(landHitCoords);

                land.AddBlock(landBlockCoords, (short)block.blockID, Quaternion.LookRotation(hitInfo.normal));
            }
        }
    }

    public override void CustomEventExit(GameObject eventCaller)
    {
       
    }
}