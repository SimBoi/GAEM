using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MachineEventType
{
    disabled,
    SingleEvent,
    ContinuousEvent
}

public class Machine : Block
{
    [Header("Machine Properties")]
    public int[] inventorySizes;
    public RectTransform machineUI;
    public Port[] ports;
    [HideInInspector] public Inventory[] inventories;

    public void Start()
    {
        GameObject tmp = GenerateMachineUI();
        if (tmp != null)
            machineUI = tmp.GetComponent<RectTransform>();
    }

    public virtual GameObject GenerateMachineUI()
    {
        return null;
    }

    public void CopyFrom(Machine source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Machine source)
    {
        this.inventorySizes = (int[])source.inventorySizes.Clone();
        this.inventories = new Inventory[source.inventories.Length];
        for (int i = 0; i < source.inventories.Length; i++)
        {
            this.inventories[i] = source.inventories[i].DeepClone();
        }
    }

    public override Item Clone()
    {
        Machine clone = new Machine();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Machine spawnedItem = (Machine)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override bool BlockInitialize()
    {
        if (!base.BlockInitialize()) return false;

        if (inventories == null || inventories.Length == 0)
        {
            inventories = new Inventory[inventorySizes.Length];
            for (int i = 0; i < inventories.Length; i++)
            {
                inventories[i] = new Inventory(inventorySizes[i]);
            }
        }
        if (ports == null || ports.Length == 0)
        {
            ports = new Port[6];
            for (int i = 0; i < ports.Length; i++)
            {
                ports[i] = new Port();
            }
        }

        return true;
    }

    public override void BlockFixedUpdate()
    {
        base.BlockFixedUpdate();
        foreach (Port port in ports)
        {
            if (port.GetType() == typeof(ItemPort))
            {
                if (port.type == PortType.input)
                {
                    if (((ItemPort)port).linkedInventory.SlotEmptiedFlag)
                    {
                        ((ItemPort)port).TransferItem();
                        ((ItemPort)port).linkedInventory.SlotEmptiedFlag = false;
                    }
                }
                else if (port.type == PortType.output)
                {
                    if (((ItemPort)port).linkedInventory.SlotFilledFlag)
                    {
                        ((ItemPort)port).TransferItem();
                        ((ItemPort)port).linkedInventory.SlotFilledFlag = false;
                    }
                }
            }
        }
    }

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, VoxelGrid voxelGrid, Vector3Int landPos)
    {
        Machine spawnedItem = (Machine)base.PlaceCustomBlock(globalPos, rotation, voxelGrid, landPos);

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborLandPos = landPos + VoxelGrid.FaceToDirection(face);
            Block neighborBlock = voxelGrid.GetCustomBlock(neighborLandPos);
            if (neighborBlock != null)
            {
                if (typeof(LinkBlock).IsAssignableFrom(neighborBlock.GetType()))
                    spawnedItem.TryLinkNetwork(face, ((LinkBlock)neighborBlock).network);
                if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    Network newNetwork = spawnedItem.ports[(int)face].CreateNewNetwork();
                    spawnedItem.TryLinkNetwork(face, newNetwork);
                    ((Machine)neighborBlock).TryLinkNetwork(VoxelGrid.GetOppositeFace(face), newNetwork);
                }
            }
        }

        return spawnedItem;
    }

    public override bool BreakCustomBlock(Vector3 pos = default, bool spawnItem = false)
    {
        if (!base.BreakCustomBlock(pos, spawnItem))
            return false;
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
            UnlinkNetwork(face);
        foreach (Inventory inventory in inventories)
        {
            if (inventory == null)
                continue;
            for (int i = 0; i < inventory.size; i++)
                inventory.ThrowItem(i, inventory.GetStackSize(i), pos);
        }
        return true;
    }

    public void TryLinkNetwork(Faces face, Network targetNetwork)
    {
        if (isDestroyed || targetNetwork == null)
            return;
        targetNetwork.LinkPort(ports[(int)face]);
    }

    public void UnlinkNetwork(Faces face)
    {
        Port port = ports[(int)face];
        if (port.network != null)
            port.network.UnlinkPort(port);
    }

    /// <summary>
    /// -------------------> machine events
    /// </summary>

    public MachineEventType PrimaryMachineEventType = MachineEventType.disabled;
    private bool isPrimaryMachineEventActive = false;
    private bool wasPrimaryMachineEventActive = false;
    private GameObject lastPrimaryMachineEventCaller;
    public MachineEventType SecondaryMachineEventType = MachineEventType.disabled;
    private bool isSecondaryMachineEventActive = false;
    private bool wasSecondaryMachineEventActive = false;
    private GameObject lastSecondaryMachineEventCaller;

    public void LateUpdate()
    {
        if (PrimaryMachineEventType != MachineEventType.disabled)
        {
            if (wasPrimaryMachineEventActive && !isPrimaryMachineEventActive)
                PrimaryMachineEventExit(lastPrimaryMachineEventCaller);
            wasPrimaryMachineEventActive = isPrimaryMachineEventActive;
            isPrimaryMachineEventActive = false;
        }
        if (SecondaryMachineEventType != MachineEventType.disabled)
        {
            if (wasSecondaryMachineEventActive && !isSecondaryMachineEventActive)
                SecondaryMachineEventExit(lastSecondaryMachineEventCaller);
            wasSecondaryMachineEventActive = isSecondaryMachineEventActive;
            isSecondaryMachineEventActive = false;
        }
    }

    public void PrimaryInteractEvent(GameObject eventCaller)
    {
        if (PrimaryMachineEventType == MachineEventType.SingleEvent)
        {
            isPrimaryMachineEventActive = true;
            lastPrimaryMachineEventCaller = eventCaller;
            if (wasPrimaryMachineEventActive == false)
                PrimaryMachineEvent(eventCaller);
        }
        else if (PrimaryMachineEventType == MachineEventType.ContinuousEvent)
        {
            isPrimaryMachineEventActive = true;
            lastPrimaryMachineEventCaller = eventCaller;
            PrimaryMachineEvent(eventCaller);
        }
    }

    public void SecondaryInteractEvent(GameObject eventCaller)
    {
        if (SecondaryMachineEventType == MachineEventType.SingleEvent)
        {
            isSecondaryMachineEventActive = true;
            lastSecondaryMachineEventCaller = eventCaller;
            if (wasSecondaryMachineEventActive == false)
                SecondaryMachineEvent(eventCaller);
        }
        else if (SecondaryMachineEventType == MachineEventType.ContinuousEvent)
        {
            isSecondaryMachineEventActive = true;
            lastSecondaryMachineEventCaller = eventCaller;
            SecondaryMachineEvent(eventCaller);
        }
    }

    public virtual void PrimaryMachineEvent(GameObject eventCaller) { }

    public virtual void PrimaryMachineEventExit(GameObject eventCaller) { }
    
    public virtual void SecondaryMachineEvent(GameObject eventCaller) { }

    public virtual void SecondaryMachineEventExit(GameObject eventCaller) { }
}