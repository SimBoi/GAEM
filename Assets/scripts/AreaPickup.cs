using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaPickup : Machine
{
    public float radius;
    public bool isActive;

    public override void BlockUpdate()
    {
        if (!isActive) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        foreach (var hitCollider in hitColliders)
        {
            object[] message = new object[1]{
                null
            };
            hitCollider.SendMessageUpwards("GetItemRef", message, SendMessageOptions.DontRequireReceiver);
            Item item = (Item)message[0];
            if (item != null && item.CanBePickedUp())
            {
                inventories[0].PickupItem(item);
            }
        }
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        for (int i = 0; i < inventories[0].size; i++)
        {
            if (inventories[0].IsSlotFilled(i))
            {
                eventCaller.GetComponent<CharacterController>().inventory.PickupItem(inventories[0].GetItemRef(i));
                inventories[0].DeleteItem(i);
                break;
            }
        }
    }

    public override void SecondaryMachineEvent(GameObject eventCaller)
    {
        isActive = !isActive;
    }
}