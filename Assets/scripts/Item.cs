using System;
using  System.Collections;
using  System.Collections.Generic;
using  UnityEngine;

public class Item : MonoBehaviour
{
    public ItemPrefabs itemPrefabs;
    public string name;
    public int id;
    public int maxStackSize;
    public float maxDurability;
    public float spawnDurability;
    public  int stackSize;
    public Health health;
    public bool preventDespawn;
    public float despawnTime = 300;
    public float pickupDelay;
    public float timeSinceSpawn = 0;
    public bool isHeld;
    public bool isDestroyed = false;
    public Sprite icon;
    public GameObject ui = null;

    public void CopyFrom(Item source)
    {
        this.itemPrefabs = source.itemPrefabs;
        this.name = source.name;
        this.id = source.id;
        this.maxStackSize = source.maxStackSize;
        this.maxDurability = source.maxDurability;
        this.spawnDurability = source.spawnDurability;
        this.stackSize = source.stackSize;
        if (source.health == null)
            this.health = new Health(source.spawnDurability, 0, source.maxDurability, 0, 0);
        else
            this.health = new Health(source.health.GetHp(), 0, source.maxDurability, 0, 0);
        this.preventDespawn = source.preventDespawn;
        this.despawnTime = source.despawnTime;
        this.pickupDelay = source.pickupDelay;
        this.timeSinceSpawn = source.timeSinceSpawn;
        this.isHeld = source.isHeld;
        this.isDestroyed = source.isDestroyed;
        this.icon = source.icon;
    }

    public virtual Item Clone()
    {
        Item clone = new Item();
        clone.CopyFrom(this);
        return clone;
    }

    public static bool operator ==(Item a, Item b)
    {
        if (a is null || b is null)
        {
            if (a is null && b is null) return true;
            return false;
        }

        if (a.id == b.id && a.name == b.name)
            return true;
        return false;
    }
    
    public static bool operator !=(Item a, Item b)
    {
        if (a == b)
            return false;
        return true;
    }

    //returns new stacksize on success, returns -1 on error
    public int ChangeStackSize(int stackChange)
    {
        if (stackSize + stackChange < 0 || stackSize + stackChange > maxStackSize) return -1;
            stackSize += stackChange;
        if (stackSize <= 0)
            Despawn();
        return stackSize;
    }

    //returns stacksize
    public int DamageItem(float dmg)
    { 
        health.DealDamage(dmg);
        if (health.GetHp() <= 0) ChangeStackSize(-1);
        return stackSize;
    }
    
    public int GetStackSize()
    {
        return stackSize;
    }
    
    public void SetStackSize(int newSize)
    {
        stackSize = newSize;
    }

    public float GetDurability()
    {
        return health.GetHp();
    }

    public virtual Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        GameObject newItem;
        if (parent == null)
            newItem = Instantiate(itemPrefabs.prefabs[id], pos, rotation);
        else
            newItem = Instantiate(itemPrefabs.prefabs[id], pos, rotation, parent);

        Item spawnedItem = newItem.GetComponent<Item>();
        spawnedItem.CopyFrom(this);
        spawnedItem.isHeld = isHeld;
        spawnedItem.timeSinceSpawn = 0;
        if (isHeld && spawnedItem.ui != null)
        {
            spawnedItem.ui.SetActive(true);
        }
        return spawnedItem;
    }

    public virtual bool Despawn()
    {
        try
        {
            if (gameObject != null)
                Destroy(gameObject);
        }
        catch (NullReferenceException) { }
        isDestroyed = true;
        return true;
    }

    public void FixedUpdate()
    {
        if (!preventDespawn)
        {
            if (timeSinceSpawn >= despawnTime) Despawn();
        }
        if (timeSinceSpawn < despawnTime || timeSinceSpawn < pickupDelay)
        timeSinceSpawn += Time.fixedDeltaTime;
    }

    public virtual bool CanBePickedUp()
    {
        if (timeSinceSpawn >= pickupDelay && !isHeld && !isDestroyed) return true;
        return false;
    }

    public virtual void PickupEvent() { }
    
    public virtual void HoldEvent(GameObject eventCaller) { }

    public virtual void PrimaryItemEvent(GameObject eventCaller)
    {
        SendMessage("CustomPrimaryItemEvent", eventCaller, SendMessageOptions.DontRequireReceiver);
    }

    public virtual void SecondaryItemEvent(GameObject eventCaller)
    {
        SendMessage("CustomSecondaryItemEvent", eventCaller, SendMessageOptions.DontRequireReceiver);
    }

    // message[0] = (Land)return
    public void GetItemRefMsg(object[] message)
    {
        message[0] = this;
    }
}