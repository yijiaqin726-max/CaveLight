using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MerchantRoomController : MonoBehaviour
{
    [Header("Player")]
    public PlayerEnergyStore playerEnergyStore;
    public PlayerAttack playerAttack;
    public EnergyAbsorbController energyAbsorbController;
    public PlayerLightController playerLightController;

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

    [Header("Dialogue")]
    public Text dialogueText;
    public TMP_Text dialogueTmpText;
    public GameObject dialogueBox;

    [Header("Navigation")]
    public Button enterNextCaveButton;

    private readonly List<ShopItem> itemPool = new List<ShopItem>();
    private readonly ShopItem[] currentItems = new ShopItem[3];
    private readonly bool[] purchased = new bool[3];
    private readonly Text[] nameTexts = new Text[3];
    private readonly Text[] costTexts = new Text[3];
    private readonly Text[] descriptionTexts = new Text[3];
    private readonly Text[] buyTexts = new Text[3];
    private Button[] itemButtons;
    private Coroutine dialogueCoroutine;
    private Sprite generatedCircleSprite;

    private class ShopItem
    {
        public string Name;
        public int Cost;
        public string Description;
        public Action Apply;

        public ShopItem(string name, int cost, string description, Action apply)
        {
            Name = name;
            Cost = cost;
            Description = description;
            Apply = apply;
        }
    }

    void Awake()
    {
        FindReferences();
        BuildItemPool();
        ConfigureMerchantRoomLayout();
        BindButtons();
    }

    void OnEnable()
    {
        Cursor.visible = true;
        FindReferences();
        ConfigureMerchantRoomLayout();
        RefreshEnergyText();
    }

    public void OnEnterMerchantRoom()
    {
        Cursor.visible = true;
        FindReferences();
        BuildItemPool();
        ConfigureMerchantRoomLayout();
        RollShopItems();
        BindButtons();
        RefreshShopUi();
        ShowDialogue("需要用能源来交换商品。", 2f);
    }

    private void FindReferences()
    {
        if (playerEnergyStore == null)
        {
            playerEnergyStore = FindFirstObjectByType<PlayerEnergyStore>();
        }

        if (playerAttack == null)
        {
            playerAttack = FindFirstObjectByType<PlayerAttack>();
        }

        if (energyAbsorbController == null)
        {
            energyAbsorbController = FindFirstObjectByType<EnergyAbsorbController>();
        }

        if (playerLightController == null)
        {
            playerLightController = FindFirstObjectByType<PlayerLightController>();
        }
    }

    private void BuildItemPool()
    {
        itemPool.Clear();
        itemPool.Add(new ShopItem("蓄能背包", 30, "最大能源 +20，并立即恢复 20 点能源。", ApplyEnergyBackpack));
        itemPool.Add(new ShopItem("锋利矿镐", 25, "攻击力 +1。", ApplySharpPickaxe));
        itemPool.Add(new ShopItem("稳定灯芯", 25, "能源消耗速度降低 20%。", ApplyStableWick));
        itemPool.Add(new ShopItem("广域磁吸", 20, "能源吸收范围 +1。", ApplyWideMagnet));
        itemPool.Add(new ShopItem("照明水晶", 25, "照明范围 +1。真实光照系统完成后生效。", ApplyLightCrystal));
    }

    private void RollShopItems()
    {
        List<ShopItem> available = new List<ShopItem>(itemPool);
        for (int i = 0; i < currentItems.Length; i++)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            currentItems[i] = available[index];
            available.RemoveAt(index);
            purchased[i] = false;
        }
    }

    private void BindButtons()
    {
        itemButtons = new[] { itemButton1, itemButton2, itemButton3 };
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int itemIndex = i;
            if (itemButtons[i] == null)
            {
                continue;
            }

            itemButtons[i].onClick.RemoveAllListeners();
            itemButtons[i].onClick.AddListener(() => TryBuyItem(itemIndex));
        }

        BindLeaveButton();
    }

    private void BindLeaveButton()
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

    private void TryBuyItem(int index)
    {
        if (index < 0 || index >= currentItems.Length || currentItems[index] == null || purchased[index])
        {
            return;
        }

        FindReferences();
        ShopItem item = currentItems[index];
        if (playerEnergyStore == null || playerEnergyStore.currentEnergy < item.Cost)
        {
            ShowDialogue("能源不够。", 2f);
            Debug.Log($"[MerchantRoomController] 能源不够：{item.Name}");
            return;
        }

        playerEnergyStore.ConsumeEnergy(item.Cost);
        item.Apply?.Invoke();
        purchased[index] = true;
        ShowDialogue($"已购买：{item.Name}", 1.5f);
        Debug.Log($"[MerchantRoomController] 已购买：{item.Name}");
        RefreshShopUi();
    }

    private void ApplyEnergyBackpack()
    {
        if (playerEnergyStore == null)
        {
            return;
        }

        playerEnergyStore.maxEnergy += 20f;
        playerEnergyStore.AddEnergy(20f);
    }

    private void ApplySharpPickaxe()
    {
        if (playerAttack != null)
        {
            playerAttack.attackDamage += 1f;
        }
    }

    private void ApplyStableWick()
    {
        if (playerEnergyStore != null)
        {
            playerEnergyStore.drainPerSecond *= 0.8f;
        }
    }

    private void ApplyWideMagnet()
    {
        if (energyAbsorbController != null)
        {
            energyAbsorbController.absorbRadius += 1f;
        }
    }

    private void ApplyLightCrystal()
    {
        if (playerLightController != null && playerLightController.enabled)
        {
            playerLightController.maxOuterRadius += 1f;
            return;
        }

        Debug.Log("照明范围升级已购买，等待真实光照系统接入。");
    }

    private void RefreshShopUi()
    {
        RefreshEnergyText();

        for (int i = 0; i < currentItems.Length; i++)
        {
            ShopItem item = currentItems[i];
            if (item == null)
            {
                continue;
            }

            SetText(nameTexts[i], item.Name);
            SetText(costTexts[i], $"消耗能源：{item.Cost}");
            SetText(descriptionTexts[i], item.Description);
            SetText(buyTexts[i], purchased[i] ? "已购买" : "购买");

            if (itemButtons != null && i < itemButtons.Length && itemButtons[i] != null)
            {
                itemButtons[i].interactable = !purchased[i];
            }
        }
    }

    private void RefreshEnergyText()
    {
        string value = playerEnergyStore != null
            ? $"当前能源：{playerEnergyStore.currentEnergy:0} / {playerEnergyStore.maxEnergy:0}"
            : "当前能源：-- / --";

        SetText(energyText, value);
        SetText(energyTmpText, value);
    }

    private void ShowDialogue(string message, float duration)
    {
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(true);
        }

        SetText(dialogueText, message);
        SetText(dialogueTmpText, message);

        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
        }

        dialogueCoroutine = StartCoroutine(HideDialogueAfterDelay(duration));
    }

    private System.Collections.IEnumerator HideDialogueAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (dialogueBox != null)
        {
            dialogueBox.SetActive(false);
        }
        dialogueCoroutine = null;
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
        SetImageColor(backgroundOverlay.gameObject, new Color(0.02f, 0.018f, 0.025f, 0.82f));
        backgroundOverlay.SetAsFirstSibling();

        RectTransform shopFrame = GetOrCreateChildRect("ShopFrame", transform);
        ClearChildren(shopFrame);
        ConfigureShopFrame(shopFrame);

        Text title = GetOrCreateText("TitleText", shopFrame, "商人房", 42, TextAnchor.MiddleCenter);
        ConfigureAnchoredRect(title.rectTransform, new Vector2(0.30f, 0.88f), new Vector2(0.70f, 0.98f), Vector2.zero, Vector2.zero);

        energyText = GetOrCreateText("EnergyText", shopFrame, "", 26, TextAnchor.MiddleRight);
        ConfigureAnchoredRect(energyText.rectTransform, new Vector2(0.66f, 0.88f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

        dialogueBox = GetOrCreateChildRect("DialogueBox", shopFrame).gameObject;
        ConfigureAnchoredRect((RectTransform)dialogueBox.transform, new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.86f), Vector2.zero, Vector2.zero);
        SetImageColor(dialogueBox, new Color(0.06f, 0.065f, 0.075f, 0.96f));

        dialogueText = GetOrCreateText("DialogueText", dialogueBox.transform, "需要用能源来交换商品。", 26, TextAnchor.MiddleCenter);
        ConfigureFullRect(dialogueText.rectTransform, new Vector2(20f, 10f), new Vector2(-20f, -10f));

        RectTransform goodsContainer = GetOrCreateChildRect("GoodsContainer", shopFrame);
        ConfigureGoodsContainer(goodsContainer);

        itemButton1 = EnsureItemCard(goodsContainer, 0);
        itemButton2 = EnsureItemCard(goodsContainer, 1);
        itemButton3 = EnsureItemCard(goodsContainer, 2);
        itemButtons = new[] { itemButton1, itemButton2, itemButton3 };

        enterNextCaveButton = EnsureLeaveButton(shopFrame);
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

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.enabled = false;
        }
    }

    private void ConfigureShopFrame(RectTransform shopFrame)
    {
        ConfigureAnchoredRect(shopFrame, new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.88f), Vector2.zero, Vector2.zero);
        SetImageColor(shopFrame.gameObject, new Color(0.10f, 0.11f, 0.13f, 0.97f));
    }

    private void ConfigureGoodsContainer(RectTransform goodsContainer)
    {
        ConfigureAnchoredRect(goodsContainer, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.68f), Vector2.zero, Vector2.zero);

        HorizontalLayoutGroup layout = goodsContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = goodsContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 26f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
    }

    private Button EnsureItemCard(RectTransform parent, int index)
    {
        RectTransform card = GetOrCreateChildRect($"ItemCard_{index + 1}", parent);
        SetImageColor(card.gameObject, new Color(0.15f, 0.155f, 0.17f, 1f));
        EnsureLayoutElement(card.gameObject, 310f, 360f);

        VerticalLayoutGroup layout = card.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.padding = new RectOffset(22, 22, 22, 22);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        nameTexts[index] = GetOrCreateText("ItemNameText", card, "商品", 30, TextAnchor.MiddleCenter);
        EnsureLayoutElement(nameTexts[index].gameObject, 0f, 58f);

        costTexts[index] = GetOrCreateText("ItemCostText", card, "消耗能源：--", 24, TextAnchor.MiddleCenter);
        costTexts[index].color = new Color(1f, 0.86f, 0.36f, 1f);
        EnsureLayoutElement(costTexts[index].gameObject, 0f, 44f);

        descriptionTexts[index] = GetOrCreateText("ItemDescriptionText", card, "描述", 22, TextAnchor.MiddleCenter);
        EnsureLayoutElement(descriptionTexts[index].gameObject, 0f, 126f);

        RectTransform buttonRect = GetOrCreateChildRect("BuyButton", card);
        EnsureLayoutElement(buttonRect.gameObject, 0f, 58f);
        Image image = SetImageColor(buttonRect.gameObject, new Color(0.34f, 0.30f, 0.20f, 1f));

        Button button = buttonRect.GetComponent<Button>();
        if (button == null)
        {
            button = buttonRect.gameObject.AddComponent<Button>();
        }
        button.targetGraphic = image;

        buyTexts[index] = GetOrCreateText("Text", buttonRect, "购买", 24, TextAnchor.MiddleCenter);
        ConfigureFullRect(buyTexts[index].rectTransform, Vector2.zero, Vector2.zero);

        ConfigureHover(card.gameObject, index);
        return button;
    }

    private void ConfigureHover(GameObject card, int index)
    {
        EventTrigger trigger = card.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = card.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();
        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (currentItems[index] != null)
            {
                ShowDialogue(currentItems[index].Description, 2f);
            }
        });
        trigger.triggers.Add(enter);
    }

    private Button EnsureLeaveButton(RectTransform shopFrame)
    {
        RectTransform buttonRect = GetOrCreateChildRect("EnterNextCaveButton", shopFrame);
        ConfigureAnchoredRect(buttonRect, new Vector2(0.78f, 0.06f), new Vector2(0.96f, 0.15f), Vector2.zero, Vector2.zero);

        Image image = SetImageColor(buttonRect.gameObject, new Color(0.22f, 0.28f, 0.34f, 1f));
        Button button = buttonRect.GetComponent<Button>();
        if (button == null)
        {
            button = buttonRect.gameObject.AddComponent<Button>();
        }
        button.targetGraphic = image;

        Text text = GetOrCreateText("Text", buttonRect, "离开", 26, TextAnchor.MiddleCenter);
        ConfigureFullRect(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    private Text GetOrCreateText(string objectName, Transform parent, string content, int fontSize, TextAnchor alignment)
    {
        Transform existing = parent.Find(objectName);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName, typeof(RectTransform));
        if (existing == null)
        {
            textObject.transform.SetParent(parent, false);
        }

        Text text = textObject.GetComponent<Text>();
        if (text == null)
        {
            text = textObject.AddComponent<Text>();
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
            RectTransform existingRect = existing.GetComponent<RectTransform>();
            if (existingRect != null)
            {
                return existingRect;
            }
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (Application.isPlaying)
            {
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
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

    private void EnsureLayoutElement(GameObject target, float preferredWidth, float preferredHeight)
    {
        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = target.AddComponent<LayoutElement>();
        }

        if (preferredWidth > 0f)
        {
            layoutElement.preferredWidth = preferredWidth;
        }

        if (preferredHeight > 0f)
        {
            layoutElement.preferredHeight = preferredHeight;
        }
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

    private void SetText(Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }
}
