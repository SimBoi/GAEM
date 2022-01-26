using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : Machine
{
    public Item inputItem;
    public int cost;
    public Vector3 destCoords;

    public void CopyFrom(Portal source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Portal source)
    {
        this.inputItem = source.inputItem;
        this.destCoords = source.destCoords;
        this.cost = source.cost;
    }

    public override Item Clone()
    {
        Portal clone = new Portal();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Portal spawnedItem = (Portal)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        if (eventCaller.GetComponent<CharacterController>() != null)
        {
            CharacterController characterController = eventCaller.GetComponent<CharacterController>();
            int heldItemIndex = characterController.inventory.heldItemIndex;
            if (heldItemIndex == -1) return;
            Item heldItem = characterController.inventory.GetHeldItemRef();
            if (heldItem == inputItem && characterController.inventory.GetStackSize(heldItemIndex) >= cost)
            {
                characterController.inventory.ConsumeFromStack(cost, heldItemIndex);
                eventCaller.transform.position = destCoords;
            }
            else
            {
                Debug.Log("not enough items/wrong item");
            }
        }
    }
}
