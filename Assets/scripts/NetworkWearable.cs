using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkWearable : NetworkItem
{
    private Wearable item;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        item = GetComponent<Wearable>();
        if (IsServer)
        {
            SyncWearableClientRpc(
                item.armorStrength,
                item.armorPiece);
        }
    }

    [ClientRpc]
    public void SyncWearableClientRpc(
        float armorStrength,
        ArmorPiece armorPiece)
    {
        item.armorStrength = armorStrength;
        item.armorPiece = armorPiece;
    }
}
