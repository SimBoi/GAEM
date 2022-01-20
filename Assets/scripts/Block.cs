using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : Item
{
    public int blockID;
    public float stiffness;
    public bool hasCustomMesh;
    public GameObject itemObject;
    public GameObject blockObject;
    public VoxelChunk parentChunk = null;

    public void CopyFrom(Block source)
    {
        base.CopyFrom(source);
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

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Block spawnedItem = (Block)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFrom(this);
        return spawnedItem;
    }

    public virtual Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, VoxelChunk parentChunk, Vector3Int chunkPos)
    {
        Block spawnedItem = (Block)base.Spawn(false, globalPos, rotation, parentChunk.transform);
        spawnedItem.CopyFrom(this);
        spawnedItem.parentChunk = parentChunk;
        spawnedItem.itemObject.SetActive(false);
        spawnedItem.blockObject.SetActive(true);
        spawnedItem.preventDespawn = true;
        return spawnedItem;
    }

    public virtual bool BreakCustomBlock(bool spawnItem = false, Vector3 pos = default(Vector3))
    {
        if (spawnItem) Spawn(false, pos);
        return Despawn();
    }
}