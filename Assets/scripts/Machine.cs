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

    private bool woke;
    public virtual void Awake()
    {
        if (woke) return;
        woke = true;
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

    public override Item PlaceCustomBlock(Vector3 globalPos, Quaternion rotation, Chunk parentChunk, Vector3Int landPos)
    {
        Machine spawnedItem = (Machine)base.PlaceCustomBlock(globalPos, rotation, parentChunk, landPos);
        spawnedItem.Awake();

        object[] message = new object[1]{
                null
            };
        parentChunk.SendMessageUpwards("GetLandRef", message);
        Land land = (Land)message[0];

        foreach (Faces face in Enum.GetValues(typeof(Faces)))
        {
            Vector3Int neighborLandPos = landPos + Chunk.FaceToDirection(face);
            Block neighborBlock = land.GetCustomBlock(neighborLandPos);
            if (neighborBlock != null)
            {
                if (typeof(LinkBlock).IsAssignableFrom(neighborBlock.GetType()))
                    spawnedItem.TryLinkNetwork(face, ((LinkBlock)neighborBlock).network);
                if (typeof(Machine).IsAssignableFrom(neighborBlock.GetType()))
                {
                    Network newNetwork = spawnedItem.ports[(int)face].CreateNewNetwork();
                    spawnedItem.TryLinkNetwork(face, newNetwork);
                    ((Machine)neighborBlock).TryLinkNetwork(Chunk.GetOppositeFace(face), newNetwork);
                }
            }
        }

        return spawnedItem;
    }

    public override bool BreakCustomBlock(bool spawnItem = false, Vector3 pos = default)
    {
        if (!base.BreakCustomBlock(spawnItem, pos))
            return false;
        foreach (Faces face in Enum.GetValues(typeof(Faces)))
            UnlinkNetwork(face);
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

    public Network CreateNewNetwork(Type portType)
    {
        if (portType == typeof(EnergyPort))
            return new EnergyNetwork();
        return null;
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