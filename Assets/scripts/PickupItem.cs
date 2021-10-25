using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public float pickupDelay = 1;
    private bool canBePickedUp = false;

    private void Awake()
    {
        StartCoroutine(DelayPickup(pickupDelay));
    }

    private IEnumerator DelayPickup(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        canBePickedUp = true;
    }

    private void OnTriggerStay(Collider collider)
    {
        if (canBePickedUp)
        {
            PlayerInventory inventory = collider.gameObject.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.PickupItem(gameObject.GetComponent<Item>());
            }
        }
    }
}