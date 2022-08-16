using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum InsertResult
{
    Failure, // didnt insert any part of the stack of the item
    Success, // inserted all the stack of the item
    Partial  // inserted part of the stack of the item
}

public enum InventorySyncMode
{
    OwnerClientOnly,
    AllClients
}

public class Inventory : NetworkBehaviour
{
    public int size;
    private Item[] items;
    public bool SlotFilledFlag = false;
    public bool SlotEmptiedFlag = false;
    public InventorySyncMode syncMode = InventorySyncMode.OwnerClientOnly;

    private ClientRpcParams _clientRpcParams;
    private bool initRpcParams = true;
    public ClientRpcParams clientRpcParams
    {
        get
        {
            if (initRpcParams)
            {
                if (syncMode == InventorySyncMode.OwnerClientOnly)
                {
                    _clientRpcParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams
                        {
                            TargetClientIds = new ulong[] { OwnerClientId }
                        }
                    };
                }
                else
                {
                    _clientRpcParams = default;
                }
                initRpcParams = false;
            }

            return _clientRpcParams;
        }
    }

    static public bool operator ==(Inventory inv1, Inventory inv2)
    {
        return ReferenceEquals(inv1, inv2);
    }

    static public bool operator !=(Inventory inv1, Inventory inv2)
    {
        return !(inv1 == inv2);
    }

    private void Start()
    {
        items = new Item[size];
    }

    // if index is -1 item gets inserted in the first empty inventory space
    // should only be called on the server
    public InsertResult PickupItem(Item item, out List<int> changedIndexes, bool despawnItem = true, int index = -1)
    {
        changedIndexes = new List<int>();

        if (!IsServer) return InsertResult.Failure;

        InsertResult result;
        Item insertedItem;

        if (index == -1)
        {
            result = InsertItemCopy(item, out insertedItem, out changedIndexes, despawnItem);
        }
        else
        {
            result = SetItemCopy(item, index, out insertedItem, despawnItem);
            changedIndexes = new List<int> { index };
        }
        if (result != InsertResult.Success) return result;

        item.Despawn();
        insertedItem.PickupEvent();
        return result;
    }

    // should only be called on the server, syncs item reference with owner client
    // note: item must be network spawned
    public InsertResult InsertItemRef(Item item)
    {
        if (!IsServer) return InsertResult.Failure;

        for (int i = 0; i < size; i++) if (SetItemRef(item, i) == InsertResult.Success) return InsertResult.Success;
        return InsertResult.Failure;
    }
    
    // should only be called on the server, syncs item with owner client
    public InsertResult InsertItemCopy(Item item, out Item insertedItem, out List<int> changedIndexes, bool despawnItem = true)
    {
        insertedItem = null;
        changedIndexes = new List<int>();

        if (!IsServer) return InsertResult.Failure;

        InsertResult result = InsertResult.Failure;

        // check if there is a stack of the same item type and add item on top
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                InsertResult currResult = SetItemCopy(item, i, out insertedItem, despawnItem);
                if (currResult != InsertResult.Failure) changedIndexes.Add(i);
                if (currResult == InsertResult.Partial) result = InsertResult.Partial;
                if (currResult == InsertResult.Success) return InsertResult.Success;
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

    // fails if slot is filled or item is null
    // should only be called on the server, syncs item reference with owner client
    // note: item must be network spawned
    public InsertResult SetItemRef(Item item, int index)
    {
        if (!IsServer || item == null || items[index] != null) return InsertResult.Failure;

        items[index] = item;
        SlotFilledFlag = true;

        SetItemRefClientRpc(index, item.GetComponent<NetworkObject>().NetworkObjectId, clientRpcParams);

        return InsertResult.Success;
    }

    [ClientRpc]
    public void SetItemRefClientRpc(int index, ulong itemNetworkID, ClientRpcParams clientRpcParams)
    {
        if (IsServer) return;
        items[index] = NetworkManager.Singleton.SpawnManager.SpawnedObjects[itemNetworkID].GetComponent<Item>();
    }

    // if slot is filled with same type item the stack will be increased
    // fails if slot is filled with a different type of item or item is null
    // should only be called on the server, syncs item with owner client (syncs with all clients if owned by the server)
    public InsertResult SetItemCopy(Item item, int index, out Item insertedItem, bool despawnItem = true)
    {
        insertedItem = null;

        if (!IsServer || item == null) return InsertResult.Failure;

        InsertResult result = InsertResult.Failure;
        if (items[index] == null)
        {
            items[index] = item.Clone();
            item.ChangeStackSize(-item.GetStackSize());
            insertedItem = items[index];
            SlotFilledFlag = true;
            result = InsertResult.Success;

            SyncSlotClientRpc(index, items[index].id, items[index].Serialize(), clientRpcParams);
        }
        else if (item == items[index])
        {
            if (item.GetStackSize() + items[index].GetStackSize() > items[index].maxStackSize)
            {
                int stackToAdd = items[index].maxStackSize - items[index].GetStackSize();
                if (stackToAdd > 0)
                {
                    items[index].ChangeStackSize(stackToAdd);
                    item.ChangeStackSize(-stackToAdd);
                    insertedItem = items[index];
                    result = InsertResult.Partial;
                }
                else
                {
                    result = InsertResult.Failure;
                }
            }
            else
            {
                items[index].ChangeStackSize(item.GetStackSize());
                item.ChangeStackSize(-item.GetStackSize());
                insertedItem = items[index];
                result = InsertResult.Success;
            }

            if (result != InsertResult.Failure) SyncStackClientRpc(index, items[index].GetStackSize(), clientRpcParams);
        }

        return result;
    }

    // should only be called on the server, syncs item with owner client
    public void DeleteItem(int index)
    {
        if (!IsServer || items[index] == null) return;

        items[index] = null;
        SlotEmptiedFlag = true;

        SyncSlotClientRpc(index, 0, null, clientRpcParams);
    }

    [ServerRpc]
    public void DeleteItemServerRpc(int index)
    {
        DeleteItem(index);
    }

    // spawns item with (sets stack=itemCount isHeld=false) and deletes item from inventory if the new stack is 0
    // does nothing if there is no item with the index to throw or itemCount is bigger than the number of available items
    // should only be called on the server, syncs item with owner client
    public void ThrowItem(int index, int itemCount, Vector3 position, Quaternion rotation = default)
    {
        if (!IsServer || items[index] == null || items[index].GetStackSize() < itemCount) return;

        Item thrownItem = items[index].Spawn(false, position, rotation);
        thrownItem.SetStackSize(itemCount);
        thrownItem.NetworkSpawn();
        ConsumeFromStack(index, itemCount);
    }

    [ServerRpc]
    public void ThrowItemServerRpc(int index, int itemCount, Vector3 position, Quaternion rotation = default)
    {
        ThrowItem(index, itemCount, position, rotation);
    }

    [ClientRpc]
    public void SyncSlotClientRpc(int index, int itemID, byte[] serializedItem, ClientRpcParams clientRpcParams)
    {
        if (IsServer) return;
        items[index] = Item.Deserialize(itemID, serializedItem);
    }

    // consumes from the stack of a specific slot
    // returns number of items consumed
    // should only be called on the server, syncs item with owner client
    public int ConsumeFromStack(int index, int stackToConsume)
    {
        if (!IsServer || stackToConsume < 0) return 0;

        int oldStackSize = GetItemRef(index).GetStackSize();
        int newStackSize = GetItemRef(index).ChangeStackSize(Mathf.Clamp(-stackToConsume, -oldStackSize, 0));
        if (newStackSize == 0) DeleteItem(index);
        else SyncStackClientRpc(index, newStackSize, clientRpcParams);

        return oldStackSize - newStackSize;
    }

    [ServerRpc]
    public void ConsumeFromStackServerRpc(int index, int stackToConsume)
    {
        ConsumeFromStack(index, stackToConsume);
    }

    // consumes from the collective stack of all slots of the same item parameter type
    // returns number of items consumed
    // should only be called on the server, syncs item with owner client
    public int ConsumeFromTotalStack(Item item, int stackToConsume, out List<int> changedIndexes)
    {
        changedIndexes = new List<int>();
        
        if (!IsServer || stackToConsume < 0) return 0;

        int consumedStack = 0;
        for (int i = 0; i < size; i++)
        {
            if (GetItemRef(i) == item)
            {
                int prevConsumedStack = consumedStack;
                consumedStack += ConsumeFromStack(i, stackToConsume - consumedStack);
                if (prevConsumedStack != consumedStack) changedIndexes.Add(i);
                if (consumedStack == stackToConsume) break;
            }
        }
        return consumedStack;
    }

    [ServerRpc]
    public void ConsumeFromTotalStackServerRpc(int itemID, byte[] serializedItem, int stackToConsume)
    {
        ConsumeFromTotalStack(Item.Deserialize(itemID, serializedItem), stackToConsume, out _);
    }

    [ClientRpc]
    public void SyncStackClientRpc(int index, int newStackSize, ClientRpcParams clientRpcParams)
    {
        if (IsServer) return;
        items[index].SetStackSize(newStackSize);
    }

    public ref Item GetItemRef(int index)
    {
        return ref items[index];
    }

    public Item GetItemCopy(int index)
    {
        return items[index].Clone();
    }

    public bool IsSlotFilled(int index)
    {
        if (items[index] == null) return false;
        return true;
    }

    public int GetStackSize(int index)
    {
        if (GetItemRef(index) == null) return 0;
        return GetItemRef(index).GetStackSize();
    }

    public int GetTotalStackSize(Item item)
    {
        int totalStack = 0;
        for (int i = 0; i < size; i++) if (GetItemRef(i) == item) totalStack += GetItemRef(i).GetStackSize();
        return totalStack;
    }

    /*[ClientRpc]
    public void SyncInventoryClientRpc(int[] itemIDs, byte[][] serializedItems, ClientRpcParams clientRpcParams)
    {
        for (int i = 0; i < size; i++) items[i] = Item.Deserialize(itemIDs[i], serializedItems[i]);
    }*/
}