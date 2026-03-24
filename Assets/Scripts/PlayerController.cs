using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    public static bool EnableJump { get; set; } = false;
    
    [Header("Movement")]
    public float moveSpeed     = 7f;
    public float acceleration  = 15f;  // how fast we reach moveSpeed
    public float deceleration  = 20f;  // how fast we stop
    public float airControl    = 0.4f; // 0 = no air control, 1 = full

    [Header("Jump")]
    public float jumpForce        = 7f;
    public float jumpCutMultiplier = 0.4f; // reduces upward velocity on early release
    public float fallGravity      = 3f;   // extra gravity multiplier when falling
    public float jumpCoyoteTime   = 0.12f;
    public float jumpBufferTime   = 0.12f;

    [Header("Ground Check")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.3f;
    public Transform groundCheck;

    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;

    private Vector3 currentVelocity; // used by SmoothDamp
    private bool isGrounded;
    private bool jumpQueued;
    private bool jumpHeld;

    private float coyoteTimer;   // time since last grounded
    private float jumpBuffer;    // time since jump was pressed

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");

        jumpAction.performed += _ => jumpBuffer = jumpBufferTime;
        jumpAction.canceled  += _ => jumpHeld   = false;
        jumpAction.started   += _ => jumpHeld   = true;
        jumpAction.canceled += _ =>
        {
            jumpHeld = false;

            // One-shot cut on release
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector3(rb.linearVelocity.x,
                    rb.linearVelocity.y * jumpCutMultiplier,
                    rb.linearVelocity.z);
        };
    }

    void OnDestroy()
    {
        jumpAction.performed -= _ => jumpBuffer = jumpBufferTime;
        jumpAction.canceled  -= _ => jumpHeld   = false;
        jumpAction.started   -= _ => jumpHeld   = true;
    }

    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask);

        // Count down timers
        coyoteTimer -= Time.deltaTime;
        jumpBuffer  -= Time.deltaTime;

        if (isGrounded)
            coyoteTimer = jumpCoyoteTime;

        // Jump if buffer is active and coyote time allows
        if (jumpBuffer > 0f && coyoteTimer > 0f)
        {
            jumpQueued   = true;
            jumpBuffer   = 0f;
            coyoteTimer  = 0f;
        }
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleGravity();
        HandleJump();
    }

    void HandleMovement()
    {
        Vector2 raw   = moveAction.ReadValue<Vector2>();
        Vector3 target = new Vector3(raw.x, 0f, raw.y).normalized * moveSpeed;

        // Use different accel/decel rates, reduced in air
        float control = isGrounded ? 1f : airControl;
        float rate    = raw.magnitude > 0.01f
                        ? acceleration * control
                        : deceleration * control;

        // Preserve Y velocity — only lerp XZ
        Vector3 current  = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        Vector3 newXZ    = Vector3.MoveTowards(current, target, rate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newXZ.x, rb.linearVelocity.y, newXZ.z);
    }

    void HandleGravity()
    {
        // Extra downward force when falling for snappier arc
        if (rb.linearVelocity.y < 0f)
            rb.AddForce(Vector3.down * fallGravity, ForceMode.Acceleration);
    }

    void HandleJump()
    {
        if (!jumpQueued) return;
        
        if (!EnableJump || !isGrounded && coyoteTimer <= 0f) return;

        // Clear Y velocity before jump for consistent height
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        jumpQueued = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}