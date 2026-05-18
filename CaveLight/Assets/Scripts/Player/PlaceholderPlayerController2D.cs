using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlaceholderPlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] public float moveSpeed = 6f;
    [SerializeField] public float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] public float groundCheckDistance = 0.08f;
    [SerializeField] public LayerMask groundLayer = ~0; // Everything by default

    Rigidbody2D rb;
    Collider2D col;

    bool jumpRequested = false;
    bool isGrounded = false;
    float horizontalInput = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
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
        // Ground check using Collider2D.Cast downwards
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = false;
        filter.SetLayerMask(groundLayer);

        RaycastHit2D[] results = new RaycastHit2D[1];
        int hitCount = col.Cast(Vector2.down, filter, results, groundCheckDistance);
        isGrounded = hitCount > 0;

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
}
