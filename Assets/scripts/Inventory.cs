using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum InsertResult
{
    Failure, // didnt insert any part of the stack of the item
    Success, // inserted all the stack of the item
    Partial  // inserted part of the stack of the item
}

public class Inventory : NetworkBehaviour
{
    public int size;
    private Item[] items;
    public bool SlotFilledFlag = false;
    public bool SlotEmptiedFlag = false;

    static public bool operator ==(Inventory inv1, Inventory inv2)
    {
        return ReferenceEquals(inv1, inv2);
    }

    static public bool operator !=(Inventory inv1, Inventory inv2)
    {
        return !(inv1 == inv2);
    }

    /*public Inventory DeepClone()
    {
        Inventory result = new Inventory(this.size);
        for (int i = 0; i < this.size; i++)
        {
            result.SetItemCopy(this.items[i], i, out _);
        }
        return result;
    }*/

    private void Start()
    {
        items = new Item[size];
    }

    // if index is -1 item gets inserted in the first empty inventory space, returns true on success, returns false if inventory is full
    public InsertResult PickupItem(Item item, out List<int> changedIndexes, bool despawnItem = true, int index = -1)
    {
        InsertResult result;
        Item insertedItem;

        if (index == -1)
        {
            result = InsertItemCopy(item, out insertedItem, out changedIndexes, despawnItem);
            if (result != InsertResult.Success)
                return result;
        }
        else
        {
            result = SetItemCopy(item, index, out insertedItem, despawnItem);
            changedIndexes = new List<int> { index };
            if (result != InsertResult.Success)
                return result;
        }

        item.Despawn();
        insertedItem.PickupEvent();
        return result;
    }

    //returns true on success, false on failure (e.g. inventry is full)
    public InsertResult InsertItemRef(Item item)
    {
        for (int i = 0; i < size; i++)
            if (SetItemRef(item, i) == InsertResult.Success) return InsertResult.Success;
        return InsertResult.Failure;
    }
    
    //returns true on success, false on failure (e.g. inventry is full)
    public InsertResult InsertItemCopy(Item item, out Item insertedItem, out List<int> changedIndexes, bool despawnItem = true)
    {
        insertedItem = null;
        changedIndexes = new List<int>();
        InsertResult result = InsertResult.Failure;

        // check if there is a stack of the same item type and add item on top
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                InsertResult currResult = SetItemCopy(item, i, out insertedItem, despawnItem);
                if (currResult != InsertResult.Failure)
                    changedIndexes.Add(i);
                if (currResult == InsertResult.Partial)
                    result = InsertResult.Partial;
                if (currResult == InsertResult.Success)
                    return InsertResult.Success;
            }
        }
        // add item to the first empty slot
        for (int i = 0; i < size; i++)
        {
            if (SetItemCopy(item, i, out insertedItem) == InsertResult.Success)
            {
                changedIndexes.Add(i);
                return InsertResult.Success;
            }
        }

        return result;
    }

    public ref Item GetItemRef(int index)
    {
        return ref items[index];
    }

    public Item GetItemCopy(int index)
    {
        return items[index].Clone();
    }

    public InsertResult SetItemRef(Item item, int index)
    {
        if (item == null)
        {
            return InsertResult.Failure;
        }
        else if (items[index] == null)
        {
            items[index] = item;
            SlotFilledFlag = true;
            return InsertResult.Success;
        }
        else
        {
            return InsertResult.Failure;
        }
    }
    
    public InsertResult SetItemCopy(Item item, int index, out Item insertedItem, bool despawnItem = true)
    {
        if (item == null)
        {
            insertedItem = null;
            return InsertResult.Failure;
        }
        else if (items[index] == null)
        {
            items[index] = item.Clone();
            item.ChangeStackSize(-item.GetStackSize());
            insertedItem = items[index];
            SlotFilledFlag = true;
            return InsertResult.Success;
        }
        else if (item == items[index])
        {
            if (item.GetStackSize() + items[index].GetStackSize() > items[index].maxStackSize)
            {
                int stackToAdd = items[index].maxStackSize - items[index].GetStackSize();
                items[index].ChangeStackSize(stackToAdd);
                item.ChangeStackSize(-stackToAdd);
                insertedItem = items[index];
                return InsertResult.Partial;
            }
            else
            {
                items[index].ChangeStackSize(item.GetStackSize());
                item.ChangeStackSize(-item.GetStackSize());
                insertedItem = items[index];
                return InsertResult.Success;
            }
        }
        else
        {
            insertedItem = null;
            return InsertResult.Failure;
        }
    }

    // returns true if items[index] contains an item, false if its empty
    public bool IsSlotFilled(int index)
    {
        if (items[index] == null)
            return false;
        return true;
    }

    public int GetStackSize(int index)
    {
        if (GetItemRef(index) == null)
            return 0;
        return GetItemRef(index).GetStackSize();
    }

    public int GetTotalStackSize(Item item)
    {
        int totalStack = 0;
        for (int i = 0; i < size; i++)
            if (GetItemRef(i) == item)
                totalStack += GetItemRef(i).GetStackSize();
        return totalStack;
    }

    // returns number of items consumed
    public int ConsumeFromStack(int index, int stackToConsume)
    {
        int oldStackSize = GetItemRef(index).GetStackSize();
        int newStackSize = GetItemRef(index).ChangeStackSize(Mathf.Clamp(-stackToConsume, -oldStackSize, 0));
        if (newStackSize == 0)
            DeleteItem(index);

        return oldStackSize - newStackSize;
    }

    // returns number of items consumed
    public int ConsumeFromTotalStack(Item item, int stackToConsume, out List<int> changedIndexes)
    {
        changedIndexes = new List<int>();
        int consumedStack = 0;
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                int prevConsumedStack = consumedStack;
                consumedStack += ConsumeFromStack(i, stackToConsume - consumedStack);
                if (prevConsumedStack != consumedStack)
                    changedIndexes.Add(i);
                if (consumedStack == stackToConsume)
                    break;
            }
        }
        return consumedStack;
    }

    //spawns item with (sets stack=itemCount isHeld=false) and deletes item from inventory if the new stack is 0, does nothing if there is no item with the index to throw or itemCount is bigger than the number of available items
    public void ThrowItem(int index, int itemCount, Vector3 position, Quaternion rotation = default)
    {
        if (items[index] == null || items[index].GetStackSize() < itemCount)
            return;

        Item itemToBeThrown = items[index].Clone();
        itemToBeThrown.SetStackSize(itemCount);
        itemToBeThrown.isHeld = false;
        ThrowItemServerRpc(itemToBeThrown.id, itemToBeThrown.Serialize(), position, rotation);
        if (items[index].ChangeStackSize(-1 * itemCount) == 0)
            DeleteItem(index);
    }

    [ServerRpc]
    public void ThrowItemServerRpc(int itemID, byte[] serializedItem, Vector3 position, Quaternion rotation = default)
    {
        Item.Deserialize(itemID, serializedItem).Spawn(false, position, rotation);
    }
    
    //returns true on success, false on failure (e.g. there is no item with the index to delete)
    public bool DeleteItem(int index)
    {
        if (items[index] == null) return false;
        items[index] = null;
        SlotEmptiedFlag = true;
        return true;
    }
}