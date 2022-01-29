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

    public void OnClick()
    {
        itemIcon.sprite = controller.OnClickInventorySlot(inventory, slotIndex);
    }
}