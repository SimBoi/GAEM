using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : Item
{
    public int blockID;
    public float stiffness ;

    public void CopyFrom(Block source)
    {
        base.CopyFrom(source);
        this.blockID = source.blockID;
        this.stiffness = source.stiffness;
    }

    public override Item Clone()
    {
        Block clone = new Block();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(Vector3 pos, Quaternion rotation, Transform parent = null)
    {
        Block spawnedItem = (Block)base.Spawn(pos, rotation, parent);
        spawnedItem.CopyFrom(this);
        return spawnedItem;
    }
}
