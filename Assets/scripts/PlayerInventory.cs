using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerInventoryType
{
    Backpack,
    Hotbar,
    Armor,
    Null
}

public enum ArmorPiece
{
    Helmet,
    Chestplate,
    Leggings,
    Boots
}

public class PlayerInventory : MonoBehaviour
{
    public int backpackSize;
    public int hotbarSize;
    public Health health;
    public Transform heldItemPos;
    public int heldItemIndex = -1; // held item index = -1 when no item is held
    private Inventory backpack;
    private Inventory hotbar;
    private Inventory armor;
    private float totalArmorProtection;

    private void Start()
    {
        heldItemIndex = -1;
        backpack = new Inventory(backpackSize);
        hotbar = new Inventory(hotbarSize);
        armor = new Inventory(System.Enum.GetNames(typeof(ArmorPiece)).Length);
    }

    private void Update()
    {
        if (heldItemIndex != -1 && GetHeldItemRef().isDestroyed == true)
        {
            hotbar.DeleteItem(heldItemIndex);
            heldItemIndex = -1;
        }
    }

    public Inventory GetInventory(PlayerInventoryType type)
    {
        if (type == PlayerInventoryType.Hotbar)
        {
            return hotbar;
        }
        if (type == PlayerInventoryType.Backpack)
        {
            return backpack;
        }
        return armor;
    }

    public void SwitchToItem(int index)
    {
        if (index == heldItemIndex) return;

        // let go of currently held item
        LetGoOfHeldItem();

        // check if there is an item to hold
        if (hotbar.IsSlotFilled(index))
        {
            // spawn item to be held and update inventory to the new spawned item and update held item index
            Item spawnedItem = hotbar.GetItemRef(index).Spawn(true, heldItemPos.position, heldItemPos.rotation, heldItemPos);
            spawnedItem.isHeld = true;
            spawnedItem.preventDespawn = true;
            spawnedItem.HoldEvent(gameObject);
            hotbar.DeleteItem(index);
            hotbar.SetItemRef(spawnedItem, index);
            heldItemIndex = index;
        }
    }

    public ref Item GetHeldItemRef()
    {
        return ref hotbar.GetItemRef(heldItemIndex);
    }

    public void LetGoOfHeldItem()
    {
        if (heldItemIndex == -1) return;

        // return previously selected item to the inventory by saving a copy of the item in the inventory and despawning the held item
        Item heldItem = GetHeldItemRef();
        heldItem.isHeld = false;
        heldItem.preventDespawn = false;
        hotbar.DeleteItem(heldItemIndex);
        hotbar.SetItemCopy(heldItem, heldItemIndex, out _);
        heldItem.Despawn();

        // set held item index to holding nothing
        heldItemIndex = -1;
    }

    // returns true on success, false if inventory is full
    public InsertResult PickupItem(Item item)
    {
        InsertResult hotbarResult = hotbar.PickupItem(item);
        InsertResult backpackResult = InsertResult.Failure;
        if (hotbarResult != InsertResult.Success)
            backpackResult = backpack.PickupItem(item);

        if (hotbarResult == InsertResult.Success || backpackResult == InsertResult.Success)
            return InsertResult.Success;
        if (hotbarResult == InsertResult.Partial || backpackResult == InsertResult.Partial)
            return InsertResult.Partial;
        return InsertResult.Failure;
    }

    public InsertResult SetItemCopy(PlayerInventoryType inventoryType, Item item, int index, out Item insertedItem)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
        {
            return backpack.SetItemCopy(item, index, out insertedItem);
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            return hotbar.SetItemCopy(item, index, out insertedItem);
        }
        else
        {
            return armor.SetItemCopy(item, index, out insertedItem);
        }
    }

    //returns true on success, false on failure
    public bool EquipArmor(Item item, ArmorPiece armorPiece, float protection)
    {
        if (armor.PickupItem(item, (int)armorPiece) == InsertResult.Failure)
            return false;

        totalArmorProtection += protection;
        return true;
    }

    public int GetStackSize(int index, PlayerInventoryType inventoryType = PlayerInventoryType.Hotbar)
    {
        if (inventoryType == PlayerInventoryType.Hotbar)
        {
            return hotbar.GetStackSize(index);
        }
        else
        {
            return backpack.GetStackSize(index);
        }
    }

    public int GetTotalStackSize(Item item)
    {
        return hotbar.GetTotalStackSize(item) + backpack.GetTotalStackSize(item);
    }

    // returns number of items consumed
    public int ConsumeFromStack(int stackToConsume, int index, PlayerInventoryType inventoryType = PlayerInventoryType.Hotbar)
    {
        if (inventoryType == PlayerInventoryType.Hotbar)
        {
            if (index == heldItemIndex && GetHeldItemRef().GetStackSize() <= stackToConsume)
            {
                LetGoOfHeldItem();
            }
            return hotbar.ConsumeFromStack(index, stackToConsume);
        }
        else
        {
            return backpack.ConsumeFromStack(index, stackToConsume);
        }
    }

    // returns number of items consumed
    public int ConsumeFromTotalStack(Item item, int stackToConsume)
    {
        int consumedStack = 0;
        if (GetHeldItemRef() == item)
        {
            consumedStack += ConsumeFromStack(stackToConsume - consumedStack, heldItemIndex);
        }
        consumedStack += hotbar.ConsumeFromTotalStack(item, stackToConsume - consumedStack);
        consumedStack += backpack.ConsumeFromTotalStack(item, stackToConsume - consumedStack);
        return consumedStack;
    }

    public Item ThrowHeldItem(int itemCount)
    {
        return ThrowItem(PlayerInventoryType.Hotbar, heldItemIndex, itemCount);
    }

    public Item ThrowItem(PlayerInventoryType inventoryType, int index, int itemCount)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
        {
            return backpack.ThrowItem(index, itemCount, transform.position);
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            if (index == heldItemIndex)
            {
                if (GetHeldItemRef().GetStackSize() == 1)
                    LetGoOfHeldItem();
                Item thrownItem = hotbar.ThrowItem(index, itemCount, transform.position);
                thrownItem.isHeld = false;
                return thrownItem;
            }
            return hotbar.ThrowItem(index, itemCount, transform.position);
        }
        else
        {
            return armor.ThrowItem(index, itemCount, transform.position);
        }
    }

    /*//returns true on success, false on failure (e.g. inventry is full)
    public bool InsertItemRef(Item item)
    {
        if (hotbar.InsertItemRef(item)) return true;
        if (backpack.InsertItemRef(item)) return true;
        return false;
    }*/

    public ref Item GetItemRef(PlayerInventoryType inventoryType, int index)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
            return ref backpack.GetItemRef(index);
        else if (inventoryType == PlayerInventoryType.Hotbar)
            return ref hotbar.GetItemRef(index);
        else
            return ref armor.GetItemRef(index);
    }

    public Item GetItemCopy(PlayerInventoryType inventoryType, int index)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
        {
            return backpack.GetItemCopy(index);
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            return hotbar.GetItemCopy(index);
        }
        else
        {
            return armor.GetItemCopy(index);
        }
    }

    public bool DeleteItem(PlayerInventoryType inventoryType, int index)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
        {
            return backpack.DeleteItem(index);
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            if (index == heldItemIndex)
                LetGoOfHeldItem();
            return hotbar.DeleteItem(index);
        }
        else
        {
            return armor.DeleteItem(index);
        }
    }

    public PlayerInventoryType GetInventoryType(Inventory inventory)
    {
        if (inventory == backpack) return PlayerInventoryType.Backpack;
        if (inventory == hotbar) return PlayerInventoryType.Hotbar;
        if (inventory == hotbar) return PlayerInventoryType.Hotbar;
        return PlayerInventoryType.Null;
    }
}