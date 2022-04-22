using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class NetworkedTransform : NetworkBehaviour
{
    public bool syncVelocity = false;
    private Rigidbody rb;
    private NetworkVariable<Vector3> position = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Quaternion> rotation = new NetworkVariable<Quaternion>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    private NetworkVariable<Vector3> velocity = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Start()
    {
        if (syncVelocity)
        {
            rb = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        if (IsOwner)
        {
            position.Value = transform.position;
            rotation.Value = transform.rotation;
            if (syncVelocity) velocity.Value = rb.velocity;
        }
        else
        {
            transform.position = position.Value;
            transform.rotation = rotation.Value;
            if (syncVelocity) rb.velocity = velocity.Value;
        }
    }
}