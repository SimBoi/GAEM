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
        eventCaller.GetComponent<CharacterController>().ToggleInventoriesUI(machineUI);
    }
}
