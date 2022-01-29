using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaPickup : Machine
{
    public float radius;
    public bool isActive = false;

    public void CopyFrom(AreaPickup source)
    {
        base.CopyFrom(source);
        CopyFromDerived(source);
    }

    public void CopyFromDerived(AreaPickup source)
    {
        this.radius = source.radius;
    }

    public override Item Clone()
    {
        AreaPickup clone = new AreaPickup();
        clone.CopyFrom(this);
        return clone;
    }

    public override Item Spawn(bool isHeld, Vector3 pos, Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        AreaPickup spawnedItem = (AreaPickup)base.Spawn(isHeld, pos, rotation, parent);
        spawnedItem.CopyFromDerived(this);
        return spawnedItem;
    }

    public override bool BlockInitialize()
    {
        if (!base.BlockInitialize()) return false;

        ports[(int)Faces.Up] = new ItemPort()
        {
            type = PortType.output,
            linkedInventory = inventories[0]
        };

        return true;
    }

    public override void BlockUpdate()
    {
        base.BlockUpdate();
        if (!isActive) return;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
        foreach (var hitCollider in hitColliders)
        {
            object[] message = new object[1]{
                null
            };
            hitCollider.SendMessageUpwards("GetItemRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Item item = (Item)message[0];
            if (item != null && item.CanBePickedUp())
            {
                inventories[0].PickupItem(item);
            }
        }
    }

    public override void PrimaryMachineEvent(GameObject eventCaller)
    {
        for (int i = 0; i < inventories[0].size; i++)
        {
            if (inventories[0].IsSlotFilled(i))
            {
                eventCaller.GetComponent<CharacterController>().inventory.PickupItem(inventories[0].GetItemRef(i));
                inventories[0].DeleteItem(i);
                break;
            }
        }
    }

    public override void SecondaryMachineEvent(GameObject eventCaller)
    {
        isActive = !isActive;
    }
}