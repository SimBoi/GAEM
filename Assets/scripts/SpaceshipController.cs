using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceshipController : MonoBehaviour
{
    public Rigidbody rb;
    public float upwardsThrust;
    public float forwardThrust;
    public float brakePower;
    public float xDrag;
    public float yDrag;
    public float zDrag;
    public float maxSpeedUpwards;

    /*public Transform yawTransform;
    public Transform pitchTransform;
    public Transform rollTransform;*/
    public float yawSensitivity;
    public float pitchSensitivity;
    public float rollSensitivity;
    public float yawLimit;
    public float pitchLimit;

    void Update()
    {
        Vector3 lookDegrees = new Vector3(
            Mathf.Clamp(-Input.GetAxis("Pitch") * pitchSensitivity, -pitchLimit * Time.deltaTime, pitchLimit * Time.deltaTime),
            Mathf.Clamp(Input.GetAxis("Yaw") * yawSensitivity, -yawLimit * Time.deltaTime, yawLimit * Time.deltaTime),
            -Input.GetAxis("Roll") * rollSensitivity * Time.deltaTime
        );

        Quaternion deltaRotation = Quaternion.Euler(lookDegrees);
        rb.MoveRotation(rb.rotation * deltaRotation);

        /*yawTransform.RotateAround(yawTransform.position, yawTransform.up, lookDegrees.x);
        pitchTransform.RotateAround(pitchTransform.position, pitchTransform.right, -lookDegrees.y);
        rollTransform.RotateAround(rollTransform.position, rollTransform.forward, -lookDegrees.z);*/
    }

    void FixedUpdate()
    {
        

        Vector3 relativeVelocity = transform.InverseTransformDirection(rb.velocity);

        // calculate thrust
        Vector3 thrust = new Vector3(0, Input.GetAxisRaw("Jump") * upwardsThrust, Mathf.Clamp(Input.GetAxisRaw("Vertical"), 0, 1) * forwardThrust);
        if (relativeVelocity.y > maxSpeedUpwards) thrust.y = 0;
        rb.AddRelativeForce(thrust);

        // calculate drag
        Vector3 airDrag = new Vector3(-relativeVelocity.x * relativeVelocity.x * xDrag, -relativeVelocity.y * relativeVelocity.y * yDrag, -relativeVelocity.z * relativeVelocity.z * zDrag);
        if (relativeVelocity.z > 0) airDrag.z += Mathf.Clamp(Input.GetAxisRaw("Vertical"), -1, 0) * brakePower * relativeVelocity.z * relativeVelocity.z;
        rb.AddRelativeForce(airDrag);
    }
}