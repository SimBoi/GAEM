using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chest : Machine
{
    public override void InitializeFields()
    {
        base.InitializeFields();
        if (!IsServer) return;

        ports[(int)Faces.Down] = new ItemPort()
        {
            type = PortType.input,
            linkedInventory = inventories[0]
        };
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        eventCaller.GetComponent<CharacterController>().ToggleInventoriesUI(machineUI);
    }
}
