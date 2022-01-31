using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Machine
{
    public override bool BlockInitialize()
    {
        if (!base.BlockInitialize()) return false;

        ports[(int)Faces.Down] = new ItemPort() {
            type = PortType.input,
            linkedInventory = inventories[0]
        };
        
        return true;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        for (int i = 0; i < inventories[0].size; i++)
        {
            if (inventories[0].IsSlotFilled(i))
            {
                eventCaller.GetComponent<CharacterController>().inventory.PickupItem(inventories[0].GetItemRef(i), out _, out _);
                inventories[0].DeleteItem(i);
                break;
            }
        }
    }
}
