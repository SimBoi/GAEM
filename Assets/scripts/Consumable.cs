using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Consumable : Item
{
    public float nutritionalValue;

    public void CopyFrom(Consumable source)
    {
        base.CopyFrom(source);
        this.nutritionalValue = source.nutritionalValue;
    }

    public override Item Clone()
    {
        Consumable clone = new Consumable();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation, Transform parent = null)
    {
        Consumable spawnedItem = (Consumable)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFrom(this);
        return spawnedItem;
    }
}