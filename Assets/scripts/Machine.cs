using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Port
{
    disabled,
    itemInput,
    itemOutput,
    energyInput,
    energyOutput
}

public class Machine : Block
{
    public int[] inventorySizes;
    public Port[] portTypes;
    public int[] portConnections;
    public int[] faces = new int[6];

    [HideInInspector] public Inventory[] inventories;
    [HideInInspector] public int energyInput;
    [HideInInspector] public int energyOutput;

    public virtual void Awake()
    {
        inventories = new Inventory[inventorySizes.Length];
        for (int i = 0; i< inventories.Length; i++)
        {
            inventories[i] = new Inventory(inventorySizes[i]);
            Debug.Log(inventories[i].size);
        }
    }
    
}
