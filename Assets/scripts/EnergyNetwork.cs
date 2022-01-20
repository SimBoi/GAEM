using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyPort : Port
{
    public int capacity = 0;
    public int peakDemand = 0;
    public int input = 0;
    public int output = 0;

    public void ChangeSpecs(int capacity, int peakDemand, int input, int output)
    {
        this.capacity = capacity;
        this.peakDemand = peakDemand;
        this.input = input;
        this.output = output;

        ((EnergyNetwork)network).CalcSpecs();
    }
}

public class EnergyNetwork : Network
{
    int capacity = 0;
    int peakDemand = 0;

    public void CalcSpecs()
    {
        capacity = 0;
        peakDemand = 0;
        foreach (EnergyPort port in linkedPorts)
        {
            capacity += port.capacity;
            peakDemand += port.peakDemand;
        }
        int inputPercentage = Mathf.Clamp(peakDemand / capacity, 0, 1);
        int outputPercentage = Mathf.Clamp(capacity / peakDemand, 0, 1);
        foreach (EnergyPort port in linkedPorts)
        {
            port.output = port.capacity * inputPercentage;
            port.input = port.peakDemand * outputPercentage;
        }
    }

    public override void LinkPort(Port port)
    {
        base.LinkPort(port);
        CalcSpecs();
    }

    public override void UnlinkPort(Port port)
    {
        base.UnlinkPort(port);
        CalcSpecs();
    }
}