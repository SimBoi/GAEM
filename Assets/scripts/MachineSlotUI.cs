using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineSlotUI : InventorySlotUI
{
    public Machine machine;
    public int inventoryIndex;

    public void Start()
    {
        inventory = machine.inventories[inventoryIndex];
    }
}