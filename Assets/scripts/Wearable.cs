using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wearable : Item
{
    public float armorStrength;
    public ArmorPiece armorPiece;

    public void CopyFrom(Wearable source)
    {
        base.CopyFrom(source);
        this.armorStrength = source.armorStrength;
        this.armorPiece = source.armorPiece;
    }

    public override Item Clone()
    {
        Wearable clone = new Wearable();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Wearable spawnedItem = (Wearable)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFrom(this);
        return spawnedItem;
    }

    public override void SecondaryItemEvent(GameObject eventCaller)
    {
        eventCaller.GetComponent<PlayerInventory>().EquipArmor(this, armorPiece, armorStrength); ////////////////////// if item was held then destroy reference to this wearable
    }
}