using UnityEngine;

public class PlayerDarkMaskController : MonoBehaviour
{
    [SerializeField] private Transform maskTransform;
    [SerializeField] private Sprite maskSprite;
    [SerializeField] private PlayerEnergyStore energyStore;
    [SerializeField] private float minScale = 4.5f;
    [SerializeField] private float maxScale = 9f;
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float maskAlpha = 0.85f;
    [SerializeField] private int sortingOrder = 500;

    private SpriteRenderer maskRenderer;
    private bool loggedVerify;

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

        maskRenderer.color = new Color(0f, 0f, 0f, maskAlpha);
        maskRenderer.sortingOrder = sortingOrder;

        SpriteRenderer playerRenderer = GetComponent<SpriteRenderer>();
        if (playerRenderer != null)
        {
            maskRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        }
    }

    private void UpdateMaskScale()
    {
        if (maskTransform == null)
        {
            return;
        }

        float ratio = energyStore != null ? Mathf.Clamp01(energyStore.GetEnergyPercent()) : 1f;
        float targetScale = Mathf.Lerp(minScale, maxScale, ratio);
        Vector3 target = new Vector3(targetScale, targetScale, 1f);
        maskTransform.localScale = Vector3.Lerp(maskTransform.localScale, target, Time.deltaTime * smoothSpeed);
        maskTransform.localPosition = Vector3.zero;

        if (!loggedVerify)
        {
            Debug.Log($"[MASK VERIFY] energy ratio = {ratio:F2}, targetScale = {targetScale:F2}, alpha = {maskAlpha:F2}, sortingOrder = {sortingOrder}");
            Debug.Log($"[ORDER VERIFY] DarknessMask order = {sortingOrder}");
            loggedVerify = true;
        }
    }
}
