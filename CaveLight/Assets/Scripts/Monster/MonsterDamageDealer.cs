using UnityEngine;

public class MonsterDamageDealer : MonoBehaviour
{
    public float damageToEnergy = 10f;
    public float damageCooldown = 1f;

    private float nextDamageTime;

    void OnValidate()
    {
        damageToEnergy = Mathf.Max(0f, damageToEnergy);
        damageCooldown = Mathf.Max(0.05f, damageCooldown);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other);
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

        PlayerInvincibility playerInvincibility = other.GetComponent<PlayerInvincibility>();
        if (playerInvincibility == null)
        {
            playerInvincibility = other.GetComponentInParent<PlayerInvincibility>();
        }

        if (playerInvincibility != null)
        {
            if (playerInvincibility.TryTakeEnergyDamage(damageToEnergy))
            {
                nextDamageTime = Time.time + damageCooldown;
                Debug.Log($"Monster attacked player. Energy damage: {damageToEnergy}");
            }

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
        Debug.Log($"Monster attacked player. Energy damage: {damageToEnergy}");
    }
}
