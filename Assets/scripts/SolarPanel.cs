using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarPanel : Machine
{
    public float rate;
    public bool isActive = false;

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