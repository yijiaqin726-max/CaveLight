using UnityEngine;

public class MonsterHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 3f;
    public float currentHealth = 3f;
    public GameObject energyPrefab;
    public int dropCountMin = 2;
    public int dropCountMax = 4;
    public float dropRadius = 0.5f;
    public float dropImpulse = 1.6f;

    private bool dead;

    void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
        dropCountMin = Mathf.Max(0, dropCountMin);
        dropCountMax = Mathf.Max(dropCountMin, dropCountMax);
    }

    void OnValidate()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        dropCountMin = Mathf.Max(0, dropCountMin);
        dropCountMax = Mathf.Max(dropCountMin, dropCountMax);
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
        Debug.Log($"[MonsterHealth] {name} damaged. HP: {currentHealth}/{maxHealth}");

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
        RunStatsManager.Instance.AddMonsterKilled();
        SpawnEnergyDrops();

        CaveLevelGenerator levelGenerator = FindFirstObjectByType<CaveLevelGenerator>();
        if (levelGenerator != null)
        {
            levelGenerator.AddKillCount(1);
        }

        Debug.Log($"[MonsterHealth] {name} died.");
        Destroy(gameObject);
    }

    private void SpawnEnergyDrops()
    {
        if (energyPrefab == null)
        {
            Debug.LogWarning($"[MonsterHealth] {name} has no energyPrefab assigned.");
            return;
        }

        int dropCount = Random.Range(dropCountMin, dropCountMax + 1);
        for (int i = 0; i < dropCount; i++)
        {
            Vector2 offset = Random.insideUnitCircle * dropRadius;
            GameObject energy = Instantiate(energyPrefab, transform.position + new Vector3(offset.x, offset.y, 0f), Quaternion.identity);

            Rigidbody2D rb = energy.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 1f;
                rb.freezeRotation = true;
                rb.linearVelocity = new Vector2(Random.Range(-1f, 1f), Random.Range(0.35f, 1f)).normalized * dropImpulse;
            }
        }

        Debug.Log($"[MonsterHealth] {name} spawned {dropCount} energy drops.");
    }
}
