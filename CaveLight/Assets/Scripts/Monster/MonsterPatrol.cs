using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class MonsterPatrol : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float patrolDistance = 3f;
    public LayerMask groundLayer = ~0;
    public float wallCheckDistance = 0.35f;
    public float ledgeCheckDistance = 1.2f;

    private Rigidbody2D rb;
    private Collider2D ownCollider;
    private SpriteRenderer spriteRenderer;
    private Vector2 startPosition;
    private int direction = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        if (ShouldTurnAround())
        {
            direction *= -1;
        }

        rb.linearVelocity = new Vector2(direction * moveSpeed, 0f);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction < 0;
        }
    }

    private bool ShouldTurnAround()
    {
        float distanceFromStart = transform.position.x - startPosition.x;
        if ((direction > 0 && distanceFromStart >= patrolDistance) || (direction < 0 && distanceFromStart <= -patrolDistance))
        {
            return true;
        }

        Vector2 origin = transform.position;
        if (HasSolidRayHit(origin, Vector2.right * direction, wallCheckDistance))
        {
            return true;
        }

        Vector2 groundCheckOrigin = origin + new Vector2(direction * 0.55f, -0.55f);
        return !HasSolidRayHit(groundCheckOrigin, Vector2.down, ledgeCheckDistance);
    }

    private bool HasSolidRayHit(Vector2 origin, Vector2 direction, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider == ownCollider || hitCollider.isTrigger)
            {
                continue;
            }

            if (hitCollider.attachedRigidbody == rb)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)startPosition : transform.position;
        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);
        Gizmos.DrawWireSphere(center + Vector3.left * patrolDistance, 0.08f);
        Gizmos.DrawWireSphere(center + Vector3.right * patrolDistance, 0.08f);
    }
}
