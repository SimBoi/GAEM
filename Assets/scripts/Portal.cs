using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : Machine
{
    public Item inputItem;
    public int cost;
    public Vector3 destCoords;

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
