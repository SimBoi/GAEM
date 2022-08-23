using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyPort : Port
{
    private float _capacity = 0;
    private float _peakDemand = 0;
    private float _input = 0;
    private float _output = 0;

    public float capacity
    {
        get { return _capacity; }
        set { _capacity = value; CalcNetworkSpecs(); }
    }

    public float peakDemand
    {
        get { return _peakDemand; }
        set { _peakDemand = value; CalcNetworkSpecs(); }
    }

    public float input
    {
        get { return _input; }
        set { _input = value; }
    }

    public float output
    {
        get { return _output; }
        set { _output = value; }
    }

    private void CalcNetworkSpecs()
    {
        if (network != null)  ((EnergyNetwork)network).CalcSpecs();
    }

    public override Network CreateNewNetwork()
    {
        return EnergyNetwork.CreateNewNetwork();
    }
}

public class EnergyNetwork : Network
{
    float capacity = 0;
    float peakDemand = 0;

    public void CalcSpecs()
    {
        capacity = 0;
        peakDemand = 0;
        foreach (EnergyPort port in linkedPorts)
        {
            capacity += port.capacity;
            peakDemand += port.peakDemand;
        }
        float inputPercentage = capacity != 0 ? Mathf.Clamp(peakDemand / capacity, 0, 1) : 0;
        float outputPercentage = peakDemand != 0 ? Mathf.Clamp(capacity / peakDemand, 0, 1) : 0;
        foreach (EnergyPort port in linkedPorts)
        {
            port.output = port.capacity * inputPercentage;
            port.input = port.peakDemand * outputPercentage;
        }
    }

    public override bool LinkPort(Port port)
    {
        if (port.type != PortType.disabled && port.GetType() == typeof(EnergyPort) && base.LinkPort(port))
        {
            CalcSpecs();
            return true;
        }
        return false;
    }

    public override bool UnlinkPort(Port port)
    {
        if (base.UnlinkPort(port))
        {
            ((EnergyPort)port).input = 0;
            ((EnergyPort)port).output = 0;
            CalcSpecs();
            return true;
        }
        return false;
    }

    static public new Network CreateNewNetwork()
    {
        return new EnergyNetwork();
    }
}