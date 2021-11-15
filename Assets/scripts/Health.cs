using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public float spawnHp;
    public float minHp;
    public float maxHp;
    public float armor;
    public float regenRate; //regen only works when the script is used as a component :(
    public float regenDelay;
    private float hp;
    private float regenTimer = 0;

    public Health(float spawnHp = 1, float minHp = 0, float maxHp = 1, float regenRate = 0, float regenDelay = 0, float armor = 0)
    {
        this.spawnHp = spawnHp;
        this.minHp = minHp;
        this.maxHp = maxHp;
        this.armor = armor;
        this.regenRate = regenRate;
        this.regenDelay = regenDelay;
        this.hp = spawnHp;
        this.regenTimer = 0;
    }

    /*public void copyDataFrom(Health source)
    {
        this.spawnHp = source.spawnHp;
        this.minHp = source.minHp;
        this.maxHp = source.maxHp;
        this.armor = source.armor;
        this.regenRate = source.regenRate;
        this.regenDelay = source.regenDelay;
        this.hp = source.spawnHp;
    }*/

    private void Start() 
    {
        hp = spawnHp;
        regenTimer = 0;
    }

    private void FixedUpdate()
    {
        if (regenTimer <= 0)
            AddHp(regenRate * Time.fixedDeltaTime);
        else
            regenTimer -= Time.fixedDeltaTime;
    }

    public void DealDamage(float dmg)
    {
        float effectiveDmg = Mathf.Clamp(dmg - armor, 0, maxHp);
        ChangeHp(-effectiveDmg);
        Debug.Log(hp);
        regenTimer = regenDelay;
    }

    public void AddHp(float heal)
    {
        ChangeHp(heal);
    }

    private void ChangeHp(float hpChange)
    {
        hp = Mathf.Clamp(hp + hpChange, minHp, maxHp);
    }

    public float GetHp()
    {
        return hp;
    }

    public float GetMaxHp()
    {
        return maxHp;
    }
}
