using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum InsertResult
{
    Failure, // didnt insert any part of the stack of the item
    Success, // inserted all the stack of the item
    Partial  // inserted part of the stack of the item
}

public class Inventory : MonoBehaviour
{
    public int size;
    private Item[] items;

    public Inventory(int size)
    {
        this.size = size;
        items = new Item[size];
    }

    private void Start()
    {
        items = new Item[size];
    }

    // returns true on success, returns false if inventory is full
    public InsertResult PickupItem(Item item, int index = -1) // if index is -1 item gets inserted in the first empty inventory space
    {
        InsertResult result;
        Item insertedItem;

        if (index == -1)
        {
            result = InsertItemCopy(item, out insertedItem);
            if (result != InsertResult.Success)
                return result;
        }
        else
        {
            result = SetItemCopy(item, index, out insertedItem);
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
    public InsertResult InsertItemCopy(Item item, out Item insertedItem)
    {
        insertedItem = null;
        InsertResult result = InsertResult.Failure;

        // check if there is a stack of the same item type and add item on top
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                InsertResult currResult = SetItemCopy(item, i, out insertedItem);
                if (currResult == InsertResult.Partial)
                    result = InsertResult.Partial;
                if (currResult == InsertResult.Success)
                    return InsertResult.Success;
            }
        }
        // add item to the first empty slot
        for (int i = 0; i < size; i++)
            if (SetItemCopy(item, i, out insertedItem) == InsertResult.Success)
                return InsertResult.Success;

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
        if (items[index] == null)
        {
            items[index] = item;
            return InsertResult.Success;
        }
        else
        {
            return InsertResult.Failure;
        }
    }
    
    public InsertResult SetItemCopy(Item item, int index, out Item insertedItem)
    {
        if (items[index] == null)
        {
            items[index] = item.Clone();
            insertedItem = items[index];
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
            items[index].ChangeStackSize(item.GetStackSize());
            insertedItem = items[index];
            return InsertResult.Success;
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

    public int GetTotalStack(Item item)
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
    public int ConsumeFromTotalStack(Item item, int stackToConsume)
    {
        int consumedStack = 0;
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                consumedStack += ConsumeFromStack(i, stackToConsume - consumedStack);
                if (consumedStack == stackToConsume)
                    break;
            }
        }
        return consumedStack;
    }

    //returns the thrown item on success, null on failure (e.g. there is no item with the index to delete, itemCount is bigger than the number of available items)
    public Item ThrowItem(int index, int itemCount, Transform throwPosition, Transform parent = null)
    {
        if (items[index] == null || items[index].GetStackSize() < itemCount)
            return null;

        Item itemToBeThrown = items[index].Clone();
        itemToBeThrown.SetStackSize(itemCount);
        Item thrownItem = itemToBeThrown.Spawn(false, throwPosition.position, throwPosition.rotation, parent);
        if (items[index].ChangeStackSize(-1 * itemCount) == 0)
        {
            DeleteItem(index);
        }

        return thrownItem;
    }
    
    //returns true on success, false on failure (e.g. there is no item with the index to delete)
    public bool DeleteItem(int index)
    {
        if (items[index] == null) return false;
        items[index] = null;
        return true;
    }
}