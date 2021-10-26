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

        if (index == -1)
        {
            result = InsertItemCopy(item);
            if (result != InsertResult.Success)
                return result;
        }
        else
        {
            result = SetItemCopy(item, index);
            if (result != InsertResult.Success)
                return result;
        }

        item.Despawn();
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
    public InsertResult InsertItemCopy(Item item)
    {
        InsertResult result = InsertResult.Failure;
        // check if there is a stack of the same item type and add item on top
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                InsertResult currResult = SetItemCopy(item, i);
                if (currResult == InsertResult.Partial)
                    result = InsertResult.Partial;
                if (currResult == InsertResult.Success)
                    return InsertResult.Success;
            }
        }
        // add item to the first empty slot
        for (int i = 0; i < size; i++)
            if (SetItemCopy(item, i) == InsertResult.Success)
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

    //returns true on success, false on failure
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
    
    //returns true on success, false on failure
    public InsertResult SetItemCopy(Item item, int index)
    {
        if (items[index] == null)
        {
            items[index] = item.Clone();
            return InsertResult.Success;
        }
        else if (item == items[index])
        {
            if (item.GetStackSize() + items[index].GetStackSize() > items[index].maxStackSize)
            {
                int stackToAdd = items[index].maxStackSize - items[index].GetStackSize();
                items[index].ChangeStackSize(stackToAdd);
                item.ChangeStackSize(-stackToAdd);
                return InsertResult.Partial;
            }
            items[index].ChangeStackSize(item.GetStackSize());
            return InsertResult.Success;
        }
        else
        {
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
    public int ConsumeFromTotalStack(Item item, int stackToConsume)
    {
        int consumedStack = 0;
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                int oldStackSize = GetItemRef(i).GetStackSize();
                int newStackSize = GetItemRef(i).ChangeStackSize(Mathf.Clamp(-(stackToConsume - consumedStack), -oldStackSize, 0));
                if (newStackSize == 0)
                    DeleteItem(i);
                consumedStack += oldStackSize - newStackSize;

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
        Item thrownItem = itemToBeThrown.Spawn(throwPosition.position, throwPosition.rotation, parent);
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