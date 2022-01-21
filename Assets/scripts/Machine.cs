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
    public int[] inventorySizes;
    public Port[] ports;

    [HideInInspector] public Inventory[] inventories;

    public virtual void Awake()
    {
        inventories = new Inventory[inventorySizes.Length];
        for (int i = 0; i< inventories.Length; i++)
        {
            inventories[i] = new Inventory(inventorySizes[i]);
        }
        ports = new Port[6];
        for (int i = 0; i < ports.Length; i++)
        {
            ports[i] = new Port();
        }
    }

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int chunkPos)
    {
        Machine spawnedItem = (Machine)base.PlaceCustomBlock(globalPos, rotation, parentChunk, chunkPos);

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborPos = chunkPos + Chunk.FaceToDirection(face);
            Block neighborBlock = parentChunk.GetCustomBlock(neighborPos);
            if (neighborBlock != null)
            {
                if (typeof(LinkBlock).IsAssignableFrom(neighborBlock.GetType()))
                    spawnedItem.TryLinkNetwork(face, ((LinkBlock)neighborBlock).network);
            }
        }

        return spawnedItem;
    }

    public void TryLinkNetwork(Faces face, Network targetNetwork)
    {
        Port port = ports[(int)face];
        if (port.type != PortType.disabled && targetNetwork.GetType() == typeof(EnergyNetwork) && port.GetType() == typeof(EnergyPort))
        {
            targetNetwork.LinkPort(port);
        }
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

    private void LateUpdate()
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