using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lamp : Machine
{
    public float peakEnergyDemand;
    public float maxIntensity;
    public Light pointLight;
    public bool isActive = false;

    public override void Awake()
    {
        base.Awake();
        ports[(int)Faces.Down] = new EnergyPort()
        {
            type = PortType.input
        };
        ((EnergyPort)ports[(int)Faces.Down]).peakDemand = isActive ? peakEnergyDemand : 0;
    }

    private void Update()
    {
        pointLight.intensity = ((EnergyPort)ports[(int)Faces.Down]).input * maxIntensity / peakEnergyDemand;
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        isActive = !isActive;
        ((EnergyPort)ports[(int)Faces.Down]).peakDemand = isActive ? peakEnergyDemand : 0;
    }
}