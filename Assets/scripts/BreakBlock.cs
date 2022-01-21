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
            hitInfo.collider.SendMessageUpwards("GetLandRef", message, SendMessageOptions.DontRequireReceiver);
            Land land = (Land)message[0];

            if (land != null)
            {
                Vector3 globalHitCoords = hitInfo.point - (0.5f * hitInfo.normal);
                Vector3 landHitCoords = land.transform.InverseTransformPoint(globalHitCoords);
                Vector3Int landBlockCoords = Vector3Int.FloorToInt(landHitCoords);
                Vector3Int chunkBlockCoords = new Vector3Int((int)landHitCoords.x % land.chunkSizeX, (int)landHitCoords.y % land.chunkSizeY, (int)landHitCoords.z % land.chunkSizeZ);
                if (landBlockCoords != prevCoords) timer = 0;
                float blockStiffness = land.chunks[new Vector2Int((int)landBlockCoords.x/ land.chunkSizeX, (int)landBlockCoords.z/ land.chunkSizeZ)].GetComponent<VoxelChunk>().GetStiffness(chunkBlockCoords);

                /////////////////////////// debug remove later
                /////heq
                Debug.Log(timer * 100 / (blockStiffness / efficiency) + "%");

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