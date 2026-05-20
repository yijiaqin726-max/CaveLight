using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Attack")]
    public float attackDamage = 1f;
    public float attackCooldown = 0.4f;
    public float attackRange = 1.2f;
    public float attackRadius = 1.1f;
    public Vector2 attackCenterOffset = new Vector2(0.35f, -0.2f);

    private float nextAttackTime;
    private float lastFacingDirection = 1f;

    void Update()
    {
        UpdateFacingDirection();

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + attackCooldown;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(GetAttackCenter(), attackRadius);
    }

    private void UpdateFacingDirection()
    {
        if (Mathf.Abs(transform.localScale.x) > 0.01f)
        {
            lastFacingDirection = Mathf.Sign(transform.localScale.x);
            return;
        }

        float horizontalInput = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            lastFacingDirection = Mathf.Sign(horizontalInput);
        }
    }

    private void Attack()
    {
        Vector2 attackCenter = GetAttackCenter();
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, attackRadius);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable damageable = hits[i].GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = hits[i].GetComponentInParent<IDamageable>();
            }

            if (damageable == null)
            {
                continue;
            }

            damageable.TakeDamage(attackDamage);
            Debug.Log($"[PlayerAttack] Hit {hits[i].name}");
        }
    }

    private Vector2 GetAttackCenter()
    {
        float facingDirection = Mathf.Abs(lastFacingDirection) > 0.01f ? Mathf.Sign(lastFacingDirection) : 1f;
        Vector2 offset = attackCenterOffset;
        offset.x *= facingDirection;
        return (Vector2)transform.position + offset;
    }
}
