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

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation, Transform parent = null)
    {
        Block spawnedItem = (Block)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFrom(this);
        return spawnedItem;
    }

    public Item PlaceBlock(Vector3 pos, Quaternion rotation, VoxelChunk parentChunk)
    {
        Block spawnedItem = (Block)base.Spawn(false, pos, rotation, parentChunk.transform);
        spawnedItem.CopyFrom(this);
        spawnedItem.parentChunk = parentChunk;
        spawnedItem.itemObject.SetActive(false);
        spawnedItem.blockObject.SetActive(true);
        spawnedItem.preventDespawn = true;
        spawnedItem.GetComponent<PickupItem>().enabled = false;
        return spawnedItem;
    }

    public virtual bool BreakBlock(Vector3 pos, bool spawnItem = false)
    {
        if (spawnItem) Spawn(false, pos, default(Quaternion));
        return Despawn();
    }
}
