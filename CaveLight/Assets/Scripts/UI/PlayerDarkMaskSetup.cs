using UnityEngine;

public class PlayerDarkMaskSetup : MonoBehaviour
{
    [SerializeField] private Sprite maskSprite;
    [SerializeField] private string maskObjectName = "DarknessMaskSprite";
    [SerializeField] private float alpha = 0.65f;
    [SerializeField] private Vector3 localScale = new Vector3(8f, 8f, 1f);
    [SerializeField] private Vector3 localPosition = Vector3.zero;
    [SerializeField] private int sortingOrder = 200;

    private void Awake()
    {
        SpriteRenderer maskRenderer = EnsureMaskRenderer();
        if (maskRenderer == null)
        {
            Debug.LogError("[DARK MASK] Failed to attach mask object to PlayerPlaceholder");
            return;
        }

        Debug.Log($"[DARK MASK] Mask image generated/assigned = {maskRenderer.sprite != null}");
        Debug.Log($"[DARK MASK] Mask object attached to PlayerPlaceholder = {maskRenderer.transform.parent == transform}");
        Debug.Log($"[DARK MASK] alpha = {alpha}, scale = {maskRenderer.transform.localScale}, localPosition = {maskRenderer.transform.localPosition}, sortingOrder = {maskRenderer.sortingOrder}");
    }

    private SpriteRenderer EnsureMaskRenderer()
    {
        Transform maskTransform = transform.Find(maskObjectName);
        if (maskTransform == null)
        {
            GameObject maskObject = new GameObject(maskObjectName);
            maskTransform = maskObject.transform;
            maskTransform.SetParent(transform, false);
        }

        maskTransform.localPosition = localPosition;
        maskTransform.localRotation = Quaternion.identity;
        maskTransform.localScale = localScale;

        SpriteRenderer maskRenderer = maskTransform.GetComponent<SpriteRenderer>();
        if (maskRenderer == null)
        {
            maskRenderer = maskTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        maskRenderer.sprite = maskSprite;
        maskRenderer.color = new Color(0f, 0f, 0f, alpha);
        maskRenderer.sortingOrder = sortingOrder;

        SpriteRenderer parentRenderer = GetComponent<SpriteRenderer>();
        if (parentRenderer != null)
        {
            maskRenderer.sortingLayerID = parentRenderer.sortingLayerID;
        }

        return maskRenderer;
    }
}
