using System;
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
    public GameObject inventoryUIPrefab;
    public GameObject inventorySlotUIPrefab;
    public GameObject playerInventoryUI;
    private Item clickedItem = null;
    public Image clickedItemUI;
    public float clickedItemOpacity;
    public int maxInventoryRowSize;
    public int paddingBetweenInventories = 25;
    public int paddingAtinventoryBorders = 100;
    public int paddingBetweenSlots = 25;
    public int slotSideLength;
    public int clickedItemSideLength;
    private List<List<InventorySlotUI>> inventoriesUI = new List<List<InventorySlotUI>>();

    public void Start()
    {
        foreach (PlayerInventoryType inventoryType in Enum.GetValues(typeof(PlayerInventoryType)))
            inventoriesUI.Add(new List<InventorySlotUI>());
        float scale = clickedItemSideLength / (float)clickedItemUI.rectTransform.rect.width;
        clickedItemUI.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void Update()
    {
        if (health.GetHp() <= 0)
        {
            Die();
        }

        GetPlayerInput();
        PickupItemsNearby();
        UpdateHealthUI();
        UpdateHeldItemUI();
        UpdateClickedItemUI();
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
            TogglePlayerInventoryUI();
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
                inventory.PickupItem(item, out _, out _);
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

    public void UpdateClickedItemUI()
    {
        if (clickedItem != null)
        {
            clickedItemUI.sprite = clickedItem.icon;
            clickedItemUI.color = new Color(1, 1, 1, clickedItemOpacity);
            clickedItemUI.transform.position = Input.mousePosition;
        }
        else
        {
            clickedItemUI.sprite = null;
            clickedItemUI.color = Color.clear;
        }
    }

    public void GeneratePlayerInventoryUI()
    {
        float totalHeight = 0;
        List<Vector2> dimentions = new List<Vector2>();
        foreach (PlayerInventoryType inventoryType in Enum.GetValues(typeof(PlayerInventoryType)))
        {
            // clear old inventory ui
            if (inventoriesUI[(int)inventoryType].Count > 0)
                Destroy(inventoriesUI[(int)inventoryType][0].transform.parent.gameObject);
            inventoriesUI[(int)inventoryType].Clear();

            // generate new inventory ui
            List<InventorySlotUI> slotsUI;
            GameObject inventoryUI;
            dimentions.Add(GenerateInventoryUI(this.inventory.GetInventory(inventoryType), out slotsUI, out inventoryUI));
            totalHeight += dimentions[(int)inventoryType].y;
            if (inventoryUI != null)
            {
                inventoriesUI[(int)inventoryType] = slotsUI;
                inventoryUI.transform.parent = playerInventoryUI.transform;
            }
        }

        float doneHeight = 0;
        foreach (PlayerInventoryType inventoryType in Enum.GetValues(typeof(PlayerInventoryType)))
        {
            if (inventoriesUI[(int)inventoryType].Count > 0)
            {
                float yPos = (totalHeight / 2) - doneHeight - ((int)inventoryType * paddingBetweenInventories) - (dimentions[(int)inventoryType].y / 2);
                inventoriesUI[(int)inventoryType][0].transform.parent.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(0, yPos, 0);
                doneHeight += dimentions[(int)inventoryType].y;
            }
        }

    }

    public Vector2 GenerateInventoryUI(Inventory inventory, out List<InventorySlotUI> slotsUI, out GameObject inventoryUI)
    {
        slotsUI = new List<InventorySlotUI>();
        if (inventory.size == 0)
        {
            inventoryUI = null;
            return Vector2.zero;
        }
        inventoryUI = Instantiate(inventoryUIPrefab, canvas.transform);
        inventoryUI.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
        float slotPrefabSideLength = inventorySlotUIPrefab.GetComponent<RectTransform>().rect.width;
        float inventoryScale = slotSideLength / slotPrefabSideLength;
        int rowSize = Mathf.Min(maxInventoryRowSize, inventory.size);
        int columnSize = Mathf.Max(1, 1 + (inventory.size - 1) / rowSize);
        Vector3 firstSlotPos = new Vector3(-((rowSize - 1) * slotPrefabSideLength + (rowSize - 1) * 2 * paddingBetweenSlots) / 2, ((columnSize - 1) * slotPrefabSideLength + (columnSize - 1) * 2 * paddingBetweenSlots) / 2, 0);
        for (int i = 0; i < inventory.size; i++)
        {
            InventorySlotUI slot = Instantiate(inventorySlotUIPrefab, inventoryUI.transform).GetComponent<InventorySlotUI>();
            slot.controller = this;
            slot.inventory = inventory;
            slot.slotIndex = i;
            slot.gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector3(firstSlotPos.x + (i % (int)rowSize) * (slotPrefabSideLength + 2 * paddingBetweenSlots), firstSlotPos.y - (i / (int)rowSize) * (slotPrefabSideLength + 2 * paddingBetweenSlots), 0);
            slotsUI.Add(slot);
        }
        Vector2 size = new Vector2(rowSize * slotPrefabSideLength + 2f * paddingAtinventoryBorders + rowSize * 2 * paddingBetweenSlots, columnSize * slotPrefabSideLength + 2f * paddingAtinventoryBorders + columnSize * 2 * paddingBetweenSlots);
        inventoryUI.GetComponent<RectTransform>().sizeDelta = size;
        inventoryUI.GetComponent<Transform>().localScale = new Vector3(inventoryScale, inventoryScale, 1);
        return size * inventoryScale;
    }

    public void TogglePlayerInventoryUI()
    {
        playerInventoryUI.SetActive(!playerInventoryUI.activeSelf);
        if (playerInventoryUI.activeSelf)
        {
            if (playerInventoryUI.transform.childCount == 0)
            {
                GeneratePlayerInventoryUI();
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

    // returns the new item icon to display in the slot
    public void OnClickInventorySlot(Inventory inventory, int slotIndex)
    {
        Item previousClickedItem = clickedItem;
        PlayerInventoryType inventoryType;

        if (this.inventory.DoesInventoryExist(inventory, out inventoryType))
        {
            if (this.inventory.IsItemCompatible(inventoryType, clickedItem, slotIndex))
            {
                // copy item in slot
                if (this.inventory.IsSlotFilled(inventoryType, slotIndex))
                {
                    clickedItem = this.inventory.GetItemCopy(inventoryType, slotIndex);
                    clickedItem.isHeld = false;
                }
                else
                {
                    clickedItem = null;
                }

                // delete old item from slot
                this.inventory.DeleteItem(inventoryType, slotIndex);

                // insert new item in slot
                if (previousClickedItem != null)
                {
                    this.inventory.SetItemCopy(inventoryType, previousClickedItem, slotIndex, out _);
                }
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
            }
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}