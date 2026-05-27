using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MerchantGoodsHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float pressedScale = 0.98f;
    [SerializeField] private float animSpeed = 14f;
    [SerializeField] private Image cardImage;
    [SerializeField] private Outline outline;

    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private Color normalColor = new Color(0.13f, 0.11f, 0.08f, 0.88f);
    private Color hoverColor = new Color(0.22f, 0.18f, 0.11f, 0.94f);
    private Color normalOutline = new Color(0.95f, 0.74f, 0.28f, 0.42f);
    private Color hoverOutline = new Color(1f, 0.88f, 0.42f, 0.95f);
    private bool isPressed;
    private bool isHovering;

    private void Awake()
    {
        baseScale = transform.localScale;
        targetScale = baseScale;
        ResolveReferences();
        ApplyHoverState(false);
    }

    private void OnEnable()
    {
        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }

        targetScale = baseScale;
        ApplyHoverState(false);
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        targetScale = baseScale * hoverScale;
        ApplyHoverState(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (!isPressed)
        {
            targetScale = baseScale;
        }

        ApplyHoverState(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        targetScale = baseScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        targetScale = isHovering ? baseScale * hoverScale : baseScale;
    }

    public void SetSold(bool sold)
    {
        ResolveReferences();
        if (cardImage != null)
        {
            cardImage.color = sold ? new Color(0.18f, 0.18f, 0.18f, 0.82f) : normalColor;
        }

        if (outline != null)
        {
            outline.effectColor = sold ? new Color(0.55f, 0.55f, 0.55f, 0.45f) : normalOutline;
        }
    }

    private void ResolveReferences()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        if (outline == null)
        {
            outline = GetComponent<Outline>();
        }
    }

    private void ApplyHoverState(bool hover)
    {
        ResolveReferences();
        if (cardImage != null)
        {
            cardImage.color = hover ? hoverColor : normalColor;
        }

        if (outline != null)
        {
            outline.effectColor = hover ? hoverOutline : normalOutline;
        }
    }
}
