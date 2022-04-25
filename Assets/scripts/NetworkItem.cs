using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System;

/// <summary>
/// item should only be spawned on the server
/// </summary>

public struct SerializedItem : INetworkSerializable
{
    public string name;
    public int id;
    public int maxStackSize;
    public float maxDurability;
    public float spawnDurability;
    public int stackSize;
    public float hp;
    public bool preventDespawn;
    public float despawnTime;
    public bool preventPickup;
    public float pickupDelay;
    public float timeSinceSpawn;
    public bool isHeld;
    public bool isDestroyed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref name);
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref maxStackSize);
        serializer.SerializeValue(ref maxDurability);
        serializer.SerializeValue(ref spawnDurability);
        serializer.SerializeValue(ref stackSize);
        serializer.SerializeValue(ref hp);
        serializer.SerializeValue(ref preventDespawn);
        serializer.SerializeValue(ref despawnTime);
        serializer.SerializeValue(ref preventPickup);
        serializer.SerializeValue(ref pickupDelay);
        serializer.SerializeValue(ref timeSinceSpawn);
        serializer.SerializeValue(ref isHeld);
        serializer.SerializeValue(ref isDestroyed);
    }

    static public SerializedItem Serialize(Item source)
    {
        SerializedItem result = new SerializedItem
        {
            name = source.name,
            id = source.id,
            maxStackSize = source.maxStackSize,
            maxDurability = source.maxDurability,
            spawnDurability = source.spawnDurability,
            stackSize = source.stackSize,
            preventDespawn = source.preventDespawn,
            despawnTime = source.despawnTime,
            preventPickup = source.preventPickup,
            pickupDelay = source.pickupDelay,
            timeSinceSpawn = source.timeSinceSpawn,
            isHeld = source.isHeld,
            isDestroyed = source.isDestroyed
        };
        if (source.health == null)
            result.hp = source.spawnDurability;
        else
            result.hp = source.health.GetHp();
        return result;
    }

    static public Item Deserialize(SerializedItem source)
    {
        Item result = new Item
        {
            name = source.name,
            id = source.id,
            maxStackSize = source.maxStackSize,
            maxDurability = source.maxDurability,
            spawnDurability = source.spawnDurability,
            stackSize = source.stackSize,
            preventDespawn = source.preventDespawn,
            despawnTime = source.despawnTime,
            preventPickup = source.preventPickup,
            pickupDelay = source.pickupDelay,
            timeSinceSpawn = source.timeSinceSpawn,
            isHeld = source.isHeld,
            isDestroyed = source.isDestroyed
        };
        result.health = new Health(source.hp, 0, source.maxDurability);
        return result;
    }
}

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
            SyncItemClientRpc(item.spawnDurability, SerializedItem.Serialize(item));
        }
    }

    // syncs item on all clients on spawn
    [ClientRpc]
    public void SyncItemClientRpc(float spawnDurability, SerializedItem serializedItem)
    {
        item.CopyFrom(SerializedItem.Deserialize(serializedItem));
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
