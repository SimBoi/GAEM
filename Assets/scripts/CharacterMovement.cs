using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Stance
{
    Idle,
    Walk,
    Sprint,
    CrouchIdle,
    CrouchWalk,
    Slide,
    AirborneStand,
    AirborneCrouch,
    LedgeGrab
}

public class CharacterMovement : MonoBehaviour
{
    [Header("General")]
    public Rigidbody rb;
    public CapsuleCollider capsuleCollider;
    public float height;
    public Transform eyeTransform;
    public float eyeHeight = 0.8f;
    public float skinWidth = 0.1f;
    public float groundMaxDistance = 0.02f;
    public float slopeLimit = 36;
    public bool animate;

    [Header("Standing Parameters")]
    public float groundedAcceleration;
    public float groundedDeceleration;
    public float walkSpeed;
    public float sprintSpeedFactor;
    public float sprintAngleLimitation = 1.5f;
    public float jumpSpeed;

    [Header("Crouch & Slide Parameters")]
    public float crouchSpeedFactor;
    public float crouchHeightPercentage;
    public float crouchingDuration = 0.35f;
    public float minSlideSpeedPercentage = 0.8f;
    public float slideExitSpeedPercentage = 0.25f;
    public float slideForceBoost = 5;
    public float slideBoostDelay = 3;
    public float slideDeceleration;

    [Header("In Air Parameters")]
    public float airAcceleration;
    public float airDeceleration;
    public float airSpeed;

    [HideInInspector]
    public bool grounded;
    [HideInInspector]
    public Stance stance;

    // Update is called once per frame
    private void Update()
    {
        UpdateCrouchHeight();

        if (Input.GetButtonDown("Jump"))
            Jump();

        if (animate)
        {
            SendMessageUpwards("AnimStance", stance);
            SendMessageUpwards("AnimIsGrounded", grounded);
        }
    }

    private void FixedUpdate()
    {
        UpdateGrounded();
        UpdateStance();
        UpdateMovement();
    }

    // updates grounded variable
    private void UpdateGrounded()
    {
        RaycastHit hit;
        float radius = capsuleCollider.radius - skinWidth;
        float distance = (capsuleCollider.height / 2) + groundMaxDistance - radius;

        if (Physics.SphereCast(transform.position, radius, Vector3.down, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle < slopeLimit)
            {
                if (stance != Stance.Slide)
                {
                    // counterForce = -m*g*sin(slopeAngle)
                    Vector3 counterForce = Vector3.RotateTowards(hit.normal, Vector3.down, -Mathf.PI / 2, 0.0f) * Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * Physics.gravity.magnitude * rb.mass;
                    rb.AddForce(counterForce, ForceMode.Force);
                }
                grounded = true;
            }
            else
            {
                grounded = false;
            }
        }
        else
        {
            grounded = false;
        }
    }    

    // updated stance variable, and initiates slide if needed
    private void UpdateStance()
    {
        if (grounded)
        {
            Vector3 relativeVelocity = transform.InverseTransformDirection(new Vector3(rb.velocity.x, 0, rb.velocity.z));
            float rationalVelocity = relativeVelocity.magnitude / (walkSpeed * sprintSpeedFactor);

            if (!(stance == Stance.Slide && Input.GetAxisRaw("Crouch") == 1 && rationalVelocity > slideExitSpeedPercentage))
            {
                if (Input.GetAxisRaw("Crouch") == 1 || !CanStandUp())
                {
                    if (((Input.GetAxisRaw("Vertical") == 1 && Input.GetAxisRaw("Sprint") == 1) || (stance == Stance.AirborneCrouch || stance == Stance.AirborneStand)) && rationalVelocity >= minSlideSpeedPercentage)
                        InitiateSlide();
                    else if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
                        stance = Stance.CrouchIdle;
                    else
                        stance = Stance.CrouchWalk;
                }
                else
                {
                    if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
                        stance = Stance.Idle;
                    else if (Input.GetAxisRaw("Vertical") == 1 && Input.GetAxisRaw("Sprint") == 1)
                        stance = Stance.Sprint;
                    else
                        stance = Stance.Walk;
                }
            }
        }
        else
        {
            if (Input.GetAxisRaw("Crouch") == 1 || !CanStandUp())
                stance = Stance.AirborneCrouch;
            else
                stance = Stance.AirborneStand;
        }
    }

    // smoothly change height when crouching/standing for capsule and update eye position
    private float smoothedHeightVelocity = 0;
    private void UpdateCrouchHeight()
    {
        float targetHeight = (stance == Stance.CrouchIdle || stance == Stance.CrouchWalk || stance == Stance.AirborneCrouch || stance == Stance.Slide) ? crouchHeightPercentage * height : height;
        float smoothedHeight = Mathf.SmoothDamp(capsuleCollider.height, targetHeight, ref smoothedHeightVelocity, crouchingDuration);
        if (grounded)
        {
            // stick to the ground when changing height
            float newYPosition = transform.position.y + (smoothedHeight - capsuleCollider.height) / 2;
            transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);
        }
        capsuleCollider.height = smoothedHeight;
        eyeTransform.localPosition = new Vector3(0, eyeHeight * (smoothedHeight / height), 0);
    }    

    private bool CanStandUp()
    {
        if (!(stance == Stance.CrouchIdle || stance == Stance.CrouchWalk)) return true;

        RaycastHit hit;
        float radius = capsuleCollider.radius - skinWidth;
        float distance = height - (capsuleCollider.height / 2) - radius - 0.01f;

        return !(Physics.SphereCast(transform.position, radius, Vector3.up, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
    }

    private void UpdateMovement()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        Vector3 relativeVelocity = transform.InverseTransformDirection(rb.velocity);

        // calculate maxSpeed/acceleration/deceleration based on current stance and grounded state
        float acceleration;
        float deceleration;
        float maxSpeed;
        if (grounded)
        {
            if (stance == Stance.Slide)
            {
                acceleration = 0;
                deceleration = slideDeceleration;
                maxSpeed = 0;
            }
            else
            {
                acceleration = groundedAcceleration;
                deceleration = groundedDeceleration;
                maxSpeed = walkSpeed;
                if (stance == Stance.Sprint)
                {
                    maxSpeed *= sprintSpeedFactor;
                    input.z *= sprintAngleLimitation;
                }
                else if (stance == Stance.CrouchWalk || stance == Stance.CrouchIdle)
                {
                    maxSpeed *= crouchSpeedFactor;
                }
            }
        }
        else
        {
            acceleration = airAcceleration;
            deceleration = airDeceleration;
            maxSpeed = airSpeed;
        }

        // calculate velocity change
        float zVelocityChange;
        float xVelocityChange;
        float zTargetSpeed = maxSpeed * input.normalized.z;
        float xTargetSpeed = maxSpeed * input.normalized.x;
        if (Mathf.Abs(relativeVelocity.z) > Mathf.Abs(zTargetSpeed))
        {
            if (relativeVelocity.z > zTargetSpeed)
                zVelocityChange = Mathf.Clamp(-deceleration * Time.fixedDeltaTime, zTargetSpeed - relativeVelocity.z, 0);
            else
                zVelocityChange = Mathf.Clamp(deceleration * Time.fixedDeltaTime, 0, zTargetSpeed - relativeVelocity.z);
        }
        else
        {
            if (relativeVelocity.z < zTargetSpeed)
                zVelocityChange = Mathf.Clamp(acceleration * Time.fixedDeltaTime, 0, zTargetSpeed - relativeVelocity.z);
            else
                zVelocityChange = Mathf.Clamp(-acceleration * Time.fixedDeltaTime, zTargetSpeed - relativeVelocity.z, 0);
        }
        if (Mathf.Abs(relativeVelocity.x) > Mathf.Abs(xTargetSpeed))
        {
            if (relativeVelocity.x > xTargetSpeed)
                xVelocityChange = Mathf.Clamp(-deceleration * Time.fixedDeltaTime, xTargetSpeed - relativeVelocity.x, 0);
            else
                xVelocityChange = Mathf.Clamp(deceleration * Time.fixedDeltaTime, 0, xTargetSpeed - relativeVelocity.x);
        }
        else
        {
            if (relativeVelocity.x < xTargetSpeed)
                xVelocityChange = Mathf.Clamp(acceleration * Time.fixedDeltaTime, 0, xTargetSpeed - relativeVelocity.x);
            else
                xVelocityChange = Mathf.Clamp(-acceleration * Time.fixedDeltaTime, xTargetSpeed - relativeVelocity.x, 0);
        }

        // apply velocity change
        relativeVelocity.z += zVelocityChange;
        relativeVelocity.x += xVelocityChange;
        rb.velocity = transform.TransformDirection(relativeVelocity);
    }

    private void Jump()
    {
        if (!grounded) return;

        Vector3 v = rb.velocity;
        if (v.y < jumpSpeed)
        {
            v.y = jumpSpeed;
            rb.velocity = v;
        }

        if (animate)
        {
            SendMessageUpwards("AnimJump");
        }
    }

    private bool slideBoostInDelay = false;
    private void InitiateSlide()
    {
        stance = Stance.Slide;
        if (!slideBoostInDelay)
        {
            StartCoroutine(SlideBoostDelay());
            rb.AddForce(rb.velocity.normalized * slideForceBoost, ForceMode.Impulse);
        }
    }

    IEnumerator SlideBoostDelay()
    {
        slideBoostInDelay = true;
        yield return new WaitForSeconds(slideBoostDelay);
        slideBoostInDelay = false;
    }
}