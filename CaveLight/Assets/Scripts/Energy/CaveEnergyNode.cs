using UnityEngine;

public class CaveEnergyNode : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 3f;
    public float currentHealth = 3f;

    [Header("Drops")]
    public GameObject energyPrefab;
    public int minDropCount = 3;
    public int maxDropCount = 5;
    public float dropRadius = 0.6f;
    public float dropImpulse = 2f;

    private bool dead;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        minDropCount = Mathf.Max(0, minDropCount);
        maxDropCount = Mathf.Max(minDropCount, maxDropCount);
        dropImpulse = Mathf.Max(0f, dropImpulse);
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        minDropCount = Mathf.Max(0, minDropCount);
        maxDropCount = Mathf.Max(minDropCount, maxDropCount);
        dropRadius = Mathf.Max(0f, dropRadius);
        dropImpulse = Mathf.Max(0f, dropImpulse);
    }

    public void TakeDamage(float damage)
    {
        if (dead || damage <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        Debug.Log($"[CaveEnergyNode] {name} health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (dead)
        {
            return;
        }

        dead = true;
        SpawnEnergyDrops();
        Destroy(gameObject);
    }

    private void SpawnEnergyDrops()
    {
        if (energyPrefab == null)
        {
            Debug.LogWarning($"[CaveEnergyNode] {name} has no energyPrefab assigned. No energy drops spawned.");
            return;
        }

        int dropCount = Random.Range(minDropCount, maxDropCount + 1);
        for (int i = 0; i < dropCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * dropRadius;
            Vector3 spawnPosition = transform.position + new Vector3(offset.x, offset.y, 0f);
            GameObject energy = Instantiate(energyPrefab, spawnPosition, Quaternion.identity);
            ConfigureEnergyDrop(energy);

            Rigidbody2D rb = energy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(0.35f, 1f)).normalized * dropImpulse;
                rb.linearVelocity = velocity;
            }
        }

        Debug.Log($"[CaveEnergyNode] {name} spawned {dropCount} energy drops.");
    }

    private void ConfigureEnergyDrop(GameObject energy)
    {
        if (energy == null)
        {
            return;
        }

        CircleCollider2D circleCollider = energy.GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = energy.AddComponent<CircleCollider2D>();
        }

        circleCollider.isTrigger = true;
        circleCollider.radius = 0.2f;

        Rigidbody2D rb = energy.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = energy.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.freezeRotation = true;

        SpriteRenderer spriteRenderer = energy.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 950;
            spriteRenderer.color = new Color(1f, 0.92f, 0.35f, 1f);
            Debug.Log("[ORDER VERIFY] EnergyPickup order = 950");
        }

        if (energy.GetComponent<EnergyPickup>() == null)
        {
            Debug.LogWarning($"[CaveEnergyNode] Spawned energy {energy.name} is missing EnergyPickup.");
        }

        energy.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        Debug.Log($"[CaveEnergyNode] Spawned energy {energy.name}, hasCircleCollider={circleCollider != null}, isTrigger={circleCollider.isTrigger}, radius={circleCollider.radius}, hasRigidbody={rb != null}");
    }
}
