using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MerchantRoomController : MonoBehaviour
{
    [Header("Player")]
    public PlayerEnergyStore playerEnergyStore;
    public EnergyAbsorbController energyAbsorbController;

    [Header("Item Buttons")]
    public Button itemButton1;
    public Button itemButton2;
    public Button itemButton3;

    [Header("Item Text")]
    public Text itemText1;
    public Text itemText2;
    public Text itemText3;
    public TMP_Text itemTmpText1;
    public TMP_Text itemTmpText2;
    public TMP_Text itemTmpText3;

    [Header("Energy Display")]
    public Text energyText;
    public TMP_Text energyTmpText;

    [Header("Navigation")]
    public Button enterNextCaveButton;

    private bool backpackPurchased;
    private bool slowBurnPurchased;
    private bool biggerAbsorbPurchased;
    private Sprite generatedCircleSprite;

    private const float BackpackCost = 30f;
    private const float SlowBurnCost = 25f;
    private const float BiggerAbsorbCost = 20f;

    void Awake()
    {
        FindReferences();
        ConfigureMerchantRoomLayout();
        BindButtons();
    }

    void OnEnable()
    {
        FindReferences();
        ConfigureMerchantRoomLayout();
        RefreshButtons();
    }

    public void OnEnterMerchantRoom()
    {
        backpackPurchased = false;
        slowBurnPurchased = false;
        biggerAbsorbPurchased = false;
        FindReferences();
        ConfigureMerchantRoomLayout();
        BindButtons();
        RefreshButtons();
    }

    private void FindReferences()
    {
        if (playerEnergyStore == null)
        {
            playerEnergyStore = FindFirstObjectByType<PlayerEnergyStore>();
        }

        if (energyAbsorbController == null)
        {
            energyAbsorbController = FindFirstObjectByType<EnergyAbsorbController>();
        }

        AutoFindUiReferences();
    }

    private void BindButtons()
    {
        if (itemButton1 != null)
        {
            itemButton1.onClick.RemoveListener(BuyEnergyBackpack);
            itemButton1.onClick.AddListener(BuyEnergyBackpack);
        }

        if (itemButton2 != null)
        {
            itemButton2.onClick.RemoveListener(BuySlowBurn);
            itemButton2.onClick.AddListener(BuySlowBurn);
        }

        if (itemButton3 != null)
        {
            itemButton3.onClick.RemoveListener(BuyBiggerAbsorb);
            itemButton3.onClick.AddListener(BuyBiggerAbsorb);
        }

        BindEnterNextCaveButton();
    }

    private void BuyEnergyBackpack()
    {
        if (backpackPurchased)
        {
            return;
        }

        if (!TrySpendEnergy(BackpackCost, "Energy Backpack", itemText1, itemTmpText1))
        {
            return;
        }

        playerEnergyStore.maxEnergy += 20f;
        playerEnergyStore.AddEnergy(20f);
        backpackPurchased = true;
        Debug.Log("[MerchantRoomController] Purchased Energy Backpack.");
        RefreshButtons();
    }

    private void BuySlowBurn()
    {
        if (slowBurnPurchased)
        {
            return;
        }

        if (!TrySpendEnergy(SlowBurnCost, "Slow Burn", itemText2, itemTmpText2))
        {
            return;
        }

        playerEnergyStore.drainPerSecond *= 0.8f;
        slowBurnPurchased = true;
        Debug.Log("[MerchantRoomController] Purchased Slow Burn.");
        RefreshButtons();
    }

    private void BuyBiggerAbsorb()
    {
        if (biggerAbsorbPurchased)
        {
            return;
        }

        if (!TrySpendEnergy(BiggerAbsorbCost, "Bigger Absorb", itemText3, itemTmpText3))
        {
            return;
        }

        if (energyAbsorbController != null)
        {
            energyAbsorbController.absorbRadius += 1f;
        }

        biggerAbsorbPurchased = true;
        Debug.Log("[MerchantRoomController] Purchased Bigger Absorb.");
        RefreshButtons();
    }

    private bool TrySpendEnergy(float cost, string itemName, Text buttonText, TMP_Text buttonTmpText)
    {
        FindReferences();

        if (playerEnergyStore == null || playerEnergyStore.currentEnergy < cost)
        {
            SetButtonLabel(buttonText, buttonTmpText, "Not Enough Energy");
            Debug.Log($"[MerchantRoomController] Not enough Energy for {itemName}.");
            return false;
        }

        playerEnergyStore.ConsumeEnergy(cost);
        return true;
    }

    private void RefreshButtons()
    {
        SetItemState(itemButton1, itemText1, itemTmpText1, backpackPurchased);
        SetItemState(itemButton2, itemText2, itemTmpText2, slowBurnPurchased);
        SetItemState(itemButton3, itemText3, itemTmpText3, biggerAbsorbPurchased);
        RefreshEnergyText();
    }

    private void SetItemState(Button button, Text text, TMP_Text tmpText, bool purchased)
    {
        if (button != null)
        {
            button.interactable = !purchased;
        }

        SetButtonLabel(text, tmpText, purchased ? "Purchased" : "Buy");
    }

    private void SetButtonLabel(Text text, TMP_Text tmpText, string value)
    {
        if (text != null)
        {
            text.text = value;
        }

        if (tmpText != null)
        {
            tmpText.text = value;
        }
    }

    private void RefreshEnergyText()
    {
        string value = playerEnergyStore != null
            ? $"Energy: {playerEnergyStore.currentEnergy:0} / {playerEnergyStore.maxEnergy:0}"
            : "Energy: -- / --";

        if (energyText != null)
        {
            energyText.text = value;
        }

        if (energyTmpText != null)
        {
            energyTmpText.text = value;
        }
    }

    private void AutoFindUiReferences()
    {
        if (itemButton1 == null)
        {
            itemButton1 = FindButtonInChildren("BuyButton_EnergyBackpack");
        }

        if (itemButton2 == null)
        {
            itemButton2 = FindButtonInChildren("BuyButton_SlowBurn");
        }

        if (itemButton3 == null)
        {
            itemButton3 = FindButtonInChildren("BuyButton_BiggerAbsorb");
        }

        if (enterNextCaveButton == null)
        {
            enterNextCaveButton = FindButtonInChildren("EnterNextCaveButton");
        }

        AutoFindTextReferences(itemButton1, ref itemText1, ref itemTmpText1);
        AutoFindTextReferences(itemButton2, ref itemText2, ref itemTmpText2);
        AutoFindTextReferences(itemButton3, ref itemText3, ref itemTmpText3);
    }

    private void BindEnterNextCaveButton()
    {
        if (enterNextCaveButton == null)
        {
            return;
        }

        CaveLevelGenerator generator = FindFirstObjectByType<CaveLevelGenerator>();
        if (generator == null)
        {
            return;
        }

        generator.merchantRoomPanel = gameObject;
        generator.enterNextCaveButton = enterNextCaveButton;
        generator.merchantRoomController = this;

        enterNextCaveButton.onClick.RemoveListener(generator.ExitMerchantRoom);
        enterNextCaveButton.onClick.AddListener(generator.ExitMerchantRoom);
    }

    private Button FindButtonInChildren(string objectName)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == objectName)
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void AutoFindTextReferences(Button button, ref Text text, ref TMP_Text tmpText)
    {
        if (button == null)
        {
            return;
        }

        if (text == null)
        {
            text = button.GetComponentInChildren<Text>(true);
        }

        if (tmpText == null)
        {
            tmpText = button.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void ConfigureMerchantRoomLayout()
    {
        RectTransform panelRect = GetComponent<RectTransform>();
        if (panelRect == null)
        {
            return;
        }

        ConfigureCanvasScaler(panelRect);
        ConfigurePanel(panelRect);

        RectTransform backgroundOverlay = GetOrCreateChildRect("BackgroundOverlay", transform);
        ConfigureFullRect(backgroundOverlay, Vector2.zero, Vector2.zero);
        SetImageColor(backgroundOverlay.gameObject, new Color(0.015f, 0.018f, 0.025f, 0.78f));
        backgroundOverlay.SetAsFirstSibling();

        RectTransform shopFrame = GetOrCreateChildRect("ShopFrame", transform);
        ConfigureShopFrame(shopFrame);

        Text title = GetOrCreateText("TitleText", shopFrame, "Merchant Room", 42, TextAnchor.MiddleCenter);
        ConfigureAnchoredRect(title.rectTransform, new Vector2(0.28f, 0.88f), new Vector2(0.72f, 0.98f), Vector2.zero, Vector2.zero);

        energyText = GetOrCreateText("EnergyText", shopFrame, "", 26, TextAnchor.MiddleRight);
        ConfigureAnchoredRect(energyText.rectTransform, new Vector2(0.70f, 0.88f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

        RectTransform merchantArea = GetOrCreateChildRect("MerchantArea", shopFrame);
        ConfigureAnchoredRect(merchantArea, new Vector2(0.04f, 0.14f), new Vector2(0.28f, 0.84f), Vector2.zero, Vector2.zero);
        SetImageColor(merchantArea.gameObject, new Color(0.08f, 0.085f, 0.095f, 0.88f));

        ConfigureMerchantArea(merchantArea);

        RectTransform goodsContainer = GetOrCreateChildRect("GoodsContainer", shopFrame);
        ConfigureGoodsContainer(goodsContainer);

        itemButton1 = EnsureItemCard(goodsContainer, "ItemCard_EnergyBackpack", "BuyButton_EnergyBackpack", "Energy Backpack", "Cost: 30 Energy", "Max Energy +20", out itemText1, out itemTmpText1);
        itemButton2 = EnsureItemCard(goodsContainer, "ItemCard_SlowBurn", "BuyButton_SlowBurn", "Slow Burn", "Cost: 25 Energy", "Drain -20%", out itemText2, out itemTmpText2);
        itemButton3 = EnsureItemCard(goodsContainer, "ItemCard_BiggerAbsorb", "BuyButton_BiggerAbsorb", "Bigger Absorb", "Cost: 20 Energy", "Absorb Radius +1", out itemText3, out itemTmpText3);

        enterNextCaveButton = EnsureLeaveButton(shopFrame);
        RefreshEnergyText();
    }

    private void ConfigureCanvasScaler(RectTransform panelRect)
    {
        Canvas canvas = panelRect.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigurePanel(RectTransform panelRect)
    {
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.enabled = false;
        }
    }

    private void ConfigureShopFrame(RectTransform shopFrame)
    {
        ConfigureAnchoredRect(shopFrame, new Vector2(0.10f, 0.125f), new Vector2(0.90f, 0.875f), Vector2.zero, Vector2.zero);
        SetImageColor(shopFrame.gameObject, new Color(0.10f, 0.12f, 0.15f, 0.96f));
    }

    private void ConfigureMerchantArea(RectTransform merchantArea)
    {
        RectTransform silhouette = GetOrCreateChildRect("MerchantSilhouette", merchantArea);
        ConfigureAnchoredRect(silhouette, new Vector2(0.16f, 0.34f), new Vector2(0.84f, 0.92f), Vector2.zero, Vector2.zero);

        RectTransform head = GetOrCreateChildRect("Head", silhouette);
        ConfigureAnchoredRect(head, new Vector2(0.32f, 0.58f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero);
        Image headImage = SetImageColor(head.gameObject, new Color(0.02f, 0.022f, 0.026f, 1f));
        headImage.sprite = GetCircleSprite();

        RectTransform body = GetOrCreateChildRect("Body", silhouette);
        ConfigureAnchoredRect(body, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.62f), Vector2.zero, Vector2.zero);
        SetImageColor(body.gameObject, new Color(0.025f, 0.027f, 0.032f, 1f));

        Text dialogue = GetOrCreateText("MerchantDialogueText", merchantArea, "Spend your remaining energy wisely.", 22, TextAnchor.MiddleCenter);
        ConfigureAnchoredRect(dialogue.rectTransform, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.28f), Vector2.zero, Vector2.zero);
    }

    private void ConfigureGoodsContainer(RectTransform goodsContainer)
    {
        ConfigureAnchoredRect(goodsContainer, new Vector2(0.32f, 0.20f), new Vector2(0.96f, 0.82f), Vector2.zero, Vector2.zero);

        HorizontalLayoutGroup layout = goodsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = goodsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 24f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private Button EnsureItemCard(RectTransform parent, string cardName, string buyButtonName, string itemName, string cost, string description, out Text buyText, out TMP_Text buyTmpText)
    {
        RectTransform card = GetOrCreateChildRect(cardName, parent);
        SetImageColor(card.gameObject, new Color(0.145f, 0.15f, 0.165f, 0.98f));

        LayoutElement cardLayout = card.GetComponent<LayoutElement>();
        if (cardLayout == null)
        {
            cardLayout = card.gameObject.AddComponent<LayoutElement>();
        }
        cardLayout.minWidth = 250f;
        cardLayout.preferredWidth = 310f;
        cardLayout.minHeight = 360f;
        cardLayout.preferredHeight = 430f;

        VerticalLayoutGroup cardVertical = card.GetComponent<VerticalLayoutGroup>();
        if (cardVertical == null)
        {
            cardVertical = card.gameObject.AddComponent<VerticalLayoutGroup>();
        }
        cardVertical.padding = new RectOffset(22, 22, 22, 22);
        cardVertical.spacing = 14f;
        cardVertical.childAlignment = TextAnchor.UpperCenter;
        cardVertical.childControlWidth = true;
        cardVertical.childControlHeight = true;
        cardVertical.childForceExpandWidth = true;
        cardVertical.childForceExpandHeight = false;

        Text nameText = GetOrCreateText("ItemNameText", card, itemName, 28, TextAnchor.MiddleCenter);
        SetPreferredHeight(nameText.gameObject, 64f);

        Text costText = GetOrCreateText("ItemCostText", card, cost, 22, TextAnchor.MiddleCenter);
        costText.color = new Color(1f, 0.86f, 0.36f, 1f);
        SetPreferredHeight(costText.gameObject, 44f);

        Text descriptionText = GetOrCreateText("ItemDescriptionText", card, description, 22, TextAnchor.MiddleCenter);
        SetPreferredHeight(descriptionText.gameObject, 150f);

        Button buyButton = EnsureCardBuyButton(card, buyButtonName, out buyText, out buyTmpText);
        return buyButton;
    }

    private Button EnsureCardBuyButton(RectTransform card, string buttonName, out Text buyText, out TMP_Text buyTmpText)
    {
        RectTransform buttonRect = GetOrCreateChildRect(buttonName, card);
        SetPreferredHeight(buttonRect.gameObject, 58f);

        Image image = SetImageColor(buttonRect.gameObject, new Color(0.34f, 0.30f, 0.22f, 1f));
        Button button = buttonRect.GetComponent<Button>();
        if (button == null)
        {
            button = buttonRect.gameObject.AddComponent<Button>();
        }
        button.targetGraphic = image;

        buyText = GetOrCreateText("Text", buttonRect, "Buy", 24, TextAnchor.MiddleCenter);
        ConfigureFullRect(buyText.rectTransform, Vector2.zero, Vector2.zero);
        buyTmpText = buttonRect.GetComponentInChildren<TMP_Text>(true);
        return button;
    }

    private Button EnsureLeaveButton(RectTransform shopFrame)
    {
        RectTransform buttonRect = GetOrCreateChildRect("EnterNextCaveButton", shopFrame);
        ConfigureAnchoredRect(buttonRect, new Vector2(0.74f, 0.06f), new Vector2(0.96f, 0.15f), Vector2.zero, Vector2.zero);

        Image image = SetImageColor(buttonRect.gameObject, new Color(0.22f, 0.28f, 0.34f, 1f));
        Button button = buttonRect.GetComponent<Button>();
        if (button == null)
        {
            button = buttonRect.gameObject.AddComponent<Button>();
        }
        button.targetGraphic = image;

        Text text = GetOrCreateText("Text", buttonRect, "Leave Shop", 24, TextAnchor.MiddleCenter);
        ConfigureFullRect(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    private Text GetOrCreateText(string objectName, Transform parent, string content, int fontSize, TextAnchor alignment)
    {
        Transform existing = parent.Find(objectName);
        Text text = existing != null ? existing.GetComponent<Text>() : null;

        if (text == null)
        {
            GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform));
            if (existing == null)
            {
                textObject.transform.SetParent(parent, false);
            }

            text = textObject.GetComponent<Text>();
            if (text == null)
            {
                text = textObject.AddComponent<Text>();
            }
        }

        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = Mathf.Clamp(fontSize, 18, 44);
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private RectTransform GetOrCreateChildRect(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing.GetComponent<RectTransform>();
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private Image SetImageColor(GameObject target, Color color)
    {
        Image image = target.GetComponent<Image>();
        if (image == null)
        {
            image = target.AddComponent<Image>();
        }

        image.color = color;
        return image;
    }

    private void SetPreferredHeight(GameObject target, float height)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        layoutElement.preferredHeight = height;
    }

    private void ConfigureAnchoredRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void ConfigureFullRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        ConfigureAnchoredRect(rect, Vector2.zero, Vector2.one, offsetMin, offsetMax);
    }

    private Sprite GetCircleSprite()
    {
        if (generatedCircleSprite != null)
        {
            return generatedCircleSprite;
        }

        const int textureSize = 64;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        float radius = textureSize * 0.48f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = distance <= radius ? 1f : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        generatedCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        return generatedCircleSprite;
    }
}
