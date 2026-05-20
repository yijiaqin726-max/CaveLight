using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlaceholderPlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] public float moveSpeed = 6f;
    [SerializeField] public float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] public float groundCheckDistance = 0.15f;
    [SerializeField] public LayerMask groundLayer = ~0; // Everything by default
    [SerializeField] bool showGroundDebug = false;

    Rigidbody2D rb;
    Collider2D activeCollider;

    bool jumpRequested = false;
    bool isGrounded = false;
    bool lastGrounded = false;
    float horizontalInput = 0f;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        activeCollider = FindActiveBodyCollider();
    }

    void Reset()
    {
        groundCheckDistance = 0.15f;
        groundLayer = ~0;
        activeCollider = FindActiveBodyCollider();
    }

    void OnValidate()
    {
        if (groundCheckDistance < 0.01f)
        {
            groundCheckDistance = 0.15f;
        }
    }

    void Update()
    {
        // Read horizontal input
        horizontalInput = 0f;
        if (Input.GetKey(KeyCode.A)) horizontalInput = -1f;
        else if (Input.GetKey(KeyCode.D)) horizontalInput = 1f;

        // Read jump input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        RefreshActiveColliderIfNeeded();
        UpdateGroundedState();

        // Apply horizontal movement via velocity
        Vector2 vel = rb.linearVelocity;
        vel.x = horizontalInput * moveSpeed;

        // Apply jump if requested
        if (jumpRequested)
        {
            vel.y = jumpForce;
            jumpRequested = false;
        }

        rb.linearVelocity = vel;

        // Auto-flip based on movement direction
        if (horizontalInput > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);
        else if (horizontalInput < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
    }

    Collider2D FindActiveBodyCollider()
    {
        CapsuleCollider2D capsule = GetComponent<CapsuleCollider2D>();
        if (IsValidGroundCollider(capsule))
        {
            return capsule;
        }

        Collider2D[] colliders = GetComponents<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (IsValidGroundCollider(colliders[i]))
            {
                return colliders[i];
            }
        }

        return null;
    }

    bool IsValidGroundCollider(Collider2D candidate)
    {
        return candidate != null && candidate.enabled && !candidate.isTrigger;
    }

    void RefreshActiveColliderIfNeeded()
    {
        if (!IsValidGroundCollider(activeCollider))
        {
            activeCollider = FindActiveBodyCollider();
        }
    }

    void UpdateGroundedState()
    {
        bool groundedNow = false;

        if (activeCollider != null)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;
            filter.SetLayerMask(groundLayer);

            int hitCount = activeCollider.Cast(Vector2.down, filter, groundHits, groundCheckDistance);
            groundedNow = hitCount > 0;
        }

        isGrounded = groundedNow;
        if (showGroundDebug && isGrounded != lastGrounded)
        {
            Debug.Log(isGrounded ? "Grounded true" : "Grounded false");
        }

        lastGrounded = isGrounded;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGroundDebug)
        {
            return;
        }

        Collider2D debugCollider = activeCollider;
        if (!IsValidGroundCollider(debugCollider))
        {
            debugCollider = FindActiveBodyCollider();
        }

        if (debugCollider == null)
        {
            return;
        }

        Bounds bounds = debugCollider.bounds;
        Vector3 left = new Vector3(bounds.min.x, bounds.min.y, transform.position.z);
        Vector3 center = new Vector3(bounds.center.x, bounds.min.y, transform.position.z);
        Vector3 right = new Vector3(bounds.max.x, bounds.min.y, transform.position.z);
        Vector3 down = Vector3.down * groundCheckDistance;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(left, left + down);
        Gizmos.DrawLine(center, center + down);
        Gizmos.DrawLine(right, right + down);
    }
}
