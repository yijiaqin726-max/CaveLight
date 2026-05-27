using System;
using UnityEngine;

public class PlayerEnergyStore : MonoBehaviour
{
    public event Action EnergyDepleted;
    public event Action OnEnergyDepleted;

    [Header("Energy")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float drainPerSecond = 1f;

    private bool energyDepletedTriggered;

    void Awake()
    {
        maxEnergy = Mathf.Max(0f, maxEnergy);
        currentEnergy = maxEnergy;
        energyDepletedTriggered = currentEnergy <= 0f;
    }

    void Update()
    {
        if (drainPerSecond <= 0f || currentEnergy <= 0f)
        {
            return;
        }

        SetCurrentEnergy(currentEnergy - drainPerSecond * Time.deltaTime);
    }

    void OnValidate()
    {
        maxEnergy = Mathf.Max(0f, maxEnergy);
        drainPerSecond = Mathf.Max(0f, drainPerSecond);
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
    }

    public void AddEnergy(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetCurrentEnergy(currentEnergy + amount);
    }

    public bool ConsumeEnergy(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        bool hadEnoughEnergy = currentEnergy >= amount;
        SetCurrentEnergy(currentEnergy - amount);
        return hadEnoughEnergy;
    }

    public bool TrySpendEnergy(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (currentEnergy < amount)
        {
            return false;
        }

        SetCurrentEnergy(currentEnergy - amount);
        return true;
    }

    public void TakeEnergyDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetCurrentEnergy(currentEnergy - amount);
    }

    public float GetEnergyPercent()
    {
        if (maxEnergy <= 0f)
        {
            return 0f;
        }

        return currentEnergy / maxEnergy;
    }

    private void SetCurrentEnergy(float value)
    {
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);

        if (currentEnergy <= 0f && !energyDepletedTriggered)
        {
            energyDepletedTriggered = true;
            Debug.Log("Energy depleted. Game Over.");
            OnEnergyDepleted?.Invoke();
            EnergyDepleted?.Invoke();
        }
    }
}
