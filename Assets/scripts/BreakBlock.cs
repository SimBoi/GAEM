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
            Land land = null;

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
                Vector3 hitCoords = hitInfo.point - (0.5f * hitInfo.normal);
                Vector3 landHitCoords = land.transform.InverseTransformPoint(hitCoords);
                Vector3Int landBlockCoords = Vector3Int.FloorToInt(landHitCoords);
                Vector3Int chunkBlockCoords = new Vector3Int((int)landHitCoords.x % land.chunkSizeX, (int)landHitCoords.y % land.chunkSizeY, (int)landHitCoords.z % land.chunkSizeZ);
                if (landBlockCoords != prevCoords) timer = 0;
                float blockStiffness = land.chunks[new Vector2Int((int)landBlockCoords.x/ land.chunkSizeX, (int)landBlockCoords.z/ land.chunkSizeZ)].GetComponent<VoxelChunk>().GetStiffness(chunkBlockCoords);

                /////////////////////////// debug remove later
                Debug.Log(timer * 100 / (blockStiffness / efficiency) + "%");

                if (timer >= blockStiffness / efficiency)
                {
                    land.RemoveBlock(landBlockCoords, true, land.transform.TransformPoint(landBlockCoords + new Vector3(0.5f, 0.5f, 0.5f)), land.transform.rotation);
                    timer = 0;
                }
                prevCoords = landBlockCoords;
            }
            else
            {
                timer = 0;
            }
        }
        timer += Time.deltaTime;
    }

    public override void CustomEventExit(GameObject eventCaller)
    {
        timer = 0;
    }
}