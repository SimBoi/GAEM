using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
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

public class PlayerInventory : NetworkBehaviour
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
            hotbar.DeleteItemServerRpc(heldItemIndex);
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

    [ServerRpc]
    public void SwitchToItemServerRpc(int index)
    {
        if (index == heldItemIndex) return;

        // let go of currently held item
        LetGoOfHeldItemServerRpc();

        // check if there is an item to hold
        if (hotbar.IsSlotFilled(index))
        {
            // spawn item to be held and update inventory to the new spawned item and update held item index
            Item spawnedItem = hotbar.GetItemRef(index).Spawn(true, heldItemPos.position, heldItemPos.rotation, heldItemPos);
            spawnedItem.preventDespawn = true;
            spawnedItem.HoldEvent(gameObject);
            hotbar.DeleteItemServerRpc(index);
            hotbar.SetItemRef(spawnedItem, index);
            heldItemIndex = index;
        }

        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { OwnerClientId }
            }
        };
        SwitchToItemClientRpc(index, clientRpcParams);
    }

    [ClientRpc]
    public void SwitchToItemClientRpc(int index, ClientRpcParams clientRpcParams)
    {

    }

    public ref Item GetHeldItemRef()
    {
        return ref hotbar.GetItemRef(heldItemIndex);
    }

    [ServerRpc]
    public void LetGoOfHeldItemServerRpc()
    {
        if (heldItemIndex == -1) return;

        // return previously selected item to the inventory by saving a copy of the item in the inventory and despawning the held item
        Item heldItem = GetHeldItemRef();
        heldItem.isHeld = false;
        heldItem.preventDespawn = false;
        hotbar.DeleteItemServerRpc(heldItemIndex);
        hotbar.SetItemCopy(heldItem, heldItemIndex, out _);
        heldItem.Despawn();

        // set held item index to holding nothing
        heldItemIndex = -1;
    }

    [ClientRpc]
    public void LetGoOfHeldItemClientRpc()
    {

    }

    // inserts a copy of the item into the hotbar/backpack and despawns it, should only be called on the server
    public InsertResult PickupItem(Item item, out List<int> hotbarIndexes, out List<int> backpackIndexes, bool despawnItem = true)
    {
        hotbarIndexes = new List<int>();
        backpackIndexes = new List<int>();

        if (!(IsServer || IsHost)) return InsertResult.Failure;

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
        insertedItem = null;

        if (!(IsServer || IsHost)) return InsertResult.Failure;

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

    // consumes from the stack of a specific slot, returns number of items consumed, should only be called on the server
    public int ConsumeFromStack(int stackToConsume, int index, PlayerInventoryType inventoryType = PlayerInventoryType.Hotbar)
    {
        if (!(IsServer || IsHost)) return 0;

        if (inventoryType == PlayerInventoryType.Hotbar)
        {
            if (index == heldItemIndex && GetHeldItemRef().GetStackSize() <= stackToConsume) LetGoOfHeldItemServerRpc();
            return hotbar.ConsumeFromStack(index, stackToConsume);
        }
        else
        {
            return backpack.ConsumeFromStack(index, stackToConsume);
        }
    }

    [ServerRpc]
    public void ConsumeFromStackServerRpc(int index, int stackToConsume)
    {
        ConsumeFromStack(index, stackToConsume);
    }

    // consumes from the collective stack of all slots of the same item parameter type, returns number of items consumed, should only be called on the server
    public int ConsumeFromTotalStack(Item item, int stackToConsume, out List<int> hotbarIndexes, out List<int> backpackIndexes)
    {
        hotbarIndexes = new List<int>();
        backpackIndexes = new List<int>();

        if (!(IsServer || IsHost)) return 0;

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

    [ServerRpc]
    public void ConsumeFromTotalStackServerRpc(int itemID, byte[] serializedItem, int stackToConsume)
    {
        ConsumeFromTotalStack(Item.Deserialize(itemID, serializedItem), stackToConsume, out _, out _);
    }

    public void ThrowHeldItem(int itemCount)
    {
        ThrowItem(PlayerInventoryType.Hotbar, heldItemIndex, itemCount);
    }

    public void ThrowItem(PlayerInventoryType inventoryType, int index, int itemCount)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
        {
            backpack.ThrowItemServerRpc(index, itemCount, transform.position);
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            if (index == heldItemIndex)
            {
                if (GetHeldItemRef().GetStackSize() == 1)
                    LetGoOfHeldItemServerRpc();
                hotbar.ThrowItemServerRpc(index, itemCount, transform.position);
            }
            else
                hotbar.ThrowItemServerRpc(index, itemCount, transform.position);
        }
        else
        {
            armor.ThrowItemServerRpc(index, itemCount, transform.position);
        }
    }

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

    public void DeleteItem(PlayerInventoryType inventoryType, int index)
    {
        if (inventoryType == PlayerInventoryType.Backpack)
        {
            backpack.DeleteItemServerRpc(index);
        }
        else if (inventoryType == PlayerInventoryType.Hotbar)
        {
            if (index == heldItemIndex) LetGoOfHeldItemServerRpc();
            hotbar.DeleteItemServerRpc(index);
        }
        else
        {
            armor.DeleteItemServerRpc(index);
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