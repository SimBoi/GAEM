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
    public Slider healthSlider;
    public Gradient healthGradient;
    public Image healthFill;
    public Image heldItemIcon;
    public Sprite handIcon;

    private void Update()
    {
        if (health.GetHp() <= 0)
        {
            Die();
        }

        GetPlayerInput();
        PickupItemsNearby();
        UpdateUI();
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
            hitCollider.SendMessageUpwards("GetItemRef", message, SendMessageOptions.DontRequireReceiver);
            Item item = (Item)message[0];
            if (item != null && item.CanBePickedUp())
            {
                inventory.PickupItem(item);
            }
        }
    }

    public void UpdateUI()
    {
        healthSlider.maxValue = health.GetMaxHp();
        healthSlider.value = health.GetHp();
        healthFill.color = healthGradient.Evaluate(healthSlider.normalizedValue);
        
        if (inventory.heldItemIndex == -1)
        {
            heldItemIcon.sprite = handIcon;
        }
        else
        {
            heldItemIcon.sprite = inventory.GetHeldItemRef().icon;
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}