using System.Collections;
using System.Collections.Specialized;
using System.Security.Cryptography;
using UnityEngine;

public enum GroundedState
{
    Ground,
    Air,
    Slope
}

public enum Stance
{
    Standing,
    Walking,
    Sprinting,
    Crouching,
    Sliding
}

public class CharacterMovement : MonoBehaviour
{
    // GROUND SPEEDS
    public float speed = 4f;
    public float acceleration = 30f;

    // AIR SPEEDS
    public float airSpeedFactor = 1f;
    public float airAccelFactor = 0.4f;
    public float airDrag = 0.01f;
    public float airAccelDelay = 1.5f;
    private float airAccelDelayFactor;
    
    // JUMP
    public float jumpForce = 5f;
    public float jumpDelay = 0.1f;
    private bool jump = false;

    // CROUCH
    public float crouchSpeedFactor = 0.5f;
    public float crouchHeightFactor = 0.5f;
    public float crouchingDuration = 0.1f;

    // SPRINT
    public float sprintSpeedFactor = 1.5f;
    public float sprintAngleLimitation = 1.5f;

    // Slide
    public float minSlideSpeedPercentage = 0.8f;
    public float slideExitSpeedPercentage = 0.25f;
    public float slideForceBoost = 5f;
    public float slideBoostDelay = 3f;
    public float slideDrag = 10f;

    // PLAYER POSITION
    public float positionSmoothness = 0f;
    public Transform smoothedObject;
    public Transform eyeTransform;
    private Vector3 lastPosition;
    private Vector3 velocity = Vector3.zero;
    public Vector3 relativeVelocity;

    // OTHER
    public float height = 2f;
    public float eyeHeight = 0.8f;
    public float skinWidth = 0.1f;
    public float groundMaxDistance = 0.02f;
    public float slopeLimit = 36f;
    private Rigidbody rb;
    public GroundedState groundedState = GroundedState.Ground;
    public Stance stance = Stance.Standing;
    private CapsuleCollider collider;

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        collider = gameObject.GetComponent<CapsuleCollider>();
        lastPosition = transform.position;
    }

    private float crouchVelocity = 0;
    void Update()
    {
        // get the current stance
        stance = GetStance();

        // get jump key down
        if (GetJumpKeyDown()) jump = true;

        // smoothly change height when crouching/standing
        float targetHeight = ((stance == Stance.Crouching && groundedState != GroundedState.Air ) || stance == Stance.Sliding) ? crouchHeightFactor * height : height;
        float smoothedHeight = Mathf.SmoothDamp(collider.height, targetHeight, ref crouchVelocity, crouchingDuration);
        float newYPosition = transform.position.y + (smoothedHeight - collider.height) / 2;
        transform.position = new Vector3(transform.position.x, newYPosition, transform.position.z);
        collider.height = smoothedHeight;
        eyeTransform.localPosition = new Vector3(0, eyeHeight*(smoothedHeight/height), 0);
    }

    private bool wasSliding = false;
    void FixedUpdate()
    {
        if (jump)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jump = false;
        }

        groundedState = GetGroundedState();

        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        relativeVelocity = transform.InverseTransformDirection(rb.velocity);

        if (groundedState == GroundedState.Air || groundedState == GroundedState.Slope || stance == Stance.Sliding)
        {
            airAccelDelayFactor += (1 / airAccelDelay) * Time.fixedDeltaTime;
            if (airAccelDelayFactor > 1) airAccelDelayFactor = 1;
            float airMaxSpeed = speed * airSpeedFactor;
            float airAcceleration = acceleration * airAccelFactor * airAccelDelayFactor;

            Vector3 force = new Vector3(0,0,0);
            if (stance == Stance.Sliding && groundedState != GroundedState.Air)
                input = new Vector3(0, 0, 0);

            //calculate force
            force.z = CalcAirForce(relativeVelocity.z, input.normalized.z, airMaxSpeed, airDrag, airAcceleration);
            force.x = CalcAirForce(relativeVelocity.x, input.normalized.x, airMaxSpeed, airDrag, airAcceleration);
            force.y = 0;

            if (stance == Stance.Sliding && groundedState != GroundedState.Air)
            {
                if (wasSliding == false)
                {   
                    wasSliding = true;
                    if (CanSlideBoost())
                        rb.AddRelativeForce(relativeVelocity.normalized * slideForceBoost, ForceMode.Impulse);
                }
            }
            else
            {
                wasSliding = false;
            }

            if (groundedState == GroundedState.Air)
            {
                //substract air drag from force
                force.z -= CalcAirDrag(relativeVelocity.z, airDrag);
                force.x -= CalcAirDrag(relativeVelocity.x, airDrag);
            }
            else
            {
                //substract slide drag from force
                force -= CalcSlideDrag(relativeVelocity, slideDrag, speed * sprintSpeedFactor);
            }
            
            //apply force
            rb.AddRelativeForce(force, ForceMode.Acceleration);
        }
        else
        {
            wasSliding = false;
            airAccelDelayFactor = 0;
            //crouch/sprint
            float stanceFactor = 1;
            if (stance == Stance.Sprinting)
            {
                stanceFactor = sprintSpeedFactor;
                input.z *= sprintAngleLimitation;
            }
            else if (stance == Stance.Crouching)
            {
                stanceFactor = crouchSpeedFactor;
            }
            
            //calculate force
            Vector3 force;
            force.z = CalcGroundedForce(relativeVelocity.z, input.normalized.z, speed * stanceFactor, acceleration);
            force.x = CalcGroundedForce(relativeVelocity.x, input.normalized.x, speed * stanceFactor, acceleration);
            force.y = 0;

            //substract drag from force
            force.z -= CalcGroundedDrag(relativeVelocity.z, input.normalized.z, speed * stanceFactor, acceleration);
            force.x -= CalcGroundedDrag(relativeVelocity.x, input.normalized.x, speed * stanceFactor, acceleration);
            force.y -= CalcGroundedDrag(relativeVelocity.y, 0, speed, acceleration);

            //apply force
            rb.AddRelativeForce(force, ForceMode.Acceleration);
        }
    }

    void LateUpdate()
    {
        SmoothPosition();
    }

    GroundedState GetGroundedState()
    {
        RaycastHit hit;
        float radius = collider.radius - skinWidth;
        float distance = (collider.height/2) + groundMaxDistance - radius;

        bool groundedState = Physics.SphereCast(transform.position, radius, Vector3.down, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (groundedState)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
            if (slopeAngle < slopeLimit)
            {
                Vector3 counterForce = Vector3.RotateTowards(hit.normal, Vector3.down, -Mathf.PI/2, 0.0f) * Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * Physics.gravity.magnitude * rb.mass; // -m*g*sin(slopeAngle)
                rb.AddForce(counterForce, ForceMode.Force);
                return GroundedState.Ground;
            }
            else return GroundedState.Slope;
        }
        else return GroundedState.Air;
    }

    Stance GetStance()
    {
        Vector3 relativeVelocity = transform.InverseTransformDirection(new Vector3(rb.velocity.x, 0, rb.velocity.z));
        float rationalVelocity = relativeVelocity.magnitude / (speed*sprintSpeedFactor);

        if (stance != Stance.Sliding)
        {
            if (Input.GetAxisRaw("Vertical") == 1 && Input.GetAxisRaw("Sprint") == 1 && Input.GetAxisRaw("Crouch") == 1 && rationalVelocity >= minSlideSpeedPercentage)
                return Stance.Sliding;
            if (Input.GetAxisRaw("Crouch") == 1 || !CanStandUp())
                return Stance.Crouching;
            if (Input.GetAxisRaw("Horizontal") == 0 && Input.GetAxisRaw("Vertical") == 0)
                return Stance.Standing;
            if (Input.GetAxisRaw("Vertical") == 1 && Input.GetAxisRaw("Sprint") == 1)
                return Stance.Sprinting;
            return Stance.Walking;
        }
        else if (Input.GetAxisRaw("Crouch") == 0 || rationalVelocity <= slideExitSpeedPercentage)
        {
            stance = Stance.Walking;
            return GetStance();
        }
        else
        {
            return Stance.Sliding;
        }
    }

    bool CanStandUp()
    {
        if (stance != Stance.Crouching) return true;

        RaycastHit hit;
        float radius = collider.radius - skinWidth;
        float distance = height - (collider.height / 2) - radius - 0.01f;

        return !(Physics.SphereCast(transform.position, radius, Vector3.up, out hit, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
    }

    private bool jumpWasPressed = false;
    bool GetJumpKeyDown()
    {
        bool jumpKeyDown = false;
        if (Input.GetAxisRaw("Jump") == 1)
        {
            if (!jumpWasPressed) jumpKeyDown = true;
            jumpWasPressed = true;
        }
        else
        {
            jumpWasPressed = false;
        }

        if (jumpKeyDown && CanJump())
            return true;
        else
            return false;
    }

    private bool jumpInDelay = false;
    bool CanJump()
    {
        if (!jumpInDelay && groundedState == GroundedState.Ground && stance != Stance.Crouching)
        {
            StartCoroutine(JumpDelay());
            return true;
        }
        return false;
    }
    
    IEnumerator JumpDelay()
    {
        jumpInDelay = true;
        yield return new WaitForSeconds(jumpDelay);
        jumpInDelay = false;
    }

    private bool slideBoostInDelay = false;
    bool CanSlideBoost()
    {
        if (!slideBoostInDelay )
        {
            StartCoroutine(SlideBoostDelay());
            return true;
        }
        return false;
    }
    
    IEnumerator SlideBoostDelay()
    {
        slideBoostInDelay = true;
        yield return new WaitForSeconds(slideBoostDelay);
        slideBoostInDelay = false;
    }

    float CalcGroundedForce(float velocity, float input, float maxSpeed, float acceleration)
    {
        if (input == 0) return 0;
        return Mathf.Abs(velocity / (maxSpeed * input)) > 0.95f ? input * maxSpeed : input * acceleration;
    }

    float CalcAirForce(float velocity, float input, float maxSpeed, float airDrag, float acceleration)
    {
        if (input == 0) return 0;
        return Mathf.Abs(velocity / (maxSpeed * input)) > 0.95f ? input * CalcAirDrag(maxSpeed, airDrag) : input * acceleration;
    }

    float CalcGroundedDrag(float velocity, float input, float maxSpeed, float acceleration)
    {
        float drag;
        float rationalVelocity = input != 0 ? Mathf.Abs(velocity / (maxSpeed * input)) : Mathf.Abs(velocity / maxSpeed);
        
        if (rationalVelocity > 1.1f) drag = 10 * velocity;
        else if (rationalVelocity > 0.95f) drag = velocity;
        else if (input == 0 || (input < 0 && velocity > 0) || (input > 0 && velocity < 0))
        {
            if (rationalVelocity > 0.25f) drag = acceleration;
            else drag = 0.3f * acceleration * Mathf.Sqrt(Mathf.Abs(velocity));
        }
        else drag = 0;

        return Mathf.Abs(drag) * Mathf.Sign(velocity);
    }

    float CalcAirDrag(float velocity, float airDrag)
    {
        return Mathf.Abs(airDrag * Mathf.Pow(Mathf.Abs(velocity), 2)) * Mathf.Sign(velocity);
    }
    
    Vector3 CalcSlideDrag(Vector3 velocity, float slideDrag, float maxSpeed)
    {
        Vector3 drag;
        float rationalVelocity = Mathf.Abs(velocity.magnitude / maxSpeed);

        if (rationalVelocity > 0.8f)
            return velocity.normalized * slideDrag / 1.8f;
        else
            return velocity.normalized * slideDrag * rationalVelocity;
    }

    void SmoothPosition()
    {
        smoothedObject.position = Vector3.SmoothDamp(lastPosition, transform.position, ref velocity, positionSmoothness);
        lastPosition = smoothedObject.position;
    }
}