using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakBlock : ItemEvent
{
    public float maxDistance = 3;
    public float timer = 0;    
    public float efficiency = 1;
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
                Vector3 hitCoords = hitInfo.point - (0.5f * hitInfo.normal);
                Vector3Int blockCoords = new Vector3Int((int)hitCoords.x % chunk.sizeX, (int)hitCoords.y % chunk.sizeY, (int)hitCoords.z % chunk.sizeZ);
                if (blockCoords != prevCoords) timer = 0;
                float blockStiffness = chunk.GetStiffness(blockCoords);
                Debug.Log(timer * 100 /(blockStiffness / efficiency) + "%");
                if (timer >= blockStiffness/efficiency)
                {
                    Debug.Log("am here yo");
                    chunk.RemoveBlock(blockCoords);
                    timer = 0;
                }
                prevCoords = new Vector3Int((int)hitCoords.x % chunk.sizeX, (int)hitCoords.y % chunk.sizeY, (int)hitCoords.z % chunk.sizeZ);
            }
        }
        timer += Time.deltaTime;
    }

    public override void CustomEventExit(GameObject eventCaller)
    {
        timer = 0;
    }
}