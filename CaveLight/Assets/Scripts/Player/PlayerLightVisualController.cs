using UnityEngine;

public class PlayerLightVisualController : MonoBehaviour
{
    // Greybox fake lighting overlay: LightVisual is drawn above the Player and Tilemap
    // so the ground and character look illuminated without custom shaders.
    [Header("References")]
    public PlayerEnergyStore energyStore;
    public SpriteRenderer lightVisual;

    [Header("Visual")]
    public float maxScale = 6f;
    public int sortingOrder = 100;

    [Header("Low Energy Flicker")]
    public float lowEnergyThreshold = 0.25f;
    public float flickerAmount = 0.25f;
    public float flickerSpeed = 8f;

    private const string LightVisualName = "LightVisual";
    private static readonly Color LightVisualColor = new Color(1f, 0.92f, 0.25f, 0.35f);
    private Sprite generatedCircleSprite;
    private Material unlitMaterial;
    private bool renderSettingsLogged;

    void Awake()
    {
        FindReferences();
        EnsureLightVisual();
    }

    void Update()
    {
        if (energyStore == null)
        {
            energyStore = GetComponent<PlayerEnergyStore>();
        }

        if (lightVisual == null)
        {
            EnsureLightVisual();
        }

        if (energyStore == null || lightVisual == null)
        {
            return;
        }

        float energyPercent = Mathf.Clamp01(energyStore.GetEnergyPercent());

        if (energyPercent <= 0f)
        {
            lightVisual.enabled = false;
            return;
        }

        lightVisual.enabled = true;

        float flickerMultiplier = 1f;
        if (energyPercent < lowEnergyThreshold)
        {
            float flicker = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
            flickerMultiplier = 1f - flicker * flickerAmount;
        }

        float scale = maxScale * energyPercent * flickerMultiplier;
        Color color = LightVisualColor;
        if (energyPercent < lowEnergyThreshold)
        {
            color.a *= flickerMultiplier;
        }

        lightVisual.transform.localPosition = Vector3.zero;
        lightVisual.transform.localRotation = Quaternion.identity;
        lightVisual.transform.localScale = new Vector3(scale, scale, 1f);

        lightVisual.color = color;
        lightVisual.sortingLayerName = "Default";
        lightVisual.sortingOrder = sortingOrder;
    }

    void OnValidate()
    {
        maxScale = Mathf.Max(0f, maxScale);
        sortingOrder = 100;
        lowEnergyThreshold = Mathf.Clamp01(lowEnergyThreshold);
        flickerAmount = Mathf.Clamp01(flickerAmount);
        flickerSpeed = Mathf.Max(0f, flickerSpeed);
    }

    private void FindReferences()
    {
        if (energyStore == null)
        {
            energyStore = GetComponent<PlayerEnergyStore>();
        }

        if (lightVisual == null)
        {
            Transform existingVisual = transform.Find(LightVisualName);
            if (existingVisual != null)
            {
                lightVisual = existingVisual.GetComponent<SpriteRenderer>();
            }
        }
    }

    private void EnsureLightVisual()
    {
        if (lightVisual == null)
        {
            Transform existingVisual = transform.Find(LightVisualName);

            if (existingVisual == null)
            {
                GameObject visualObject = new GameObject(LightVisualName);
                visualObject.transform.SetParent(transform, false);
                existingVisual = visualObject.transform;
            }

            lightVisual = existingVisual.GetComponent<SpriteRenderer>();
            if (lightVisual == null)
            {
                lightVisual = existingVisual.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (lightVisual.transform.parent != transform)
        {
            lightVisual.transform.SetParent(transform, false);
        }

        lightVisual.sprite = GetCircleSprite();
        Debug.Log("LightVisual assigned circle sprite");

        ApplyUnlitMaterial();
        ApplyPlayerSortingOrder();
        lightVisual.transform.localPosition = Vector3.zero;
        lightVisual.transform.localRotation = Quaternion.identity;
        lightVisual.sortingLayerName = "Default";
        lightVisual.sortingOrder = sortingOrder;
        lightVisual.color = LightVisualColor;
        LogRenderSettings();
    }

    private void ApplyUnlitMaterial()
    {
        if (unlitMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                unlitMaterial = new Material(shader);
                unlitMaterial.name = "LightVisual_Unlit_Material";
            }
        }

        if (unlitMaterial != null)
        {
            lightVisual.sharedMaterial = unlitMaterial;
        }

    }

    private void ApplyPlayerSortingOrder()
    {
        SpriteRenderer playerSpriteRenderer = GetComponent<SpriteRenderer>();
        if (playerSpriteRenderer != null)
        {
            playerSpriteRenderer.sortingLayerName = "Default";
            playerSpriteRenderer.sortingOrder = 5;
        }
    }

    private void LogRenderSettings()
    {
        if (renderSettingsLogged)
        {
            return;
        }

        renderSettingsLogged = true;
        string shaderName = lightVisual.sharedMaterial != null && lightVisual.sharedMaterial.shader != null
            ? lightVisual.sharedMaterial.shader.name
            : "None";
        Debug.Log($"[PlayerLightVisualController] LightVisual sortingOrder={lightVisual.sortingOrder}, shader={shaderName}");
    }

    private Sprite GetCircleSprite()
    {
        if (generatedCircleSprite != null)
        {
            return generatedCircleSprite;
        }

        const int textureSize = 256;
        const float radius = textureSize * 0.48f;
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.name = "GeneratedLightVisualCircle";

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalized = Mathf.Clamp01(distance / radius);
                float alpha = normalized >= 1f ? 0f : Mathf.SmoothStep(1f, 0f, normalized) * 0.35f;
                texture.SetPixel(x, y, new Color(1f, 0.92f, 0.25f, alpha));
            }
        }

        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Debug.Log("Circle LightVisual sprite generated");

        generatedCircleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            32f);

        generatedCircleSprite.name = "GeneratedLightVisualCircleSprite";
        return generatedCircleSprite;
    }
}
