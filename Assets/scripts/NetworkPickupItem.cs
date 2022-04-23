using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkPickupItem : NetworkBehaviour
{
    private CharacterController controller;
    public float pickupRadius;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (IsOwner)
        {
            PickupItemsNearby();
        }
    }

    public void PickupItemsNearby() // should only be called on the client
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var hitCollider in hitColliders)
        {
            object[] message = new object[1]{
                null
            };
            hitCollider.SendMessageUpwards("GetItemRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Item item = (Item)message[0];
            if (item != null && item.CanBePickedUp())
            {
                RequestItemPickupServerRpc(item.GetComponent<NetworkObject>().NetworkObjectId, controller.networkObject.OwnerClientId);
            }
        }
    }

    [ServerRpc]
    private void RequestItemPickupServerRpc(ulong objectId, ulong clientId)
    {
        Item item = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].GetComponent<Item>();
        if (item.CanBePickedUp())
        {
            item.preventPickup = true;
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            PickupItemClientRpc(objectId, clientRpcParams);
        }
    }

    [ClientRpc]
    private void PickupItemClientRpc(ulong objectId, ClientRpcParams clientRpcParams)
    {
        Item item = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].GetComponent<Item>();
        item.preventPickup = false;
        if (controller.inventory.PickupItem(item, out _, out _) != InsertResult.Failure)
            PickupItemServerRpc(objectId, NetworkManager.Singleton.LocalClientId);
        else
            CancelPickupItemServerRpc(objectId, NetworkManager.Singleton.LocalClientId);
    }

    [ServerRpc]
    private void PickupItemServerRpc(ulong objectId, ulong clientId)
    {
        Item item = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].GetComponent<Item>();
        item.preventPickup = false;
        Destroy(item.gameObject);
    }

    [ServerRpc]
    private void CancelPickupItemServerRpc(ulong objectId, ulong clientId)
    {
        Item item = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objectId].GetComponent<Item>();
        item.preventPickup = false;
    }
}
