using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnergyCable : LinkBlock
{
    public override Network CreateNewNetwork()
    {
        return EnergyNetwork.CreateNewNetwork();
    }
}