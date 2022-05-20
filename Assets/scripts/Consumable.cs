using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Consumable : Item
{
    [Header("Consumable Properties")]
    public float nutritionalValue;

    public void CopyFrom(Consumable source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Consumable source)
    {
        this.nutritionalValue = source.nutritionalValue;
    }    

    public override Item Clone()
    {
        Consumable clone = new Consumable();
        clone.CopyFrom(this);
        return clone;
    }

    public override void Serialize(MemoryStream m, BinaryWriter writer)
    {
        base.Serialize(m, writer);

        writer.Write(nutritionalValue);
    }

    public override void Deserialize(MemoryStream m, BinaryReader reader)
    {
        base.Deserialize(m, reader);

        nutritionalValue = reader.ReadSingle();
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Consumable spawnedItem = (Consumable)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }
}