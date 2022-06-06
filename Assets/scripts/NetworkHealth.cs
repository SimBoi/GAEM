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
        if (!IsServer) return;
        health = GetComponent<Health>();
        hp.Value = health.spawnHp;
    }

    private void Update()
    {
        if (!IsServer) return;
        hp.Value = health.GetHp();
    }

    // clients should call this
    // server should call Health.DealDamage directly
    // note: when using SendMessage on server both methods will be called (both Health and NetworkHealth are enabled on the server)
    public void DealDamage(float dmg) 
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