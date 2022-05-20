using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// item should only be spawned on the server
/// </summary>

public class NetworkItem : NetworkBehaviour
{
    private Item item;
    public NetworkVariable<int> networkedStackSize = new NetworkVariable<int>();

    public void Start()
    {
        item = GetComponent<Item>();
        if (NetworkManager.Singleton.IsServer)
        {
            networkedStackSize.Value = item.stackSize;
            GetComponent<NetworkObject>().Spawn();
        }
    }

    public override void OnNetworkSpawn()
    {
        item = GetComponent<Item>();
        if (IsServer)
        {
            SyncItemClientRpc(item.Serialize());
        }
    }

    // syncs item on all clients on spawn
    [ClientRpc]
    public void SyncItemClientRpc(byte[] serializedItem)
    {
        item.Deserialize(serializedItem);
    }

    public virtual void Update()
    {
        if (IsServer)
        {
            networkedStackSize.Value = item.GetStackSize();
        }
        else
        {
            item.SetStackSize(networkedStackSize.Value);
        }
    }
}
