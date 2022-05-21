using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkHealth : NetworkBehaviour
{
    public NetworkVariable<float> hp = new NetworkVariable<float>(100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private Health health; // health script should only be enabled on server

    private void Start()
    {
        if (IsServer)
        {
            hp.Value = 100;
            health = GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (IsServer)
        {
            hp.Value = health.GetHp();
        }
    }

    public void DealDamage(float dmg) // clients should call this, server should call Health.DealDamage directly (when using SendMessage on server both methods will be called)
    {
        if (IsServer) return;
        DealDamageServerRpc(dmg);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DealDamageServerRpc(float dmg)
    {
        health.DealDamage(dmg);
    }
}