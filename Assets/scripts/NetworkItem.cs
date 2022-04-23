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

    public void Start()
    {
        if (GetComponent<NetworkObject>().IsSpawned)
            throw new Exception();
        GetComponent<NetworkObject>().Spawn();
    }

    public override void OnNetworkSpawn()
    {
        item = GetComponent<Item>();
        if (IsServer)
        {
            if (item.health == null)
                SyncItemClientRpc(
                    item.name,
                    item.id,
                    item.maxStackSize,
                    item.maxDurability,
                    item.spawnDurability,
                    item.stackSize,
                    item.spawnDurability,
                    item.despawnTime,
                    item.preventPickup,
                    item.pickupDelay,
                    item.timeSinceSpawn,
                    item.isHeld,
                    item.isDestroyed);
            else
                SyncItemClientRpc(
                    item.name,
                    item.id,
                    item.maxStackSize,
                    item.maxDurability,
                    item.spawnDurability,
                    item.stackSize,
                    item.health.GetHp(),
                    item.despawnTime,
                    item.preventPickup,
                    item.pickupDelay,
                    item.timeSinceSpawn,
                    item.isHeld,
                    item.isDestroyed);
        }
    }

    // syncs item on all clients on spawn
    [ClientRpc]
    public void SyncItemClientRpc(
        string name,
        int id,
        int maxStackSize,
        float maxDurability,
        float spawnDurability,
        int stackSize,
        float hp,
        float despawnTime,
        bool preventPickup,
        float pickupDelay,
        float timeSinceSpawn,
        bool isHeld,
        bool isDestroyed)
    {
        item.name = name;
        item.id = id;
        item.maxStackSize = maxStackSize;
        item.maxDurability = maxDurability;
        item.spawnDurability = spawnDurability;
        item.stackSize = stackSize;
        item.health = new Health(hp, 0, maxDurability, 0, 0);
        item.preventDespawn = true;
        item.despawnTime = despawnTime;
        item.preventPickup = preventPickup;
        item.pickupDelay = pickupDelay;
        item.timeSinceSpawn = timeSinceSpawn;
        item.isHeld = isHeld;
        item.isDestroyed = isDestroyed;
    }
}
