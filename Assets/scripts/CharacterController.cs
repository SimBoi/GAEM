using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    public float pickupRadius;
    public Health health;
    public Hunger hunger;
    public float interactDistance;
    public CharacterMovement characterMovement;
    public Camera characterCamera;
    public MouseLook mouseLook;
    public PlayerInventory inventory;
    public GameObject eyePosition;
    public Transform gunRaySpawnPoint;

    public Canvas canvas;
    public Slider healthSliderUI;
    public Gradient healthGradient;
    public Image healthFill;
    public Image heldItemIconUI;
    public Sprite handIcon;
    public GameObject inventoryUI;
    public Item clickedItem = null;
    public GameObject inventorySlotUIPrefab;
    public int maxInventoryRowSize;
    public int inventoryPadding = 100;
    public int slotPadding = 25;
    public int slotSideLength;
    private List<InventorySlotUI> hotbarUI = new List<InventorySlotUI>();
    private List<InventorySlotUI> backpackUI = new List<InventorySlotUI>();
    private List<InventorySlotUI> armorUI = new List<InventorySlotUI>();

    private void Update()
    {
        if (health.GetHp() <= 0)
        {
            Die();
        }

        GetPlayerInput();
        PickupItemsNearby();
        UpdateHealthUI();
        UpdateHeldItemUI();
    }

    private bool getThrowItem = false;
    private bool getThrowItemDown = false;
    private bool getInventory = false;
    private bool getInventoryDown = false;
    public void GetPlayerInput()
    {
        if (Input.GetAxisRaw("Throw Item") == 1)
        {
            if (!getThrowItem)
                getThrowItemDown = true;
            getThrowItem = true;
        }
        else
        {
            getThrowItem = false;
        }
        if (Input.GetAxisRaw("Inventory") == 1)
        {
            if (!getInventory)
                getInventoryDown = true;
            getInventory = true;
        }
        else
        {
            getInventory = false;
        }

        if (Input.GetAxisRaw("Hotbar Slot 0") == 1)
            inventory.SwitchToItem(0);
        else if (Input.GetAxisRaw("Hotbar Slot 1") == 1)
            inventory.SwitchToItem(1);
        else if (getThrowItemDown && inventory.heldItemIndex != -1)
            inventory.ThrowHeldItem(1);
        else if (inventory.heldItemIndex != -1)
        {
            Item heldItem = inventory.GetHeldItemRef();

            if (heldItem.GetType() == typeof(Gun))
            {
                if (Input.GetAxisRaw("Fire1") == 1)
                    ((Gun)heldItem).GetFireKey();
                if (Input.GetAxisRaw("Fire2") == 1)
                    ((Gun)heldItem).GetADSKey();
                if (Input.GetAxisRaw("Reload") == 1)
                    ((Gun)heldItem).GetReloadKey();
            }
            else
            {
                if (Input.GetAxisRaw("Fire1") == 1 && inventory.heldItemIndex != -1)
                    heldItem.PrimaryItemEvent(gameObject);
                if (Input.GetAxisRaw("Fire2") == 1 && inventory.heldItemIndex != -1)
                    heldItem.SecondaryItemEvent(gameObject);
            }
        }

        if (Input.GetAxisRaw("Interact1") == 1)
        {
            Transform origin = eyePosition.transform;
            RaycastHit hitInfo;
            if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, interactDistance))
            {
                hitInfo.collider.SendMessageUpwards("PrimaryInteractEvent", gameObject, SendMessageOptions.DontRequireReceiver);
            }
        }
        if (Input.GetAxisRaw("Interact2") == 1)
        {
            Transform origin = eyePosition.transform;
            RaycastHit hitInfo;
            if (Physics.Raycast(origin.position, origin.TransformDirection(Vector3.forward), out hitInfo, interactDistance))
            {
                hitInfo.collider.SendMessageUpwards("SecondaryInteractEvent", gameObject, SendMessageOptions.DontRequireReceiver);
            }
        }

        if (getInventoryDown)
        {
            ToggleInventoryUI();
        }

        getThrowItemDown = false;
        getInventoryDown = false;
    }

    public void PickupItemsNearby()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var hitCollider in hitColliders)
        {
            object[] message = new object[1]{
                null
            };
            hitCollider.SendMessageUpwards("GetItemRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Item item = (Item)message[0];
            if (item != null && item.CanBePickedUp())
            {
                List<int> hotbarIndexes;
                List<int> backpackIndexes;
                inventory.PickupItem(item, out hotbarIndexes, out backpackIndexes);
                foreach (int index in hotbarIndexes)
                    UpdateSlotUI(PlayerInventoryType.Hotbar, index);
                foreach (int index in backpackIndexes)
                    UpdateSlotUI(PlayerInventoryType.Backpack, index);
            }
        }
    }

    public void UpdateHealthUI()
    {
        healthSliderUI.maxValue = health.GetMaxHp();
        healthSliderUI.value = health.GetHp();
        healthFill.color = healthGradient.Evaluate(healthSliderUI.normalizedValue);
        
    }

    public void UpdateHeldItemUI()
    {
        if (inventory.heldItemIndex == -1)
        {
            heldItemIconUI.sprite = handIcon;
        }
        else
        {
            heldItemIconUI.sprite = inventory.GetHeldItemRef().icon;
        }
    }

    public void GenerateInventoryUI()
    {
        // clear old inventory ui
        foreach (InventorySlotUI slot in hotbarUI)
            Destroy(slot.gameObject);
        foreach (InventorySlotUI slot in backpackUI)
            Destroy(slot.gameObject);
        foreach (InventorySlotUI slot in armorUI)
            Destroy(slot.gameObject);
        hotbarUI.Clear();
        backpackUI.Clear();
        armorUI.Clear();

        // generate new ui
        inventoryUI.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        float slotPrefabSideLength = inventorySlotUIPrefab.GetComponent<RectTransform>().rect.width;
        float inventoryScale = slotSideLength / slotPrefabSideLength;
        int rowSize = Mathf.Min(maxInventoryRowSize, inventory.hotbarSize);
        int columnSize = Mathf.Max(1, 1 + (inventory.hotbarSize - 1) / rowSize);
        Vector3 firstSlotPos = new Vector3(-((rowSize - 1) * slotPrefabSideLength + (rowSize - 1) * 2 * slotPadding) / 2, ((columnSize - 1) * slotPrefabSideLength + (columnSize - 1) * 2 * slotPadding) / 2, 0);
        for (int i = 0; i < inventory.hotbarSize; i++)
        {
            InventorySlotUI slot = Instantiate(inventorySlotUIPrefab, inventoryUI.transform).GetComponent<InventorySlotUI>();
            slot.controller = this;
            slot.inventory = inventory.GetInventory(PlayerInventoryType.Hotbar);
            slot.slotIndex = i;
            slot.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(firstSlotPos.x + (i % (int)rowSize) * (slotPrefabSideLength + 2 * slotPadding), firstSlotPos.y - (i / (int)rowSize) * (slotPrefabSideLength + 2 * slotPadding), 0);
            hotbarUI.Add(slot);
            UpdateSlotUI(PlayerInventoryType.Hotbar, i);
        }
        inventoryUI.GetComponent<RectTransform>().sizeDelta = new Vector2(rowSize * slotPrefabSideLength + 2f * inventoryPadding + rowSize * 2 * slotPadding, columnSize * slotPrefabSideLength + 2f * inventoryPadding + columnSize * 2 * slotPadding);
        inventoryUI.GetComponent<Transform>().localScale = new Vector3(inventoryScale, inventoryScale, 1);
    }

    public void ToggleInventoryUI()
    {
        inventoryUI.SetActive(!inventoryUI.activeSelf);
        if (inventoryUI.activeSelf)
        {
            if (inventoryUI.transform.childCount == 0)
            {
                GenerateInventoryUI();
            }
        }
        else
        {
            if (clickedItem != null)
            {
                clickedItem.Spawn(false, transform.position);
                clickedItem = null;
            }
        }
    }

    public void UpdateSlotUI(PlayerInventoryType inventoryType, int index)
    {
        InventorySlotUI slot;
        if (inventoryType == PlayerInventoryType.Hotbar)
            slot = hotbarUI[index];
        else if (inventoryType == PlayerInventoryType.Backpack)
            slot = backpackUI[index];
        else
            slot = armorUI[index];

        if (inventory.IsSlotFilled(inventoryType, index))
            slot.itemIcon.sprite = inventory.GetItemRef(inventoryType, index).icon;
    }

    // returns the new item icon to display in the slot
    public Sprite OnClickInventorySlot(Inventory inventory, int slotIndex)
    {
        Sprite result = null;

        Item previousClickedItem = clickedItem;
        PlayerInventoryType inventoryType = this.inventory.GetInventoryType(inventory);
        if (inventoryType != PlayerInventoryType.Null)
        {
            if (this.inventory.IsSlotFilled(inventoryType, slotIndex))
            {
                clickedItem = this.inventory.GetItemCopy(inventoryType, slotIndex);
                clickedItem.isHeld = false;
            }
            else
                clickedItem = null;
            this.inventory.DeleteItem(inventoryType, slotIndex);
            if (previousClickedItem != null)
            {
                this.inventory.SetItemCopy(inventoryType, previousClickedItem, slotIndex, out _);
                result = previousClickedItem.icon;
            }
        }
        else
        {
            if (inventory.IsSlotFilled(slotIndex))
                clickedItem = inventory.GetItemCopy(slotIndex);
            else
                clickedItem = null;
            inventory.DeleteItem(slotIndex);
            if (previousClickedItem != null)
            {
                inventory.SetItemCopy(previousClickedItem, slotIndex, out _);
                result = previousClickedItem.icon;
            }
        }

        return result;
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}