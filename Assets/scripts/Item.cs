using  System.Collections;
using  System.Collections.Generic;
using  UnityEngine;

public class Item : MonoBehaviour
{
    public ItemPrefabs itemPrefabs;
    public string name;
    public int id;
    public int maxStackSize;
    public float despawnTime = 300;
    public float maxDurability;
    public float spawnDurability;
    public  int stackSize;
    public Health health;
    public bool preventDespawn;
    public float despawnTimer = 0;
    public bool isDestroyed = false;

    /*public virtual void Construct(ItemPrefabs itemPrefabs, string name, int id, int maxStackSize, float maxDurability, float spawnDurability, int stackSize, bool preventDespawn)
    {
        this.itemPrefabs = itemPrefabs;
        this.name = name;
        this.id = id;
        this.maxStackSize = maxStackSize;
        this.maxDurability = maxDurability;
        this.spawnDurability = spawnDurability;
        this.stackSize = stackSize;
        this.health = new Health(spawnDurability, 0, maxDurability, 0, 0);
        this.preventDespawn = preventDespawn;
        this.despawnTimer = 0;
    }*/

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
        this.despawnTimer = source.despawnTimer;
        this.isDestroyed = source.isDestroyed;
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

    public void ResetDespawnTimer()
    {
        despawnTimer = 0;
    }

    public virtual Item Spawn(Vector3 pos, Quaternion rotation, Transform parent = null)
    {
        GameObject newItem;
        if (parent == null)
            newItem = Instantiate(itemPrefabs.prefabs[id], pos, rotation);
        else
            newItem = Instantiate(itemPrefabs.prefabs[id], pos, rotation, parent);

        Item spawnedItem = newItem.GetComponent<Item>();
        spawnedItem.CopyFrom(this);
        return spawnedItem;
    }

    public virtual bool Despawn()
    {
        if (gameObject == null)
            return false;
        Destroy(gameObject);
        isDestroyed = true;
        return true;
    }

    private void FixedUpdate()
    {
        if (!preventDespawn)
        {
            if (despawnTimer >= despawnTime) Despawn();
            despawnTimer += Time.fixedDeltaTime;
        }
    }

    public virtual void PrimaryEvent(GameObject eventCaller)
    {
        SendMessageUpwards("CustomPrimaryEvent", eventCaller, SendMessageOptions.DontRequireReceiver);
    }

    public virtual void SecondaryEvent(GameObject eventCaller)
    {
        SendMessageUpwards("CustomSecondaryEvent", eventCaller, SendMessageOptions.DontRequireReceiver);
    }
}