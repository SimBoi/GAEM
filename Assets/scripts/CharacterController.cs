using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterController : MonoBehaviour
{
    public CharacterMovement characterMovement;
    public Camera characterCamera;
    public MouseLook mouseLook;
    public Health health;
    public Hunger hunger;
    public PlayerInventory inventory;
    public GameObject eyePosition;
    public Transform gunRaySpawnPoint;

    public Slider healthSlider;
    public Gradient healthGradient;
    public Image healthFill;

    private bool wasThrowing = false;
    private bool GetThrowItemDown = false;

    private void Update()
    {
        if (health.GetHp() <= 0)
        {
            Die();
        }

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
                    ((Gun)heldItem).GetFireKey(gameObject);
                if (Input.GetAxisRaw("Fire2") == 1)
                    ((Gun)heldItem).GetADSKey(gameObject);
                if (Input.GetAxisRaw("Reload") == 1)
                    ((Gun)heldItem).GetReloadKey(gameObject);
            }

            if (Input.GetAxisRaw("Fire1") == 1 && inventory.heldItemIndex != -1)
                heldItem.PrimaryEvent(gameObject);
            else if (Input.GetAxisRaw("Fire2") == 1 && inventory.heldItemIndex != -1)
                heldItem.SecondaryEvent(gameObject);
        }

        UpdateUI();

        GetThrowItemDown = false;
    }

    public void UpdateUI()
    {
        healthSlider.maxValue = health.GetMaxHp();
        healthSlider.value = health.GetHp();
        healthFill.color = healthGradient.Evaluate(healthSlider.normalizedValue);
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}