using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    public override void Serialize(MemoryStream m, BinaryWriter writer)
    {
        base.Serialize(m, writer);

        writer.Write(armorStrength);
        writer.Write((int)armorPiece);
    }

    public override void Deserialize(MemoryStream m, BinaryReader reader)
    {
        base.Deserialize(m, reader);

        armorStrength = reader.ReadSingle();
        armorPiece = (ArmorPiece)reader.ReadInt32();
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