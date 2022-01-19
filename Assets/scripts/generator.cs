using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : Machine
{
    public float rate;
    public float efficiency;
    public float maxFuel;
    public float fuel;
    public bool active; 

    private void Update()
    {
        if (fuel >= 0)
        {
            active = true; 
        }
        else
        {
            active = false;
        }

        if (inventories[0].IsSlotFilled(0) && fuel+10 <= maxFuel)
        {
            fuel += 10;
            inventories[0].GetItemRef(0).ChangeStackSize(-1);
        }
        if (fuel >= 0)
        {
            if (fuel >= rate * Time.deltaTime / efficiency)
            {
                fuel -= rate * Time.deltaTime / efficiency;
            }
            else
            {
                fuel = 0;
            }
        }
        
    }
    
    private void BurnItem()
    {

    }
}
