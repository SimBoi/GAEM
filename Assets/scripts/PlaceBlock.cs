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
            if (hitInfo.transform.tag == "Chunk")
            {
                VoxelChunk chunk = hitInfo.transform.GetComponent<VoxelChunk>();
                Vector3 hitCoords = hitInfo.point + (0.5f * hitInfo.normal);
                Vector3Int blockCoords = new Vector3Int((int)hitCoords.x, (int)hitCoords.y, (int)hitCoords.z);
		chunk.land.AddBlock(blockCoords, (short)block.blockID);
            }
        }
    }

    public override void CustomEventExit(GameObject eventCaller)
    {
       
    }
}