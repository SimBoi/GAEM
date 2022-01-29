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

    private bool wasThrowing = false;
    private bool GetThrowItemDown = false;
    public void GetPlayerInput()
    {
        if (Input.GetAxisRaw("Throw Item") == 1)
        {
            if (!wasThrowing)
                GetThrowItemDown = true;
            wasThrowing = true;
        }
        else
        {
            wasThrowing = false;
        }

        if (Input.GetAxisRaw("Hotbar Slot 0") == 1)
            inventory.SwitchToItem(0);
        else if (Input.GetAxisRaw("Hotbar Slot 1") == 1)
            inventory.SwitchToItem(1);
        else if (GetThrowItemDown && inventory.heldItemIndex != -1)
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

        if (Input.GetAxisRaw("Inventory") == 1)
        {
            ToggleInventoryUI();
        }

        GetThrowItemDown = false;
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
                inventory.PickupItem(item);
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

    public void ToggleInventoryUI()
    {
        inventoryUI.SetActive(!inventoryUI.activeSelf);
        if (inventoryUI.activeSelf)
        {
            for (int i = 0; i < inventory.hotbarSize; i++)
            {
                GameObject slot = Instantiate(inventorySlotUIPrefab, inventoryUI.transform);
                slot.GetComponent<InventorySlotUI>().itemIcon.sprite = inventory.GetItemRef(PlayerInventoryType.Hotbar, i).icon;
                slot.GetComponent<InventorySlotUI>().slotIndex = i;
                slot.GetComponent<InventorySlotUI>().inventory = inventory.GetInventory(PlayerInventoryType.Hotbar);
            }
        }
        else
        {
            foreach (Transform child in inventoryUI.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // returns the new item icon to display in the slot
    public Sprite OnClickInventorySlot(Inventory inventory, int slotIndex)
    {
        Sprite result = null;

        Item previousClickedItem = clickedItem;
        PlayerInventoryType inventoryType = this.inventory.GetInventoryType(inventory);
        if (inventoryType != PlayerInventoryType.Null)
        {
            clickedItem = this.inventory.GetItemCopy(inventoryType, slotIndex);
            this.inventory.DeleteItem(inventoryType, slotIndex);
            if (previousClickedItem != null)
            {
                this.inventory.SetItemCopy(inventoryType, previousClickedItem, slotIndex, out _);
                result = previousClickedItem.icon;
            }
        }
        else
        {
            clickedItem = inventory.GetItemCopy(slotIndex);
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