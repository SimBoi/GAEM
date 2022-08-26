using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.Netcode;

public class Block : Item
{
    [Header("Block Properties")]
    public int blockID;
    public float stiffness;
    public bool hasCustomMesh;
    public GameObject itemObject;
    public GameObject blockObject;

    [HideInInspector] public bool initialized = false;

    public void CopyFrom(Block source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Block source)
    {
        this.blockID = source.blockID;
        this.stiffness = source.stiffness;
        this.hasCustomMesh = source.hasCustomMesh;
    }

    public override Item Clone()
    {
        Block clone = new Block();
        clone.CopyFrom(this);
        return clone;
    }

    public override void Serialize(MemoryStream m, BinaryWriter writer)
    {
        base.Serialize(m, writer);

        writer.Write(blockID);
        writer.Write(stiffness);
        writer.Write(hasCustomMesh);
    }

    public override void Deserialize(MemoryStream m, BinaryReader reader)
    {
        base.Deserialize(m, reader);

        blockID = reader.ReadInt32();
        stiffness = reader.ReadSingle();
        hasCustomMesh = reader.ReadBoolean();
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Block spawnedItem = (Block)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override bool CanBePickedUp()
    {
        if (base.CanBePickedUp() && (!hasCustomMesh || itemObject.activeSelf)) return true;
        return false;
    }

    public void Update()
    {
        if (initialized) BlockUpdate();
    }

    public new void FixedUpdate()
    {
        base.FixedUpdate();
        if (initialized) BlockFixedUpdate();
    }

    // initialization of fields for custom blocks should be done by overriding this function
    public virtual void InitializeFields() { }

    public virtual void InitializeCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int landPos)
    {
        if (initialized) return;
        initialized = true;

        InitializeFields();
        itemObject.SetActive(false);
        blockObject.SetActive(true);
        preventDespawn = true;
    }

    [ClientRpc]
    public void InitializeCustomBlockClientRpc(Vector3 globalPos, Quaternion rotation, NetworkBehaviourReference parentChunkRef, Vector3 landPosFloats)
    {
        if (initialized) return;
        parentChunkRef.TryGet(out Chunk parentChunk);
        Vector3Int landPos = Vector3Int.FloorToInt(landPosFloats);
        InitializeCustomBlock(globalPos, rotation, parentChunk, landPos);
    }

    public virtual void BlockUpdate() { }

    public virtual void BlockFixedUpdate() { }

    // spawns the custom block across the network and calls BlockInitialize
    // should only be called on the server
    public Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int landPos)
    {
        if (!NetworkManager.Singleton.IsServer) return null;
        Block spawnedItem = (Block)Spawn(false, globalPos, rotation, parentChunk.transform);
        spawnedItem.NetworkSpawn();
        spawnedItem.InitializeCustomBlock(globalPos, rotation, parentChunk, landPos);
        spawnedItem.InitializeCustomBlockClientRpc(globalPos, rotation, parentChunk, landPos);
        return spawnedItem;
    }

    // should only be called on the server
    public virtual bool BreakCustomBlock(out Block spawnedItem, Vector3 pos = default, bool spawnItem = false)
    {
        spawnedItem = null;
        if (spawnItem) spawnedItem = (Block)Spawn(false, pos);
        return Despawn();
    }
}