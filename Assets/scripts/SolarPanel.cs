using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarPanel : Machine
{
    public float rate;
    public bool isActive = false;

    public void CopyFrom(SolarPanel source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(SolarPanel source)
    {
        this.rate = source.rate;
    }

    public override Item Clone()
    {
        SolarPanel clone = new SolarPanel();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        SolarPanel spawnedItem = (SolarPanel)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override bool BlockInitialize()
    {
        if (!base.BlockInitialize()) return false;

        ports[(int)Faces.Up] = new EnergyPort(){
            type = PortType.output
        };
        ((EnergyPort)ports[(int)Faces.Up]).capacity = isActive ? rate : 0;

        return true;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        isActive = !isActive;
        ((EnergyPort)ports[(int)Faces.Up]).capacity = isActive ? rate : 0;
    }
}