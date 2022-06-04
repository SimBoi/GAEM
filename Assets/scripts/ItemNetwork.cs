using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPort : Port
{
    public Inventory linkedInventory = null;

    public void TransferItem()
    {
        if (network == null || linkedInventory == null) return;
        ((ItemNetwork)network).TransferItem();
    }

    public override Network CreateNewNetwork()
    {
        return ItemNetwork.CreateNewNetwork();
    }
}

public class ItemNetwork : Network
{
    public void TransferItem()
    {
        Debug.Log("transfer called");
        foreach (ItemPort outputPort in linkedPorts)
        {
            if (outputPort.type == PortType.output && outputPort.linkedInventory != null)
            {
                for (int i = 0; i < outputPort.linkedInventory.size; i++)
                {
                    Item item = outputPort.linkedInventory.GetItemRef(i);
                    if (item != null)
                    {
                        foreach (ItemPort inputPort in linkedPorts)
                        {
                            if (inputPort.type == PortType.input && inputPort.linkedInventory != null)
                            {
                                inputPort.linkedInventory.InsertItemCopy(item, out _, out _);
                                if (item.GetStackSize() == 0)
                                {
                                    outputPort.linkedInventory.DeleteItemServerRpc(i);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public override bool LinkPort(Port port)
    {
        if (port.type != PortType.disabled && port.GetType() == typeof(ItemPort) && base.LinkPort(port))
        {
            TransferItem();
            return true;
        }
        return false;
    }

    static public new Network CreateNewNetwork()
    {
        return new ItemNetwork();
    }
}
