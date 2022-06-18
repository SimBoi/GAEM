using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ItemConverter : Machine
{
    public Item inputItem;
    public Item outputItem;
    public int cost = 1;

    public void CopyFrom(ItemConverter source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(ItemConverter source)
    {
        this.inputItem = source.inputItem;
        this.outputItem = source.outputItem;
        this.cost = source.cost;
    }

    public override Item Clone()
    {
        ItemConverter clone = new ItemConverter();
        clone.CopyFrom(this);
        return clone;
    }

    public override void Serialize(MemoryStream m, BinaryWriter writer)
    {
        base.Serialize(m, writer);

        writer.Write(inputItem.id);
        writer.Write(outputItem.id);
        writer.Write(cost);
    }

    public override void Deserialize(MemoryStream m, BinaryReader reader)
    {
        base.Deserialize(m, reader);

        inputItem = Item.prefabs[reader.ReadInt32()].GetComponent<Item>();
        outputItem = Item.prefabs[reader.ReadInt32()].GetComponent<Item>();
        cost = reader.ReadInt32();
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        ItemConverter spawnedItem = (ItemConverter)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        if (eventCaller.GetComponent<CharacterController>() != null)
        {
            PlayerInventory characterController = eventCaller.GetComponent<PlayerInventory>();
            int heldItemIndex = characterController.heldItemIndex;
            if (heldItemIndex == -1) return;
            Item heldItem = characterController.GetHeldItemRef();
            if (heldItem == inputItem && characterController.GetStackSize(heldItemIndex) >= cost)
            {
                characterController.ConsumeFromStack(cost, heldItemIndex);
                characterController.PickupItem(outputItem.Clone(), out _, out _);
            }
            else
            {
                Debug.Log("not enough items/wrong item");
            }
        }
    }
}