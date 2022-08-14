using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PickupItemsNearby : NetworkBehaviour
{
    private CharacterController controller;
    public float pickupRadius;
    private object[] message = new object[1] { null };

    private bool spawnDelay = true;
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        yield return new WaitForSeconds(0.5f);
        spawnDelay = false;
    }

    private void Update()
    {
        if (spawnDelay || !IsSpawned || !IsServer) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var hitCollider in hitColliders)
        {
            message[0] = null;
            hitCollider.SendMessageUpwards("GetItemRefMsg", message, SendMessageOptions.DontRequireReceiver);
            Item item = (Item)message[0];

            if (item != null && item.CanBePickedUp()) controller.inventory.PickupItem(item, out _, out _, false);
        }
    }
}
