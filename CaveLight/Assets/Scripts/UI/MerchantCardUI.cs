using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MerchantCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image cardBackground;
    [SerializeField] private Image titleBar;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconBackground;
    [SerializeField] private TMP_Text iconSymbolText;
    [SerializeField] private TMP_Text descText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private GameObject soldOutOverlay;
    [SerializeField] private TMP_Text soldOutText;

    private Button button;
    private Vector3 baseScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private bool soldOut;
    private bool hovering;
    private bool pressed;

    public Button Button
    {
        get
        {
            EnsureBuilt();
            return button;
        }
    }

    private void Awake()
    {
        EnsureBuilt();
    }

    private void OnEnable()
    {
        baseScale = Vector3.one;
        targetScale = baseScale;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * 14f);
    }

    public void Bind(MerchantProductData product, bool isSoldOut)
    {
        EnsureBuilt();
        soldOut = isSoldOut;
        nameText.text = product.displayName;
        descText.text = product.description;
        costText.text = $"Cost: {product.cost} Energy";
        iconBackground.color = product.iconColor;
        iconSymbolText.text = product.iconText;
        soldOutOverlay.SetActive(soldOut);
        button.interactable = !soldOut;
        ApplyVisualState();
    }

    public void SetSoldOut(bool value)
    {
        EnsureBuilt();
        soldOut = value;
        soldOutOverlay.SetActive(soldOut);
        button.interactable = !soldOut;
        ApplyVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        targetScale = soldOut ? baseScale : baseScale * 1.05f;
        ApplyVisualState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        pressed = false;
        targetScale = baseScale;
        ApplyVisualState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (soldOut)
        {
            return;
        }

        pressed = true;
        targetScale = baseScale * 0.97f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
        targetScale = hovering && !soldOut ? baseScale * 1.05f : baseScale;
    }

    private void EnsureBuilt()
    {
        RectTransform rect = EnsureRect(gameObject);
        rect.sizeDelta = new Vector2(260f, 360f);

        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        cardBackground = EnsureImage(gameObject);
        cardBackground.color = new Color(0.22f, 0.13f, 0.11f, 0.96f);
        button.targetGraphic = cardBackground;

        Outline outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.98f, 0.78f, 0.42f, 0.55f);
        outline.effectDistance = new Vector2(3f, -3f);

        titleBar = EnsureChildImage("TitleBar", transform, new Vector2(0f, 145f), new Vector2(232f, 44f), new Color(0.52f, 0.17f, 0.16f, 1f));
        nameText = EnsureChildText("NameText", titleBar.transform, Vector2.zero, new Vector2(220f, 36f), 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        iconBackground = EnsureChildImage("IconArea", transform, new Vector2(0f, 69f), new Vector2(190f, 104f), new Color(0.4f, 0.6f, 0.5f, 1f));
        iconSymbolText = EnsureChildText("IconSymbol", iconBackground.transform, Vector2.zero, new Vector2(180f, 86f), 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        descText = EnsureChildText("DescText", transform, new Vector2(0f, -42f), new Vector2(210f, 82f), 18f, FontStyles.Normal, TextAlignmentOptions.Center);
        costText = EnsureChildText("CostText", transform, new Vector2(0f, -136f), new Vector2(210f, 32f), 19f, FontStyles.Bold, TextAlignmentOptions.Center);

        RectTransform overlayRect = EnsureChildRect("SoldOutOverlay", transform, Vector2.zero, new Vector2(260f, 360f));
        soldOutOverlay = overlayRect.gameObject;
        Image overlayImage = EnsureImage(soldOutOverlay);
        overlayImage.color = new Color(0f, 0f, 0f, 0.62f);
        soldOutText = EnsureChildText("SoldOutText", overlayRect, Vector2.zero, new Vector2(230f, 70f), 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        soldOutText.text = "SOLD OUT";
        soldOutText.color = new Color(1f, 0.86f, 0.48f, 1f);
        soldOutOverlay.SetActive(false);
    }

    private void ApplyVisualState()
    {
        if (cardBackground == null)
        {
            return;
        }

        if (soldOut)
        {
            cardBackground.color = new Color(0.18f, 0.18f, 0.18f, 0.92f);
            return;
        }

        cardBackground.color = hovering || pressed
            ? new Color(0.34f, 0.18f, 0.13f, 1f)
            : new Color(0.22f, 0.13f, 0.11f, 0.96f);
    }

    private static RectTransform EnsureRect(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = target.AddComponent<RectTransform>();
        }

        return rect;
    }

    private static RectTransform EnsureChildRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null ? existing.GetComponent<RectTransform>() : null;
        if (rect == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            rect = child.GetComponent<RectTransform>();
        }

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image EnsureChildImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        RectTransform rect = EnsureChildRect(name, parent, anchoredPosition, size);
        Image image = EnsureImage(rect.gameObject);
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text EnsureChildText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        RectTransform rect = EnsureChildRect(name, parent, anchoredPosition, size);
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Image EnsureImage(GameObject target)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        return image;
    }
}
