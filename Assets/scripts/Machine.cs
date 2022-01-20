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
    public int peakEnergyDemand = 0;

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

    public void TryLinkNetwork(Faces face, Network targetNetwork)
    {
        Port port = ports[(int)face];
        if (port.type != PortType.disabled && targetNetwork.GetType() == typeof(EnergyNetwork) && port.GetType() == typeof(EnergyPort))
        {
            targetNetwork.LinkPort(port);
        }
    }

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