using System;
using  System.Collections;
using  System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using UnityEngine;

// network object, if present, should have auto parenting sync disabled
public class Item : NetworkBehaviour
{
    private static ItemPrefabs itemPrefabs = null;
    public static GameObject[] prefabs
    {
        get
        {
            if (itemPrefabs == null) itemPrefabs = ((GameObject)Resources.Load("ItemPrefabReferences", typeof(GameObject))).GetComponent<ItemPrefabs>();
            return itemPrefabs.prefabs;
        }
    }

    [Header("Item Properties")]
    public string name;
    public int id;
    public int maxStackSize;
    public float maxDurability;
    public float spawnDurability;
    public int stackSize;
    public Health health;
    public bool preventDespawn;
    public float despawnTime = 300;
    public bool preventPickup;
    public float pickupDelay;
    public float timeSinceSpawn = 0;
    public bool isHeld;
    public bool isDestroyed = false;
    public Sprite icon;
    public GameObject ui = null;
    public AnimatorOverrideController fpsArmsAnimatorOverrideController;

    public void CopyFrom(Item source)
    {
        this.name = source.name;
        this.id = source.id;
        this.maxStackSize = source.maxStackSize;
        this.maxDurability = source.maxDurability;
        this.spawnDurability = source.spawnDurability;
        this.stackSize = source.stackSize;
        if (source.health == null)
            this.health = new Health(source.spawnDurability, 0, source.maxDurability, 0, 0);
        else
            this.health = new Health(source.health.GetHp(), 0, source.maxDurability, 0, 0);
        this.preventDespawn = source.preventDespawn;
        this.despawnTime = source.despawnTime;
        this.preventPickup = source.preventPickup;
        this.pickupDelay = source.pickupDelay;
        this.timeSinceSpawn = source.timeSinceSpawn;
        this.isHeld = source.isHeld;
        this.isDestroyed = source.isDestroyed;
        this.icon = source.icon;
        this.fpsArmsAnimatorOverrideController = source.fpsArmsAnimatorOverrideController;
    }

    public virtual Item Clone()
    {
        Item clone = new Item();
        clone.CopyFrom(this);
        return clone;
    }

    public byte[] Serialize()
    {
        MemoryStream m = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(m);
        Serialize(m, writer);
        return m.ToArray();
    }

    public void Deserialize(byte[] serializedItem)
    {
        MemoryStream m = new MemoryStream(serializedItem);
        BinaryReader reader = new BinaryReader(m);
        Deserialize(m, reader);
    }

    // returns deserialized Item using the overridden deserialize method from the correct derived type
    // returns null for id <= 0 or id >= prefabs.length
    public static Item Deserialize(int id, byte[] serializedItem)
    {
        if (id <= 0 || id >= prefabs.Length) return null;

        Item item = prefabs[id].GetComponent<Item>().Clone();
        item.Deserialize(serializedItem);
        return item;
    }

    public virtual void Serialize(MemoryStream m, BinaryWriter writer)
    {
        writer.Write(name);
        writer.Write(id);
        writer.Write(maxStackSize);
        writer.Write(maxDurability);
        writer.Write(spawnDurability);
        writer.Write(stackSize);
        if (health == null)
            writer.Write(spawnDurability);
        else
            writer.Write(health.GetHp());
        writer.Write(preventDespawn);
        writer.Write(despawnTime);
        writer.Write(preventPickup);
        writer.Write(pickupDelay);
        writer.Write(timeSinceSpawn);
        writer.Write(isHeld);
        writer.Write(isDestroyed);
    }

    public virtual void Deserialize(MemoryStream m, BinaryReader reader)
    {
        name = reader.ReadString();
        id = reader.ReadInt32();
        maxStackSize = reader.ReadInt32();
        maxDurability = reader.ReadSingle();
        spawnDurability = reader.ReadSingle();
        stackSize = reader.ReadInt32();
        health = new Health(reader.ReadSingle(), 0, maxDurability);
        preventDespawn = reader.ReadBoolean();
        despawnTime = reader.ReadSingle();
        preventPickup = reader.ReadBoolean();
        pickupDelay = reader.ReadSingle();
        timeSinceSpawn = reader.ReadSingle();
        isHeld = reader.ReadBoolean();
        isDestroyed = reader.ReadBoolean();
    }

    [ClientRpc]
    public void SyncItemClientRpc(byte[] serializedItem)
    {
        Deserialize(serializedItem);
    }

    public static bool operator ==(Item a, Item b)
    {
        if (a is null || b is null)
        {
            if (a is null && b is null) return true;
            return false;
        }

        if (a.id == b.id && a.name == b.name)
            return true;
        return false;
    }

    public static bool operator !=(Item a, Item b)
    {
        if (a == b)
            return false;
        return true;
    }

    // returns new stacksize on success
    // returns -1 on error
    public int ChangeStackSize(int stackChange, bool despawnItem = true)
    {
        if (stackSize + stackChange < 0 || stackSize + stackChange > maxStackSize) return -1;
        stackSize += stackChange;
        if (despawnItem && stackSize <= 0) Despawn();
        return stackSize;
    }

    // returns stacksize
    public int DamageItem(float dmg)
    {
        health.DealDamage(dmg);
        if (health.GetHp() <= 0) ChangeStackSize(-1);
        return stackSize;
    }

    public int GetStackSize()
    {
        return stackSize;
    }

    public void SetStackSize(int newSize)
    {
        stackSize = newSize;
    }

    public float GetDurability()
    {
        return health.GetHp();
    }

    public virtual Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        GameObject newItem;
        if (parent == null) newItem = Instantiate(prefabs[id], pos, rotation);
        else newItem = Instantiate(prefabs[id], pos, rotation, parent);
        Item spawnedItem = newItem.GetComponent<Item>();
        spawnedItem.CopyFrom(this);
        spawnedItem.isHeld = isHeld;
        spawnedItem.timeSinceSpawn = 0;
        return spawnedItem;
    }

    // Spawn the item on all clients
    // Optional: set ownership to provided owner id, defaul is server owned
    public void NetworkSpawn(ulong ownerClientID = NetworkManager.ServerClientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        GetComponent<NetworkObject>().SpawnWithOwnership(ownerClientID);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) InitializeItemClientRpc(Serialize());
    }

    [ClientRpc]
    public virtual void InitializeItemClientRpc(byte[] serializedItem)
    {
        Deserialize(serializedItem);
        if (IsOwner && isHeld && ui != null) {
            print("heq");
            ui.SetActive(true);
        }
    }

    public virtual bool Despawn()
    {
        try { if (gameObject != null) Destroy(gameObject); }
        catch (NullReferenceException) { }
        isDestroyed = true;
        return true;
    }

    public void FixedUpdate()
    {
        if (!IsServer) return;

        if (!preventDespawn && timeSinceSpawn >= despawnTime) Despawn();
        if (timeSinceSpawn < despawnTime || timeSinceSpawn < pickupDelay) timeSinceSpawn += Time.fixedDeltaTime;
    }

    public virtual bool CanBePickedUp()
    {
        if (!preventPickup && timeSinceSpawn >= pickupDelay && !isHeld && !isDestroyed)
            return true;
        return false;
    }

    public virtual void PickupEvent() { }

    // calls HoldItem on the client that owns eventCaller
    // eventCaller has to have a NetworkObject component
    public void HoldEvent(GameObject eventCaller)
    {
        NetworkObject callerNetworkObject = eventCaller.GetComponent<NetworkObject>();
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { callerNetworkObject.OwnerClientId }
            }
        };
        HoldEventClientRpc(callerNetworkObject.NetworkObjectId, clientRpcParams);
    }

    [ClientRpc]
    public void HoldEventClientRpc(ulong eventCallerID, ClientRpcParams clientRpcParams)
    {
        GameObject eventCaller = NetworkManager.Singleton.SpawnManager.SpawnedObjects[eventCallerID].gameObject;
        HoldItem(eventCaller);
    }

    public virtual void HoldItem(GameObject eventCaller) { }

    public virtual void PrimaryItemEvent(GameObject eventCaller)
    {
        SendMessage("CustomPrimaryItemEvent", eventCaller, SendMessageOptions.DontRequireReceiver);
    }

    public virtual void SecondaryItemEvent(GameObject eventCaller)
    {
        SendMessage("CustomSecondaryItemEvent", eventCaller, SendMessageOptions.DontRequireReceiver);
    }

    // message[0] = (Item)return
    public void GetItemRefMsg(object[] message)
    {
        message[0] = this;
    }
}