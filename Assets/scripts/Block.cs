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

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Block spawnedItem = (Block)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override bool CanBePickedUp()
    {
        if (base.CanBePickedUp() && (!hasCustomMesh || itemObject.activeSelf))
            return true;
        return false;
    }

    public void Update()
    {
        if (blockObject != null && blockObject.activeSelf)
        {
            BlockInitialize();
            BlockUpdate();
        }
    }

    public new void FixedUpdate()
    {
        base.FixedUpdate();
        if (blockObject != null && blockObject.activeSelf)
        {
            BlockFixedUpdate();
        }
    }

    private bool initialized = false;
    public virtual bool BlockInitialize()
    {
        if (initialized || blockObject == null || !blockObject.activeSelf) return false;
        initialized = true;
        return true;
    }

    public virtual void BlockUpdate() { }

    public virtual void BlockFixedUpdate() { }

    public virtual Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int landPos)
    {
        Block spawnedItem = (Block)Spawn(false, globalPos, rotation, parentChunk.transform);
        spawnedItem.itemObject.SetActive(false);
        spawnedItem.blockObject.SetActive(true);
        spawnedItem.preventDespawn = true;
        spawnedItem.BlockInitialize();
        return spawnedItem;
    }

    public virtual bool BreakCustomBlock(Vector3 pos = default, bool spawnItem = false)
    {
        if (spawnItem) Spawn(false, pos);
        return Despawn();
    }
}