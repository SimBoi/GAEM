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
            object[] message = new object[1] { null };
            hitInfo.collider.SendMessageUpwards("GetLandRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Land land = (Land)message[0];
            Vector3Int landBlockCoords = land.ConvertToLandCoords(hitInfo.point + (0.99f * hitInfo.normal));
            if (land != null) PlaceBlockServerRpc(land.gameObject, landBlockCoords, (short)block.blockID, block.id, block.Serialize(), eventCaller);
        }
    }

    [ServerRpc]
    public void PlaceBlockServerRpc(NetworkObjectReference landRef, Vector3 landBlockCoordsFloats, short blockID, int itemID, byte[] serializedItem, NetworkObjectReference eventCallerRef)
    {
        landRef.TryGet(out NetworkObject landNetworkObject);
        Land land = landNetworkObject.GetComponent<Land>();
        Vector3Int landBlockCoords = Vector3Int.FloorToInt(landBlockCoordsFloats);
        if (land.AddBlock(landBlockCoords, blockID))
        {
            eventCallerRef.TryGet(out NetworkObject eventCallerNetworkObject);
            PlayerInventory playerInventory = eventCallerNetworkObject.GetComponent<PlayerInventory>();
            playerInventory.ConsumeFromTotalStack(Item.Deserialize(itemID, serializedItem), 1, out _, out _);
        }
    }
}