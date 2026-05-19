using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLightController : MonoBehaviour
{
    [Header("References")]
    public Light2D targetLight;
    public PlayerEnergyStore energyStore;

    [Header("Light Range")]
    public float maxOuterRadius = 6f;
    public float minOuterRadius = 1.5f;

    [Header("Light Intensity")]
    public float maxIntensity = 1f;
    public float minIntensity = 0.25f;

    [Header("Low Energy Flicker")]
    public float lowEnergyThreshold = 0.25f;
    public float flickerAmount = 0.25f;
    public float flickerSpeed = 8f;

    private bool missingLightWarningLogged;

    void Awake()
    {
        FindReferences();
    }

    void Update()
    {
        if (targetLight == null || energyStore == null)
        {
            FindReferences();

            if (targetLight == null)
            {
                LogMissingLightWarningOnce();
                return;
            }

            if (energyStore == null)
            {
                return;
            }
        }

        float energyPercent = Mathf.Clamp01(energyStore.GetEnergyPercent());
        float outerRadius = Mathf.Lerp(minOuterRadius, maxOuterRadius, energyPercent);
        float intensity = Mathf.Lerp(0f, maxIntensity, energyPercent);

        if (energyPercent > 0f)
        {
            intensity = Mathf.Max(intensity, minIntensity * energyPercent);
        }

        if (energyPercent > 0f && energyPercent < lowEnergyThreshold)
        {
            float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
            float flickerMultiplier = 1f - flicker * flickerAmount;
            outerRadius *= flickerMultiplier;
            intensity *= flickerMultiplier;
        }

        targetLight.pointLightOuterRadius = Mathf.Max(0f, outerRadius);
        targetLight.intensity = Mathf.Max(0f, intensity);
    }

    void OnValidate()
    {
        maxOuterRadius = Mathf.Max(0f, maxOuterRadius);
        minOuterRadius = Mathf.Clamp(minOuterRadius, 0f, maxOuterRadius);
        maxIntensity = Mathf.Max(0f, maxIntensity);
        minIntensity = Mathf.Clamp(minIntensity, 0f, maxIntensity);
        lowEnergyThreshold = Mathf.Clamp01(lowEnergyThreshold);
        flickerAmount = Mathf.Clamp01(flickerAmount);
        flickerSpeed = Mathf.Max(0f, flickerSpeed);
    }

    private void FindReferences()
    {
        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light2D>();
        }

        if (energyStore == null)
        {
            energyStore = GetComponent<PlayerEnergyStore>();
        }
    }

    private void LogMissingLightWarningOnce()
    {
        if (missingLightWarningLogged)
        {
            return;
        }

        missingLightWarningLogged = true;
        Debug.LogWarning("[PlayerLightController] Missing Light2D. Assign targetLight or add a Light2D to the PlayerLight child object.");
    }
}
