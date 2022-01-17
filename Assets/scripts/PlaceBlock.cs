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
            Land land = new Land();
            if (hitInfo.transform.GetComponent<VoxelChunk>() != null)
            {
                land = hitInfo.transform.GetComponent<VoxelChunk>().land;
            }
            else if (hitInfo.transform.GetComponent<Block>() != null)
            {
                land = hitInfo.transform.GetComponent<Block>().parentChunk.land;
            }
            if (land != null)
            {
                Vector3 hitCoords = hitInfo.point + (0.5f * hitInfo.normal);
                Vector3 landHitCoords = land.transform.InverseTransformPoint(hitCoords);
                Vector3Int landBlockCoords = Vector3Int.FloorToInt(landHitCoords);

                land.AddBlock(landBlockCoords, (short)block.blockID, Quaternion.LookRotation(hitInfo.normal));
            }
        }
    }

    public override void CustomEventExit(GameObject eventCaller)
    {
       
    }
}