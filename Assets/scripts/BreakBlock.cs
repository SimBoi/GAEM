using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BreakBlock : ItemEvent
{
    public float maxDistance = 3;
    public float timer = 0;    
    public float efficiency = 1;
    public Vector3 prevCoords;
    public short prevBlockID;

    public override void CustomItemEvent(GameObject eventCaller)
    {
        Transform origin = eventCaller.GetComponent<CharacterController>().eyePosition.transform;
        RaycastHit hitInfo;
        if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, maxDistance))
        {
            object[] message = new object[1] { null };
            hitInfo.collider.SendMessageUpwards("GetLandRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Land land = (Land)message[0];

            if (land != null)
            {
                Vector3Int landBlockCoords = land.ConvertToLandCoords(hitInfo.point - (0.01f * hitInfo.normal));
                Vector3Int chunkBlockCoords = new Vector3Int(landBlockCoords.x % land.chunkSizeX, landBlockCoords.y % land.chunkSizeY, landBlockCoords.z % land.chunkSizeZ);
                short blockID = land.GetBlockID(landBlockCoords);
                if (landBlockCoords != prevCoords || blockID != prevBlockID) timer = 0;

                float blockStiffness = land.chunks[new Vector2Int((int)landBlockCoords.x/ land.chunkSizeX, (int)landBlockCoords.z/ land.chunkSizeZ)].GetComponent<Chunk>().GetStiffness(chunkBlockCoords); // todo: stiffness wrapper in land
                if (timer >= blockStiffness / efficiency)
                {
                    BreakBlockServerRpc(land.gameObject, blockID, landBlockCoords);
                    timer = 0;
                }
                prevCoords = landBlockCoords;
                prevBlockID = blockID;
            }
            else
            {
                timer = 0;
            }
        }
        timer += Time.deltaTime;
    }

    [ServerRpc]
    public void BreakBlockServerRpc(NetworkObjectReference landRef, short blockID, Vector3 landBlockCoordsFloats)
    {
        landRef.TryGet(out NetworkObject landNetworkObject);
        Land land = landNetworkObject.GetComponent<Land>();
        Vector3Int landBlockCoords = Vector3Int.FloorToInt(landBlockCoordsFloats);
        if (blockID == land.GetBlockID(landBlockCoords)) land.RemoveBlock(landBlockCoords, true);
    }

    public override void CustomItemEventExit(GameObject eventCaller)
    {
        timer = 0;
    }
}