using Unity.Netcode;
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
            hitInfo.collider.SendMessageUpwards("GetLandRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Land land = (Land)message[0];
            Vector3Int landBlockCoords = land.ConvertToLandCoords(hitInfo.point + (0.99f * hitInfo.normal));

            //if (land != null) PlaceBlockServerRpc(land.GetComponent<NetworkObject>().NetworkObjectId, landBlockCoords, (short)block.blockID, block.id, block.Serialize(), eventCaller.GetComponent<NetworkObject>().NetworkObjectId, OwnerClientId);
        }
    }

    /*[ServerRpc]
    public void PlaceBlockServerRpc(ulong landID, Vector3Int landBlockCoords, short blockID, int itemID, byte[] serializedItem, ulong eventCallerID, ulong clientId)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        Land land = NetworkManager.Singleton.SpawnManager.SpawnedObjects[landID].gameObject.GetComponent<Land>();
        //if (land.AddBlock(landBlockCoords, (short)block.blockID, Quaternion.LookRotation(hitInfo.normal)))
        if (land.AddBlock(landBlockCoords, blockID))
        {
            PlaceBlockClientRpc(itemID, serializedItem, eventCallerID, clientRpcParams);
        }
    }

    [ClientRpc]
    public void PlaceBlockClientRpc(int itemID, byte[] serializedItem, ulong eventCallerID, ClientRpcParams clientRpcParams)
    {
        PlayerInventory playerInventory = NetworkManager.Singleton.SpawnManager.SpawnedObjects[eventCallerID].gameObject.GetComponent<PlayerInventory>();
        playerInventory.ConsumeFromTotalStack(Item.Deserialize(itemID, serializedItem), 1, out _, out _);
    }*/
}