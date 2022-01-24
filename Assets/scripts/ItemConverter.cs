using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemConverter : Machine
{
    public Item inputItem;
    public Item outputItem;
    public int cost = 1;

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
                characterController.inventory.PickupItem(outputItem.Clone());
            }
            else
            {
                Debug.Log("not enough items/wrong item");
            }
        }
    }
}