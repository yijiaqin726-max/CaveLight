using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerDarkMaskController : MonoBehaviour
{
    [SerializeField] private Transform maskTransform;
    [SerializeField] private Sprite maskSprite;
    [SerializeField] private Material maskMaterial;
    [SerializeField] private PlayerEnergyStore energyStore;
    [SerializeField] private float minScale = 5.5f;
    [SerializeField] private float maxScale = 10f;
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float maskAlpha = 0.85f;
    [SerializeField] private int sortingOrder = 500;
    [SerializeField] private string sortingLayerName = "Darkness";
    [SerializeField] private string resourcesMaskPath = "player_dark_mask_4096";

    private SpriteRenderer maskRenderer;
    private bool loggedVerify;
    private Material runtimeFallbackMaterial;

    private void Awake()
    {
        ResolveReferences();
        ApplyMaskSettings();
    }

    private void Update()
    {
        ResolveReferences();
        ApplyMaskSettings();
        UpdateMaskScale();
    }

    private void ResolveReferences()
    {
        if (energyStore == null)
        {
            energyStore = GetComponent<PlayerEnergyStore>();
        }

        minScale = Mathf.Max(4.5f, minScale);
        maxScale = Mathf.Max(minScale, maxScale);

        if (maskTransform == null)
        {
            Transform existingMask = transform.Find("DarknessMaskSprite");
            if (existingMask != null)
            {
                maskTransform = existingMask;
            }
        }

        if (maskTransform == null)
        {
            GameObject maskObject = new GameObject("DarknessMaskSprite");
            maskTransform = maskObject.transform;
            maskTransform.SetParent(transform, false);
        }

        if (maskRenderer == null && maskTransform != null)
        {
            maskRenderer = maskTransform.GetComponent<SpriteRenderer>();
            if (maskRenderer == null)
            {
                maskRenderer = maskTransform.gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (maskSprite == null)
        {
            maskSprite = Resources.Load<Sprite>(resourcesMaskPath);
            if (maskSprite == null)
            {
                Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesMaskPath);
                if (sprites != null && sprites.Length > 0)
                {
                    maskSprite = sprites[0];
                }
            }
        }
    }

    private void ApplyMaskSettings()
    {
        if (maskTransform == null || maskRenderer == null)
        {
            return;
        }

        maskTransform.localPosition = Vector3.zero;
        maskTransform.localRotation = Quaternion.identity;

        if (maskSprite != null)
        {
            maskRenderer.sprite = maskSprite;
        }

        Material compatibleMaterial = GetCompatibleSpriteMaterial();
        if (compatibleMaterial != null)
        {
            maskRenderer.sharedMaterial = compatibleMaterial;
        }

        maskTransform.gameObject.SetActive(true);
        maskRenderer.enabled = true;
        Color maskColor = Color.black;
        maskColor.a = maskAlpha;
        maskRenderer.color = maskColor;
        maskRenderer.sortingOrder = sortingOrder;
        if (SortingLayerExists(sortingLayerName))
        {
            maskRenderer.sortingLayerName = sortingLayerName;
        }
    }

    private void UpdateMaskScale()
    {
        if (maskTransform == null)
        {
            return;
        }

        float currentEnergy = energyStore != null ? energyStore.currentEnergy : -1f;
        float maxEnergy = energyStore != null ? energyStore.maxEnergy : -1f;
        float ratio = energyStore != null && maxEnergy > 0f ? Mathf.Clamp01(currentEnergy / maxEnergy) : 1f;
        float targetScale = Mathf.Lerp(minScale, maxScale, ratio);
        if (float.IsNaN(targetScale) || targetScale <= 0f)
        {
            targetScale = maxScale;
        }

        targetScale = Mathf.Clamp(targetScale, minScale, maxScale);
        Vector3 target = new Vector3(targetScale, targetScale, 1f);
        maskTransform.localScale = Vector3.Lerp(maskTransform.localScale, target, Time.deltaTime * smoothSpeed);
        maskTransform.localPosition = Vector3.zero;

        if (!loggedVerify)
        {
            Debug.Log($"[MASK VERIFY] DarknessMaskSprite exists = {maskTransform != null}");
            Debug.Log($"[MASK VERIFY] SpriteRenderer exists = {maskRenderer != null}");
            Debug.Log($"[MASK VERIFY] sprite assigned = {(maskRenderer != null && maskRenderer.sprite != null)}");
            Debug.Log($"[MASK VERIFY] renderer enabled = {(maskRenderer != null && maskRenderer.enabled)}");
            Debug.Log($"[MASK VERIFY] activeInHierarchy = {(maskTransform != null && maskTransform.gameObject.activeInHierarchy)}");
            Debug.Log($"[MASK VERIFY] color alpha = {(maskRenderer != null ? maskRenderer.color.a : -1f)}");
            Debug.Log($"[MASK VERIFY] sortingOrder = {(maskRenderer != null ? maskRenderer.sortingOrder : -1)}");
            Debug.Log($"[MASK VERIFY] localScale = {maskTransform.localScale}");
            Debug.Log($"[MASK VERIFY] localPosition = {maskTransform.localPosition}");
            Debug.Log($"[MASK VERIFY] worldPosition = {maskTransform.position}");
            Debug.Log($"[MASK VERIFY] currentEnergy = {currentEnergy}");
            Debug.Log($"[MASK VERIFY] maxEnergy = {maxEnergy}");
            Debug.Log($"[MASK VERIFY] ratio = {ratio:F2}");
            Debug.Log($"[MASK VERIFY] targetScale = {targetScale:F2}");
            Debug.Log($"[ORDER VERIFY] DarknessMask order = {sortingOrder}");
            LogPinkScreenVerify();
            loggedVerify = true;
        }
    }

    private Material GetCompatibleSpriteMaterial()
    {
        if (maskMaterial != null && IsUsableMaterial(maskMaterial))
        {
            return maskMaterial;
        }

        if (maskRenderer != null && IsUsableMaterial(maskRenderer.sharedMaterial))
        {
            return maskRenderer.sharedMaterial;
        }

        if (runtimeFallbackMaterial != null)
        {
            return runtimeFallbackMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError("[PINK VERIFY] Could not find a compatible Sprite shader for DarknessMaskSprite");
            return null;
        }

        runtimeFallbackMaterial = new Material(shader)
        {
            name = "DarknessMaskSpriteMaterial_Runtime"
        };
        return runtimeFallbackMaterial;
    }

    private static bool IsUsableMaterial(Material material)
    {
        if (material == null || material.shader == null)
        {
            return false;
        }

        string shaderName = material.shader.name;
        return !shaderName.Contains("Missing") && !shaderName.Contains("InternalErrorShader");
    }

    private static string DescribeMaterial(Renderer renderer)
    {
        if (renderer == null)
        {
            return "Renderer null";
        }

        Material material = renderer.sharedMaterial;
        if (material == null)
        {
            return "Material null";
        }

        string shaderName = material.shader != null ? material.shader.name : "Shader null";
        return $"{material.name} / {shaderName}";
    }

    private void LogPinkScreenVerify()
    {
        Debug.Log($"[PINK VERIFY] DarknessMask active = {(maskTransform != null && maskTransform.gameObject.activeInHierarchy)}");
        Debug.Log($"[PINK VERIFY] DarknessMask material = {(maskRenderer != null && maskRenderer.sharedMaterial != null ? maskRenderer.sharedMaterial.name : "null")}");
        Debug.Log($"[PINK VERIFY] DarknessMask shader = {(maskRenderer != null && maskRenderer.sharedMaterial != null && maskRenderer.sharedMaterial.shader != null ? maskRenderer.sharedMaterial.shader.name : "null")}");

        SpriteRenderer caveBackground = null;
        GameObject caveBackgroundObject = GameObject.Find("CaveBackground");
        if (caveBackgroundObject != null)
        {
            caveBackground = caveBackgroundObject.GetComponent<SpriteRenderer>();
        }

        Debug.Log($"[PINK VERIFY] CaveBackground material = {DescribeMaterial(caveBackground)}");

        TilemapRenderer[] tilemapRenderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        if (tilemapRenderers.Length == 0)
        {
            Debug.Log("[PINK VERIFY] TilemapRenderer material = none found");
        }
        else
        {
            for (int i = 0; i < tilemapRenderers.Length; i++)
            {
                Debug.Log($"[PINK VERIFY] TilemapRenderer material = {tilemapRenderers[i].name}: {DescribeMaterial(tilemapRenderers[i])}");
            }
        }

        SpriteRenderer playerVisual = null;
        Transform playerVisualTransform = transform.Find("PlayerVisual");
        if (playerVisualTransform != null)
        {
            playerVisual = playerVisualTransform.GetComponent<SpriteRenderer>();
        }

        Debug.Log($"[PINK VERIFY] PlayerVisual material = {DescribeMaterial(playerVisual)}");
    }

    private static bool SortingLayerExists(string layerName)
    {
        SortingLayer[] layers = SortingLayer.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == layerName)
            {
                return true;
            }
        }

        return false;
    }
}
