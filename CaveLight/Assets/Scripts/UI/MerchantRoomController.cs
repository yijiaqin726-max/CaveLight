using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MerchantRoomController : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private GameObject merchantRoomPanel;
    [SerializeField] private TMP_Text merchantTitleText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private CaveLevelGenerator caveLevelGenerator;
    [SerializeField] private PlayerEnergyStore playerEnergyStore;
    [SerializeField] private EnergyAbsorbController energyAbsorbController;

    [Header("Product Buttons")]
    [SerializeField] private Button productButton1;
    [SerializeField] private Button productButton2;
    [SerializeField] private Button productButton3;
    [FormerlySerializedAs("productButton1")]
    [SerializeField] private Button itemButton1;
    [FormerlySerializedAs("productButton2")]
    [SerializeField] private Button itemButton2;
    [FormerlySerializedAs("productButton3")]
    [SerializeField] private Button itemButton3;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button enterNextCaveButton;

    [Header("Legacy Text References")]
    [SerializeField] private Text itemText1;
    [SerializeField] private Text itemText2;
    [SerializeField] private Text itemText3;
    [SerializeField] private TMP_Text itemTmpText1;
    [SerializeField] private TMP_Text itemTmpText2;
    [SerializeField] private TMP_Text itemTmpText3;
    [SerializeField] private Text energyText;
    [SerializeField] private TMP_Text energyTmpText;

    public string lastMethod = "None";
    public GameObject MerchantRoomPanel => merchantRoomPanel;
    public Button LeaveButton => leaveButton != null ? leaveButton : enterNextCaveButton;

    private readonly ShopItem[] items =
    {
        new ShopItem("Bigger Absorb", 20),
        new ShopItem("Attack +1", 25),
        new ShopItem("Max Energy +20", 30)
    };

    private Button[] productButtons;
    private TMP_Text[] productTmpTexts;
    private Text[] productTexts;
    private TMP_Text[] costTexts;
    private TMP_Text[] descTexts;
    private TMP_Text[] soldTexts;
    private Image[] cardImages;
    private MerchantGoodsHover[] hoverEffects;
    private readonly bool[] purchased = new bool[3];
    private readonly string[] descriptions =
    {
        "Increase absorb radius.",
        "Increase attack power.",
        "Increase max energy."
    };

    private struct ShopItem
    {
        public readonly string displayName;
        public readonly int cost;

        public ShopItem(string displayName, int cost)
        {
            this.displayName = displayName;
            this.cost = cost;
        }
    }

    void Awake()
    {
        Debug.Log("[MERCHANT VERIFY] Controller Awake");
        ResolveReferences();
        BindButtons();
        LogAssignedReferences();
    }

    void Start()
    {
        Debug.Log("[MERCHANT VERIFY] Controller Start");
        ResolveReferences();
        BindButtons();
        RefreshShopUi();
        LogAssignedReferences();
    }

    public void SetRuntimeReferences(GameObject panel, CaveLevelGenerator generator, Button leaveButtonReference)
    {
        if (panel != null)
        {
            merchantRoomPanel = panel;
        }

        if (generator != null)
        {
            caveLevelGenerator = generator;
        }

        if (leaveButtonReference != null)
        {
            enterNextCaveButton = leaveButtonReference;
            leaveButton = leaveButtonReference;
        }

        ResolveReferences();
        ConfigureShopLayout();
        BindButtons();
    }

    public void OnEnterMerchantRoom()
    {
        ShowMerchantRoom();
    }

    public void ShowMerchantRoom()
    {
        lastMethod = nameof(ShowMerchantRoom);
        Debug.Log("[MERCHANT VERIFY] ShowMerchantRoom called");

        ResolveReferences();
        if (merchantRoomPanel == null)
        {
            Debug.LogError("[MERCHANT ERROR] merchantRoomPanel is null");
            return;
        }

        merchantRoomPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        EnsureUiCanReceiveInput();
        ConfigureShopLayout();
        BindButtons();
        RefreshShopUi();

        bool productButtonsBound = itemButton1 != null && itemButton2 != null && itemButton3 != null;
        bool leaveButtonBound = leaveButton != null || enterNextCaveButton != null;
        Debug.Log($"[MERCHANT VERIFY] Panel active after SetActive = {merchantRoomPanel.activeSelf}");
        Debug.Log($"[MERCHANT VERIFY] Product buttons bound = {productButtonsBound}");
        Debug.Log($"[MERCHANT VERIFY] Leave button bound = {leaveButtonBound}");
    }

    public void LeaveMerchantRoom()
    {
        lastMethod = nameof(LeaveMerchantRoom);
        Debug.Log("[MERCHANT BUTTON] Leave clicked");

        Time.timeScale = 1f;
        if (merchantRoomPanel != null)
        {
            merchantRoomPanel.SetActive(false);
        }

        if (caveLevelGenerator == null)
        {
            caveLevelGenerator = FindFirstObjectByType<CaveLevelGenerator>(FindObjectsInactive.Include);
        }

        if (caveLevelGenerator != null)
        {
            caveLevelGenerator.ExitMerchantRoom();
        }
        else
        {
            Debug.LogError("[MERCHANT ERROR] CaveLevelGenerator is null. Cannot leave merchant room.");
        }
    }

    public void TryBuyItem(int index)
    {
        lastMethod = nameof(TryBuyItem);

        ResolveReferences();
        if (index < 0 || index >= items.Length)
        {
            return;
        }

        if (purchased[index])
        {
            return;
        }

        Debug.Log($"[MERCHANT BUTTON] Buy {items[index].displayName}");

        if (playerEnergyStore == null)
        {
            Debug.LogWarning("[MERCHANT VERIFY] PlayerEnergyStore is null. Purchase skipped.");
            return;
        }

        ShopItem item = items[index];
        if (!playerEnergyStore.ConsumeEnergy(item.cost))
        {
            Debug.Log($"[MerchantRoomController] Not enough Energy for {item.displayName}. cost={item.cost}");
            RefreshShopUi();
            return;
        }

        ApplyItem(index);
        RunStatsManager.Instance.AddPurchasedItem(item.displayName);
        purchased[index] = true;
        RefreshShopUi();
    }

    private void ApplyItem(int index)
    {
        switch (index)
        {
            case 0:
                if (energyAbsorbController == null && playerEnergyStore != null)
                {
                    energyAbsorbController = playerEnergyStore.GetComponent<EnergyAbsorbController>();
                }

                if (energyAbsorbController != null)
                {
                    energyAbsorbController.absorbRadius += 1f;
                }
                break;
            case 1:
                if (playerEnergyStore != null)
                {
                    PlayerAttack attack = playerEnergyStore.GetComponent<PlayerAttack>();
                    if (attack != null)
                    {
                        attack.attackDamage += 1f;
                    }
                }
                break;
            case 2:
                if (playerEnergyStore != null)
                {
                    playerEnergyStore.maxEnergy += 20f;
                    playerEnergyStore.AddEnergy(20f);
                }
                break;
        }
    }

    private void ResolveReferences()
    {
        if (merchantRoomPanel == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            Transform panel = canvas != null ? FindChildRecursive(canvas.transform, "MerchantRoomPanel") : null;
            if (panel != null)
            {
                merchantRoomPanel = panel.gameObject;
            }
        }

        if (caveLevelGenerator == null)
        {
            caveLevelGenerator = FindFirstObjectByType<CaveLevelGenerator>(FindObjectsInactive.Include);
        }

        if (playerEnergyStore == null)
        {
            playerEnergyStore = FindFirstObjectByType<PlayerEnergyStore>(FindObjectsInactive.Include);
        }

        if (energyAbsorbController == null && playerEnergyStore != null)
        {
            energyAbsorbController = playerEnergyStore.GetComponent<EnergyAbsorbController>();
        }

        productButton1 ??= itemButton1;
        productButton2 ??= itemButton2;
        productButton3 ??= itemButton3;
        itemButton1 ??= productButton1;
        itemButton2 ??= productButton2;
        itemButton3 ??= productButton3;

        if (itemButton1 == null)
        {
            itemButton1 = FindButtonByName("ProductButton_1");
            if (itemButton1 == null)
            {
                itemButton1 = FindButtonByName("Bigger Absorb");
            }
            productButton1 = itemButton1;
        }

        if (itemButton2 == null)
        {
            itemButton2 = FindButtonByName("ProductButton_2");
            if (itemButton2 == null)
            {
                itemButton2 = FindButtonByName("Attack +1");
            }
            if (itemButton2 == null)
            {
                itemButton2 = FindButtonByName("Slow Burn");
            }
            productButton2 = itemButton2;
        }

        if (itemButton3 == null)
        {
            itemButton3 = FindButtonByName("ProductButton_3");
            if (itemButton3 == null)
            {
                itemButton3 = FindButtonByName("Max Energy +20");
            }
            if (itemButton3 == null)
            {
                itemButton3 = FindButtonByName("Energy Backpack");
            }
            productButton3 = itemButton3;
        }

        if (leaveButton == null)
        {
            leaveButton = enterNextCaveButton != null ? enterNextCaveButton : FindButtonByName("LeaveButton");
            if (leaveButton == null)
            {
                leaveButton = FindButtonByName("Enter Next Cave Button");
            }
        }

        if (enterNextCaveButton == null)
        {
            enterNextCaveButton = leaveButton;
        }

        productButtons = new[] { itemButton1, itemButton2, itemButton3 };
        productTmpTexts = new[]
        {
            itemTmpText1 != null ? itemTmpText1 : GetButtonTmpText(itemButton1),
            itemTmpText2 != null ? itemTmpText2 : GetButtonTmpText(itemButton2),
            itemTmpText3 != null ? itemTmpText3 : GetButtonTmpText(itemButton3)
        };
        productTexts = new[]
        {
            itemText1 != null ? itemText1 : GetButtonText(itemButton1),
            itemText2 != null ? itemText2 : GetButtonText(itemButton2),
            itemText3 != null ? itemText3 : GetButtonText(itemButton3)
        };

        if (dialogueText == null)
        {
            Transform dialogue = merchantRoomPanel != null ? FindChildRecursive(merchantRoomPanel.transform, "DialogueText") : null;
            if (dialogue != null)
            {
                dialogueText = dialogue.GetComponent<TMP_Text>();
            }
        }
    }

    private void ConfigureShopLayout()
    {
        if (merchantRoomPanel == null)
        {
            return;
        }

        RectTransform panelRect = merchantRoomPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            Stretch(panelRect);
        }

        Image panelImage = merchantRoomPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = Color.clear;
            panelImage.raycastTarget = false;
        }

        RectTransform overlay = GetOrCreateRect("MerchantDimOverlay", merchantRoomPanel.transform);
        overlay.SetAsFirstSibling();
        Stretch(overlay);
        Image overlayImage = EnsureImage(overlay.gameObject);
        overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImage.raycastTarget = true;

        RectTransform root = GetOrCreateRect("MerchantRoot", merchantRoomPanel.transform);
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, -35f);
        root.sizeDelta = new Vector2(1220f, 650f);
        root.SetAsLastSibling();

        if (merchantTitleText == null)
        {
            merchantTitleText = FindOrCreateTmpText("TitleText", root, 0, 250, 520, 64);
        }
        else
        {
            merchantTitleText.rectTransform.SetParent(root, false);
        }

        ConfigureText(merchantTitleText, "Merchant", 42, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.45f, 1f));
        SetRect(merchantTitleText.rectTransform, 0f, 250f, 520f, 64f);

        if (dialogueText == null)
        {
            dialogueText = FindOrCreateTmpText("DialogueText", root, 0, 200, 760, 44);
        }
        else
        {
            dialogueText.rectTransform.SetParent(root, false);
        }

        ConfigureText(dialogueText, "Spend your energy wisely.", 24, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.9f, 0.86f, 0.78f, 1f));
        SetRect(dialogueText.rectTransform, 0f, 200f, 760f, 44f);

        RectTransform goodsContainer = GetOrCreateRect("GoodsContainer", root);
        goodsContainer.anchorMin = new Vector2(0.5f, 0.5f);
        goodsContainer.anchorMax = new Vector2(0.5f, 0.5f);
        goodsContainer.pivot = new Vector2(0.5f, 0.5f);
        goodsContainer.anchoredPosition = new Vector2(0f, -35f);
        goodsContainer.sizeDelta = new Vector2(1020f, 360f);

        Button[] buttons = { itemButton1, itemButton2, itemButton3 };
        productTmpTexts = new TMP_Text[3];
        costTexts = new TMP_Text[3];
        descTexts = new TMP_Text[3];
        soldTexts = new TMP_Text[3];
        cardImages = new Image[3];
        hoverEffects = new MerchantGoodsHover[3];

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
            {
                continue;
            }

            ConfigureGoodsCard(i, button, goodsContainer);
        }

        Button activeLeaveButton = leaveButton != null ? leaveButton : enterNextCaveButton;
        if (activeLeaveButton != null)
        {
            ConfigureLeaveButton(activeLeaveButton, root);
        }
    }

    private void BindButtons()
    {
        if (productButtons == null)
        {
            ResolveReferences();
        }

        for (int i = 0; i < productButtons.Length; i++)
        {
            int itemIndex = i;
            Button button = productButtons[i];
            if (button == null)
            {
                continue;
            }

            button.interactable = !purchased[i];
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => TryBuyItem(itemIndex));
        }

        Button activeLeaveButton = leaveButton != null ? leaveButton : enterNextCaveButton;
        if (activeLeaveButton != null)
        {
            activeLeaveButton.interactable = true;
            activeLeaveButton.onClick.RemoveAllListeners();
            activeLeaveButton.onClick.AddListener(LeaveMerchantRoom);
            leaveButton = activeLeaveButton;
            enterNextCaveButton = activeLeaveButton;
        }
    }

    private void RefreshShopUi()
    {
        if (productButtons == null || productTmpTexts == null || productTexts == null)
        {
            ResolveReferences();
        }

        for (int i = 0; i < items.Length; i++)
        {
            string label = items[i].displayName;
            if (productTmpTexts[i] != null)
            {
                productTmpTexts[i].text = label;
            }

            if (productTexts[i] != null)
            {
                productTexts[i].text = label;
            }

            if (costTexts != null && costTexts[i] != null)
            {
                costTexts[i].text = $"Cost: {items[i].cost} Energy";
            }

            if (descTexts != null && descTexts[i] != null)
            {
                descTexts[i].text = descriptions[i];
                descTexts[i].color = purchased[i]
                    ? new Color(0.58f, 0.58f, 0.58f, 1f)
                    : new Color(0.88f, 0.84f, 0.76f, 1f);
            }

            if (soldTexts != null && soldTexts[i] != null)
            {
                soldTexts[i].text = purchased[i] ? "Sold" : "Buy";
            }

            if (productButtons != null && i < productButtons.Length && productButtons[i] != null)
            {
                productButtons[i].interactable = !purchased[i];
            }

            if (hoverEffects != null && hoverEffects[i] != null)
            {
                hoverEffects[i].SetSold(purchased[i]);
            }
        }

        string energyLabel = playerEnergyStore != null
            ? $"Energy: {Mathf.FloorToInt(playerEnergyStore.currentEnergy)} / {Mathf.FloorToInt(playerEnergyStore.maxEnergy)}"
            : "Energy: --";

        if (energyTmpText != null)
        {
            energyTmpText.text = energyLabel;
        }

        if (energyText != null)
        {
            energyText.text = energyLabel;
        }

        if (dialogueText != null)
        {
            dialogueText.text = "Spend your energy wisely.";
        }
    }

    private void EnsureUiCanReceiveInput()
    {
        Canvas canvas = merchantRoomPanel != null ? merchantRoomPanel.GetComponentInParent<Canvas>() : null;
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        CanvasGroup canvasGroup = merchantRoomPanel != null ? merchantRoomPanel.GetComponent<CanvasGroup>() : null;
        if (canvasGroup != null)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private Button FindButtonByName(string objectName)
    {
        Transform root = merchantRoomPanel != null ? merchantRoomPanel.transform : transform;
        Transform found = FindChildRecursive(root, objectName);
        return found != null ? found.GetComponent<Button>() : null;
    }

    private void LogAssignedReferences()
    {
        bool productButtonsAssigned = itemButton1 != null && itemButton2 != null && itemButton3 != null;
        bool leaveButtonAssigned = leaveButton != null || enterNextCaveButton != null;
        Debug.Log($"[MERCHANT VERIFY] merchantRoomPanel assigned = {merchantRoomPanel != null}");
        Debug.Log($"[MERCHANT VERIFY] product buttons assigned = {productButtonsAssigned}");
        Debug.Log($"[MERCHANT VERIFY] leaveButton assigned = {leaveButtonAssigned}");
    }

    private void ConfigureGoodsCard(int index, Button button, RectTransform goodsContainer)
    {
        RectTransform card = button.GetComponent<RectTransform>();
        card.SetParent(goodsContainer, false);
        card.name = $"GoodsItem{index + 1}";
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = new Vector2((index - 1) * 340f, 0f);
        card.sizeDelta = new Vector2(280f, 330f);

        Image cardImage = EnsureImage(card.gameObject);
        cardImage.color = new Color(0.13f, 0.11f, 0.08f, 0.88f);
        cardImage.raycastTarget = true;
        button.targetGraphic = cardImage;
        cardImages[index] = cardImage;

        Outline outline = card.GetComponent<Outline>();
        if (outline == null)
        {
            outline = card.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.95f, 0.74f, 0.28f, 0.42f);
        outline.effectDistance = new Vector2(2f, -2f);

        MerchantGoodsHover hover = card.GetComponent<MerchantGoodsHover>();
        if (hover == null)
        {
            hover = card.gameObject.AddComponent<MerchantGoodsHover>();
        }

        hoverEffects[index] = hover;

        RectTransform icon = GetOrCreateRect("Icon", card);
        icon.anchorMin = new Vector2(0.5f, 1f);
        icon.anchorMax = new Vector2(0.5f, 1f);
        icon.pivot = new Vector2(0.5f, 1f);
        icon.anchoredPosition = new Vector2(0f, -28f);
        icon.sizeDelta = new Vector2(74f, 74f);
        Image iconImage = EnsureImage(icon.gameObject);
        iconImage.color = GetIconColor(index);
        iconImage.raycastTarget = false;

        TMP_Text nameText = FindOrCreateTmpText("NameText", card, 0f, 55f, 240f, 52f);
        ConfigureText(nameText, items[index].displayName, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        productTmpTexts[index] = nameText;

        TMP_Text costText = FindOrCreateTmpText("CostText", card, 0f, 8f, 230f, 34f);
        ConfigureText(costText, $"Cost: {items[index].cost} Energy", 20, FontStyles.Normal, TextAlignmentOptions.Center, new Color(1f, 0.83f, 0.24f, 1f));
        costTexts[index] = costText;

        TMP_Text descText = FindOrCreateTmpText("DescText", card, 0f, -62f, 230f, 70f);
        ConfigureText(descText, descriptions[index], 18, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.88f, 0.84f, 0.76f, 1f));
        descTexts[index] = descText;

        TMP_Text buyText = FindOrCreateTmpText("BuyButton", card, 0f, -132f, 150f, 36f);
        ConfigureText(buyText, "Buy", 21, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.11f, 0.08f, 0.04f, 1f));
        Image buyBg = EnsureImage(buyText.gameObject);
        buyBg.color = new Color(1f, 0.78f, 0.26f, 0.92f);
        buyBg.raycastTarget = false;
        soldTexts[index] = buyText;

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.gameObject.SetActive(false);
        }
    }

    private void ConfigureLeaveButton(Button button, RectTransform root)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.SetParent(root, false);
        rect.name = "LeaveButton";
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-12f, 18f);
        rect.sizeDelta = new Vector2(220f, 54f);

        Image image = EnsureImage(button.gameObject);
        image.color = new Color(0.16f, 0.13f, 0.09f, 0.92f);
        button.targetGraphic = image;

        TMP_Text text = GetButtonTmpText(button);
        if (text == null)
        {
            text = FindOrCreateTmpText("Text (TMP)", rect, 0f, 0f, 200f, 42f);
        }

        ConfigureText(text, "Leave", 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        SetRect(text.rectTransform, 0f, 0f, 200f, 42f);
    }

    private static TMP_Text FindOrCreateTmpText(string objectName, Transform parent, float x, float y, float width, float height)
    {
        Transform existing = FindChildRecursive(parent, objectName);
        TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : null;
        if (text == null)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            text = child.GetComponent<TMP_Text>();
        }

        SetRect(text.rectTransform, x, y, width, height);
        return text;
    }

    private static RectTransform GetOrCreateRect(string objectName, Transform parent)
    {
        Transform existing = FindChildRecursive(parent, objectName);
        if (existing != null && existing.TryGetComponent(out RectTransform rect))
        {
            return rect;
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void SetRect(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
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

    private static void ConfigureText(TMP_Text text, string value, float fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
    }

    private static Color GetIconColor(int index)
    {
        switch (index)
        {
            case 0:
                return new Color(0.4f, 0.82f, 0.94f, 0.96f);
            case 1:
                return new Color(0.96f, 0.36f, 0.24f, 0.96f);
            default:
                return new Color(0.98f, 0.76f, 0.26f, 0.96f);
        }
    }

    private static TMP_Text GetButtonTmpText(Button button)
    {
        return button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private static Text GetButtonText(Button button)
    {
        return button != null ? button.GetComponentInChildren<Text>(true) : null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
