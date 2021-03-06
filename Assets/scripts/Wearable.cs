using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wearable : Item
{
    [Header("Wearable Properties")]
    public float armorStrength;
    public ArmorPiece armorPiece;

    public void CopyFrom(Wearable source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Wearable source)
    {
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
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override void SecondaryItemEvent(GameObject eventCaller)
    {
        PlayerInventory playerInventory = eventCaller.GetComponent<PlayerInventory>();
        if (playerInventory != null)
            playerInventory.EquipArmor(this, armorPiece, out _);
    }
}