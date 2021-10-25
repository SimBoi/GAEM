using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hunger : MonoBehaviour
{
    public Health health;
    public float hungerDmgRate; //hungerDmg only works when the script is used as a component :(
    public float spawnFulness;
    public float minFullness;
    public float maxFullness;
    public float starvationRate; //starvation only works when the script is used as a component :(
    private float fullness;
    
    private void Start()
    {
        fullness = spawnFulness;
    }

    private void FixedUpdate()
    {
        Starve(starvationRate * Time.fixedDeltaTime); // starve every fixed update by starvationRate per second
        if (fullness <= minFullness)
            health.DealDamage(hungerDmgRate * Time.fixedDeltaTime); // deal damage after starving past minFullness
    }

    public void Starve(float starvation)
    {
        ChangeFullness(-starvation);
    }

    public void DecreaseHunger(float fulfill)
    {
        ChangeFullness(fulfill);
    }

    private void ChangeFullness(float hungerChange)
    {
        fullness += hungerChange;
        fullness = Mathf.Clamp(fullness, minFullness, maxFullness);
    }

    public float GetHunger()
    {
        return fullness;
    }
}
