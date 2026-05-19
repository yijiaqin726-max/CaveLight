using UnityEngine;

public class EnergyAbsorbController : MonoBehaviour
{
    public PlayerEnergyStore energyStore;
    public float absorbRadius = 3f;
    public float absorbSpeed = 8f;
    public float checkInterval = 0.2f;

    private float nextCheckTime;

    void Awake()
    {
        if (energyStore == null)
        {
            energyStore = GetComponent<PlayerEnergyStore>();
        }
    }

    void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;
        TryAbsorbNearbyEnergy();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, absorbRadius);
    }

    private void TryAbsorbNearbyEnergy()
    {
        if (energyStore == null)
        {
            energyStore = GetComponent<PlayerEnergyStore>();
        }

        if (energyStore == null || energyStore.currentEnergy >= energyStore.maxEnergy)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, absorbRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            EnergyPickup pickup = hits[i].GetComponent<EnergyPickup>();
            if (pickup == null)
            {
                pickup = hits[i].GetComponentInParent<EnergyPickup>();
            }

            if (pickup == null)
            {
                continue;
            }

            pickup.BeginAbsorb(transform, energyStore, absorbSpeed);
        }
    }
}
