using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class EnergyPickup : MonoBehaviour
{
    public float energyAmount = 10f;
    public float collectDistance = 0.2f;

    private Transform absorbTarget;
    private PlayerEnergyStore absorbEnergyStore;
    private float absorbSpeed;
    private bool absorbing;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;

        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;
    }

    void Update()
    {
        if (!absorbing)
        {
            return;
        }

        if (absorbTarget == null || absorbEnergyStore == null)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, absorbTarget.position, absorbSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, absorbTarget.position) <= collectDistance)
        {
            absorbEnergyStore.AddEnergy(energyAmount);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (absorbing)
        {
            return;
        }

        PlayerEnergyStore energyStore = collision.GetComponent<PlayerEnergyStore>();
        if (energyStore == null)
        {
            energyStore = collision.GetComponentInParent<PlayerEnergyStore>();
        }

        if (energyStore == null)
        {
            return;
        }

        energyStore.AddEnergy(energyAmount);
        Destroy(gameObject);
    }

    public void BeginAbsorb(Transform target, PlayerEnergyStore energyStore, float speed)
    {
        if (absorbing || target == null || energyStore == null)
        {
            return;
        }

        absorbing = true;
        absorbTarget = target;
        absorbEnergyStore = energyStore;
        absorbSpeed = Mathf.Max(0f, speed);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        if (circleCollider != null)
        {
            circleCollider.isTrigger = true;
        }
    }
}
