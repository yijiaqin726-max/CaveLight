using UnityEngine;

public class MonsterDamageDealer : MonoBehaviour
{
    public float damageToEnergy = 10f;
    public float damageCooldown = 1f;

    private float nextDamageTime;

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.collider);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider2D other)
    {
        if (Time.time < nextDamageTime || other == null)
        {
            return;
        }

        PlayerEnergyStore energyStore = other.GetComponent<PlayerEnergyStore>();
        if (energyStore == null)
        {
            energyStore = other.GetComponentInParent<PlayerEnergyStore>();
        }

        if (energyStore == null)
        {
            return;
        }

        energyStore.TakeEnergyDamage(damageToEnergy);
        nextDamageTime = Time.time + damageCooldown;
        Debug.Log($"[MonsterDamageDealer] {name} hit player for {damageToEnergy} energy.");
    }
}
