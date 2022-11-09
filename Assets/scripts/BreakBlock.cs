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
            hitInfo.collider.SendMessageUpwards("GetLandRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Land land = (Land)message[0];

            if (land != null)
            {
                Vector3Int landBlockCoords = land.GlobalToLandCoords(hitInfo.point - (0.01f * hitInfo.normal));
                Vector3Int chunkBlockCoords = new Vector3Int(landBlockCoords.x % land.chunkSizeX, landBlockCoords.y % land.chunkSizeY, landBlockCoords.z % land.chunkSizeZ);
                if (landBlockCoords != prevCoords) timer = 0;

                float blockStiffness = land.GetChunk(new Vector2Int((int)landBlockCoords.x/ land.chunkSizeX, (int)landBlockCoords.z/ land.chunkSizeZ)).GetComponent<Chunk>().GetStiffness(chunkBlockCoords);
                if (timer >= blockStiffness / efficiency)
                {
                    land.RemoveBlock(landBlockCoords, true);
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

    public override void CustomItemEventExit(GameObject eventCaller)
    {
        timer = 0;
    }
}