using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Inventory inventory;
    public int slotIndex;
    public CharacterController controller;
    public Image itemIcon;

    public void LateUpdate()
    {
        if (inventory.IsSlotFilled(slotIndex))
        {
            itemIcon.sprite = inventory.GetItemRef(slotIndex).icon;
            itemIcon.color = Color.white;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
        }
    }

    public void OnClick()
    {
        controller.OnClickInventorySlot(inventory, slotIndex);
    }
}