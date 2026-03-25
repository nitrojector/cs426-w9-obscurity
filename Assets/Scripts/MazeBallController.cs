using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MazeBallController : MonoBehaviour
{
    [Header("Movement")]
    public float moveAcceleration = 55f;
    public float maxSpeed = 7f;
    public float probeRadius = 0.22f;
    public float probeDistance = 0.42f;
    [Tooltip("Dot(move, -normal) above this blocks applied force (sliding along walls still allowed).")]
    public float blockIntoWallDot = 0.25f;

    [Header("Wall hit sound")]
    public float minImpactSpeed = 0.55f;
    public float wallHitCooldown = 0.1f;
    public AudioClip wallBumpClip;

    [Header("Layers")]
    public LayerMask mazeWallMask;

    [Header("Trail")]
    [Tooltip("Trail only draws while speed is above this (world units / s).")]
    public float trailMinSpeed = 0.12f;

    Rigidbody2D rb;
    TrailRenderer trailRenderer;
    InputAction moveAction;
    AudioSource wallAudio;
    float lastWallHitTime;
    Vector2Int lastCell;
    bool cellInitialized;

    public int Moves { get; private set; }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        wallAudio = gameObject.AddComponent<AudioSource>();
        wallAudio.playOnAwake = false;
        wallAudio.spatialBlend = 0f;

        if (wallBumpClip == null)
            wallBumpClip = ProceduralAudio.CreateWallBumpClip();

        moveAction = InputSystem.actions.FindAction("Move");
        trailRenderer = GetComponent<TrailRenderer>();
    }

    void Start()
    {
        // Mask is assigned after AddComponent in InvisibleMazeGame; Awake runs too early to validate.
        if (mazeWallMask.value != 0) return;

        int mazeWall = LayerMask.NameToLayer("MazeWall");
        if (mazeWall >= 0)
        {
            mazeWallMask = 1 << mazeWall;
            return;
        }

        int def = LayerMask.NameToLayer("Default");
        if (def >= 0)
        {
            mazeWallMask = 1 << def;
            Debug.LogWarning(
                $"{nameof(MazeBallController)}: No layer named \"MazeWall\". Using Default for wall probes — add a user layer MazeWall (Edit → Project Settings → Tags and Layers) and assign walls to it.");
        }
        else
            Debug.LogError($"{nameof(MazeBallController)}: Could not resolve any wall layer mask.");
    }

    void FixedUpdate()
    {
        Vector2 raw = moveAction.ReadValue<Vector2>();
        Vector2 dir = raw;
        if (dir.sqrMagnitude < 0.01f)
        {
            rb.linearVelocity *= 0.92f;
            TrackCellMoves();
            UpdateTrailEmitting();
            return;
        }

        dir.Normalize();
        Vector2 origin = rb.position;

        RaycastHit2D hit = Physics2D.CircleCast(origin, probeRadius, dir, probeDistance, mazeWallMask);
        if (hit.collider != null)
        {
            float into = Vector2.Dot(dir, -hit.normal);
            if (into > blockIntoWallDot)
            {
                TrackCellMoves();
                UpdateTrailEmitting();
                return;
            }
        }

        if (rb.linearVelocity.magnitude < maxSpeed)
            rb.AddForce(dir * moveAcceleration * rb.mass, ForceMode2D.Force);

        TrackCellMoves();
        UpdateTrailEmitting();
    }

    void UpdateTrailEmitting()
    {
        if (trailRenderer == null) return;
        float sqr = trailMinSpeed * trailMinSpeed;
        trailRenderer.emitting = rb.linearVelocity.sqrMagnitude > sqr;
    }

    void TrackCellMoves()
    {
        var cell = new Vector2Int(Mathf.RoundToInt(rb.position.x), Mathf.RoundToInt(rb.position.y));
        if (!cellInitialized)
        {
            lastCell = cell;
            cellInitialized = true;
            return;
        }

        if (cell != lastCell)
        {
            Moves++;
            lastCell = cell;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.collider.CompareTag("MazeWall"))
            return;
        if (Time.unscaledTime - lastWallHitTime < wallHitCooldown)
            return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed)
            return;

        lastWallHitTime = Time.unscaledTime;
        float vol = Mathf.Clamp01(speed / 6f);
        wallAudio.PlayOneShot(wallBumpClip, vol);
    }

    public void ResetMoves(Vector2 worldStart)
    {
        Moves = 0;
        cellInitialized = false;
        lastCell = new Vector2Int(Mathf.RoundToInt(worldStart.x), Mathf.RoundToInt(worldStart.y));
        cellInitialized = true;
    }
}
