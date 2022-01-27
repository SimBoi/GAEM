using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCable : LinkBlock
{
    public override Network CreateNewNetwork()
    {
        return ItemNetwork.CreateNewNetwork();
    }
}
