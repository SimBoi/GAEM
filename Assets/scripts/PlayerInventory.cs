using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerInventoryType
{
    Backpack,
    Hotbar,
    Armor
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
    public Health health;
    public Transform heldItemPos;
    public int heldItemIndex = -1; // held item index = -1 when no item is held
    public Inventory backpack;
    public Inventory hotbar;
    public Inventory armor;
    private float totalArmorProtection;

    private void Start()
    {
        heldItemIndex = -1;
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

    // inserts a copy of the item into the hotbar/backpack and despawns it
    public InsertResult PickupItem(Item item, out List<int> hotbarIndexes, out List<int> backpackIndexes, bool despawnItem = true)
    {
        backpackIndexes = new List<int>();
        InsertResult hotbarResult = hotbar.PickupItem(item, out hotbarIndexes, despawnItem);
        InsertResult backpackResult = InsertResult.Failure;
        if (hotbarResult != InsertResult.Success)
            backpackResult = backpack.PickupItem(item, out backpackIndexes, despawnItem);

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
            return EquipArmor(item, (ArmorPiece)index, out insertedItem) ? InsertResult.Success : InsertResult.Failure;
        }
    }

    public bool IsItemCompatible(PlayerInventoryType inventoryType, Item item, int index)
    {
        if (item == null)
        {
            return true;
        }

        if (inventoryType == PlayerInventoryType.Backpack)
        {
            return true;
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            return true;
        }
        else
        {
            if (item.GetType() != typeof(Wearable) || ((Wearable)item).armorPiece != (ArmorPiece)index)
                return false;
            return true;
        }
    }

    //returns true on success, false on failure
    public bool EquipArmor(Item item, ArmorPiece armorPiece, out Item insertedItem)
    {
        insertedItem = null;

        if (item.GetType() != typeof(Wearable) || ((Wearable)item).armorPiece != armorPiece)
            return false;
        if (armor.SetItemCopy(item, (int)armorPiece, out insertedItem) == InsertResult.Failure)
            return false;

        totalArmorProtection += ((Wearable)item).armorStrength;
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
    public int ConsumeFromTotalStack(Item item, int stackToConsume, out List<int> hotbarIndexes, out List<int> backpackIndexes)
    {
        int consumedStack = 0;
        bool consumedFromHeldItem = false;
        if (GetHeldItemRef() == item)
        {
            consumedStack += ConsumeFromStack(stackToConsume - consumedStack, heldItemIndex);
            if (consumedStack != 0)
                consumedFromHeldItem = true;
        }
        consumedStack += hotbar.ConsumeFromTotalStack(item, stackToConsume - consumedStack, out hotbarIndexes);
        if (consumedFromHeldItem)
            hotbarIndexes.Add(heldItemIndex);
        consumedStack += backpack.ConsumeFromTotalStack(item, stackToConsume - consumedStack, out backpackIndexes);
        return consumedStack;
    }

    public void ThrowHeldItem(int itemCount)
    {
        ThrowItem(PlayerInventoryType.Hotbar, heldItemIndex, itemCount);
    }

    public void ThrowItem(PlayerInventoryType inventoryType, int index, int itemCount)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
        {
            backpack.ThrowItem(index, itemCount, transform.position);
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            if (index == heldItemIndex)
            {
                if (GetHeldItemRef().GetStackSize() == 1)
                    LetGoOfHeldItem();
                hotbar.ThrowItem(index, itemCount, transform.position);
            }
            else
                hotbar.ThrowItem(index, itemCount, transform.position);
        }
        else
        {
            armor.ThrowItem(index, itemCount, transform.position);
        }
    }

    /*//returns true on success, false on failure (e.g. inventory is full)
    public bool InsertItemRef(Item item)
    {
        if (hotbar.InsertItemRef(item)) return true;
        if (backpack.InsertItemRef(item)) return true;
        return false;
    }*/

    public bool IsSlotFilled(PlayerInventoryType inventoryType, int index)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
            return backpack.IsSlotFilled(index);
        else if (inventoryType == PlayerInventoryType.Hotbar)
            return hotbar.IsSlotFilled(index);
        else
            return armor.IsSlotFilled(index);
    }

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

    public bool DoesInventoryExist(Inventory inventory, out PlayerInventoryType inventoryType)
    {
        inventoryType = PlayerInventoryType.Hotbar;

        if (inventory == hotbar)
            inventoryType = PlayerInventoryType.Hotbar;
        else if (inventory == backpack)
            inventoryType = PlayerInventoryType.Backpack;
        else if (inventory == armor)
            inventoryType = PlayerInventoryType.Armor;
        else
            return false;

        return true;
    }
}