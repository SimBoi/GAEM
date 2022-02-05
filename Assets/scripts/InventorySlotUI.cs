using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Image itemIcon;
    public int slotIndex;
    [HideInInspector] public Inventory inventory;
    [HideInInspector] public CharacterController controller;

    public void LateUpdate()
    {
        if (inventory == null)
            return;

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