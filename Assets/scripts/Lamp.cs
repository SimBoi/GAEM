using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lamp : Machine
{
    public float peakEnergyDemand;
    public float maxIntensity;
    public Light pointLight;
    public bool isActive = false;

    public void CopyFrom(Lamp source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(Lamp source)
    {
        this.peakEnergyDemand = source.peakEnergyDemand;
        this.maxIntensity = source.maxIntensity;
    }

    public override Item Clone()
    {
        Lamp clone = new Lamp();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        Lamp spawnedItem = (Lamp)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override bool BlockInitialize()
    {
        if (!base.BlockInitialize()) return false;

        ports[(int)Faces.Down] = new EnergyPort()
        {
            type = PortType.input
        };
        ((EnergyPort)ports[(int)Faces.Down]).peakDemand = isActive ? peakEnergyDemand : 0;

        return true;
    }

    public override void BlockUpdate()
    {
        pointLight.intensity = ((EnergyPort)ports[(int)Faces.Down]).input * maxIntensity / peakEnergyDemand;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        isActive = !isActive;
        ((EnergyPort)ports[(int)Faces.Down]).peakDemand = isActive ? peakEnergyDemand : 0;
    }
}