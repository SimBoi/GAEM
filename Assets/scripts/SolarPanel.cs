using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SolarPanel : Machine
{
    public float rate;
    public bool isActive = false;

    public override void Awake()
    {
        base.Awake();
        ports[(int)Faces.Up] = new EnergyPort(){
            type = PortType.output
        };
        ((EnergyPort)ports[(int)Faces.Up]).capacity = isActive ? rate : 0;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        isActive = !isActive;
        ((EnergyPort)ports[(int)Faces.Up]).capacity = isActive ? rate : 0;
    }
}