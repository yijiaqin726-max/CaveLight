using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MerchantRoomController : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private GameObject merchantRoomPanel;
    [SerializeField] private CaveLevelGenerator caveLevelGenerator;
    [SerializeField] private PlayerEnergyStore playerEnergyStore;
    [SerializeField] private EnergyAbsorbController energyAbsorbController;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private PlayerInvincibility playerInvincibility;

    [Header("Scene UI")]
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private MerchantCardUI[] cardUis = new MerchantCardUI[3];

    public string lastMethod = "None";
    public GameObject MerchantRoomPanel => merchantRoomPanel;
    public Button LeaveButton => leaveButton;

    private readonly List<MerchantProductData> productPool = new List<MerchantProductData>();
    private readonly MerchantProductData[] currentProducts = new MerchantProductData[3];
    private readonly bool[] soldOut = new bool[3];

    private RectTransform tablecloth;
    private RectTransform goodsContainer;

    private void Awake()
    {
        Debug.Log("[MERCHANT VERIFY] Controller Awake");
        ResolveReferences();
        BuildProductPool();
        EnsureUiCanReceiveInput();
        BuildMerchantUi();
        BindLeaveButton();
        LogAssignedReferences();
    }

    private void Start()
    {
        Debug.Log("[MERCHANT VERIFY] Controller Start");
        ResolveReferences();
        RefreshEnergyText();
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
            leaveButton = leaveButtonReference;
        }

        ResolveReferences();
        BuildMerchantUi();
        BindLeaveButton();
    }

    public void OnEnterMerchantRoom()
    {
        OpenMerchantRoom();
    }

    public void ShowMerchantRoom()
    {
        OpenMerchantRoom();
    }

    public void OpenMerchantRoom()
    {
        lastMethod = nameof(OpenMerchantRoom);
        Debug.Log("[MERCHANT VERIFY] ShowMerchantRoom called");

        ResolveReferences();
        BuildProductPool();
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
        BuildMerchantUi();
        RollThreeProducts();
        BindCards();
        BindLeaveButton();
        RefreshEnergyText();
        SetMessage(string.Empty);

        Debug.Log($"[MERCHANT VERIFY] Panel active after SetActive = {merchantRoomPanel.activeSelf}");
        Debug.Log("[MERCHANT VERIFY] Product buttons bound = true");
        Debug.Log($"[MERCHANT VERIFY] Leave button bound = {leaveButton != null}");
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
            Debug.Log("[MERCHANT VERIFY] Continue next cave after merchant");
        }
        else
        {
            Debug.LogError("[MERCHANT ERROR] CaveLevelGenerator is null. Cannot leave merchant room.");
        }
    }

    private void TryBuyCard(int index)
    {
        if (index < 0 || index >= currentProducts.Length || currentProducts[index] == null)
        {
            return;
        }

        MerchantProductData product = currentProducts[index];
        Debug.Log($"[MERCHANT BUTTON] Buy product index = {index}");

        if (soldOut[index])
        {
            SetMessage("Sold out");
            return;
        }

        ResolveReferences();
        if (playerEnergyStore == null)
        {
            SetMessage("Energy store missing");
            Debug.LogWarning("[MERCHANT VERIFY] PlayerEnergyStore is null. Purchase skipped.");
            return;
        }

        if (!playerEnergyStore.TrySpendEnergy(product.cost))
        {
            SetMessage("Not enough energy");
            return;
        }

        ApplyProduct(product);
        RunStatsManager.Instance.AddPurchasedItem(product.displayName);
        soldOut[index] = true;
        if (cardUis[index] != null)
        {
            cardUis[index].SetSoldOut(true);
        }

        SetMessage("Purchased");
        RefreshEnergyText();
    }

    private void ApplyProduct(MerchantProductData product)
    {
        ResolveReferences();

        switch (product.type)
        {
            case MerchantProductType.MaxEnergy:
                if (playerEnergyStore != null)
                {
                    playerEnergyStore.maxEnergy += product.value;
                    playerEnergyStore.AddEnergy(product.value);
                }
                break;
            case MerchantProductType.RestoreEnergy:
                playerEnergyStore?.AddEnergy(product.value);
                break;
            case MerchantProductType.AttackDamage:
            case MerchantProductType.HeavyStrike:
                if (playerAttack != null)
                {
                    playerAttack.attackDamage += product.value;
                }
                break;
            case MerchantProductType.AttackSpeed:
                if (playerAttack != null)
                {
                    playerAttack.attackCooldown = Mathf.Max(0.08f, playerAttack.attackCooldown - product.value);
                }
                break;
            case MerchantProductType.AbsorbRadius:
                if (energyAbsorbController != null)
                {
                    energyAbsorbController.absorbRadius += product.value;
                }
                break;
            case MerchantProductType.AbsorbSpeed:
                if (energyAbsorbController != null)
                {
                    energyAbsorbController.absorbSpeed += product.value;
                }
                break;
            case MerchantProductType.InvincibleTime:
                if (playerInvincibility != null)
                {
                    playerInvincibility.invincibleDuration += product.value;
                }
                break;
            case MerchantProductType.LightRadius:
            case MerchantProductType.EnergySaver:
            case MerchantProductType.StableLight:
            case MerchantProductType.DamageBuffer:
            case MerchantProductType.Utility:
                Debug.Log($"[MERCHANT PLACEHOLDER] {product.displayName} purchased. Effect hook not implemented yet.");
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

        if (playerEnergyStore != null)
        {
            energyAbsorbController ??= playerEnergyStore.GetComponent<EnergyAbsorbController>();
            playerAttack ??= playerEnergyStore.GetComponent<PlayerAttack>();
            playerInvincibility ??= playerEnergyStore.GetComponent<PlayerInvincibility>();
        }
    }

    private void BuildMerchantUi()
    {
        if (merchantRoomPanel == null)
        {
            return;
        }

        RectTransform panelRect = EnsureRect(merchantRoomPanel);
        Stretch(panelRect);
        Image panelImage = EnsureImage(merchantRoomPanel);
        panelImage.color = Color.clear;
        panelImage.raycastTarget = false;

        RectTransform dimOverlay = GetOrCreateRect("MerchantDimOverlay", merchantRoomPanel.transform);
        dimOverlay.SetAsFirstSibling();
        Stretch(dimOverlay);
        Image dimImage = EnsureImage(dimOverlay.gameObject);
        dimImage.color = new Color(0f, 0f, 0f, 0.55f);
        dimImage.raycastTarget = true;

        tablecloth = GetOrCreateRect("MerchantTablecloth", merchantRoomPanel.transform);
        tablecloth.anchorMin = new Vector2(0.5f, 0.5f);
        tablecloth.anchorMax = new Vector2(0.5f, 0.5f);
        tablecloth.pivot = new Vector2(0.5f, 0.5f);
        tablecloth.anchoredPosition = new Vector2(0f, -25f);
        tablecloth.sizeDelta = new Vector2(1280f, 690f);
        tablecloth.SetAsLastSibling();
        Image tableImage = EnsureImage(tablecloth.gameObject);
        tableImage.color = new Color(0.23f, 0.43f, 0.34f, 0.96f);
        tableImage.raycastTarget = true;
        EnsureTablePattern(tablecloth);

        TMP_Text title = EnsureText("MerchantTitleText", tablecloth, new Vector2(0f, 288f), new Vector2(520f, 56f), 40f, FontStyles.Bold, TextAlignmentOptions.Center);
        title.text = "Merchant Room";
        title.color = new Color(1f, 0.88f, 0.55f, 1f);

        TMP_Text dialogue = EnsureText("MerchantDialogueText", tablecloth, new Vector2(0f, 240f), new Vector2(620f, 38f), 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        dialogue.text = "Spend your energy wisely.";
        dialogue.color = new Color(0.92f, 0.93f, 0.82f, 1f);

        energyText = EnsureText("EnergyText", tablecloth, new Vector2(470f, 278f), new Vector2(250f, 42f), 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        energyText.color = new Color(1f, 0.86f, 0.36f, 1f);

        goodsContainer = GetOrCreateRect("GoodsContainer", tablecloth);
        goodsContainer.anchorMin = new Vector2(0.5f, 0.5f);
        goodsContainer.anchorMax = new Vector2(0.5f, 0.5f);
        goodsContainer.pivot = new Vector2(0.5f, 0.5f);
        goodsContainer.anchoredPosition = new Vector2(0f, -35f);
        goodsContainer.sizeDelta = new Vector2(940f, 380f);

        for (int i = 0; i < 3; i++)
        {
            RectTransform cardRect = GetOrCreateRect($"MerchantCard_{i + 1}", goodsContainer);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2((i - 1) * 310f, 0f);
            cardRect.sizeDelta = new Vector2(260f, 360f);
            MerchantCardUI card = cardRect.GetComponent<MerchantCardUI>();
            if (card == null)
            {
                card = cardRect.gameObject.AddComponent<MerchantCardUI>();
            }

            cardUis[i] = card;
        }

        leaveButton = EnsureButton("LeaveButton", tablecloth, new Vector2(-520f, -292f), new Vector2(180f, 54f), "Leave");
        messageText = EnsureText("MessageText", tablecloth, new Vector2(0f, -296f), new Vector2(500f, 40f), 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        messageText.color = new Color(1f, 0.85f, 0.45f, 1f);

        HideLegacyProductButtons();
    }

    private void EnsureTablePattern(RectTransform parent)
    {
        RectTransform patternRoot = GetOrCreateRect("TableclothPattern", parent);
        Stretch(patternRoot);
        patternRoot.SetAsFirstSibling();

        int index = 0;
        for (int y = -250; y <= 210; y += 90)
        {
            for (int x = -520; x <= 520; x += 130)
            {
                TMP_Text mark = EnsureText($"Pattern_{index}", patternRoot, new Vector2(x, y), new Vector2(54f, 36f), 34f, FontStyles.Bold, TextAlignmentOptions.Center);
                mark.text = "^";
                mark.color = new Color(0.72f, 0.86f, 0.38f, 0.23f);
                index++;
            }
        }
    }

    private void RollThreeProducts()
    {
        for (int i = 0; i < soldOut.Length; i++)
        {
            soldOut[i] = false;
        }

        List<int> availableIndexes = new List<int>();
        for (int i = 0; i < productPool.Count; i++)
        {
            availableIndexes.Add(i);
        }

        for (int slot = 0; slot < 3; slot++)
        {
            int randomListIndex = Random.Range(0, availableIndexes.Count);
            int productIndex = availableIndexes[randomListIndex];
            availableIndexes.RemoveAt(randomListIndex);
            currentProducts[slot] = productPool[productIndex];
        }
    }

    private void BindCards()
    {
        for (int i = 0; i < cardUis.Length; i++)
        {
            int index = i;
            if (cardUis[i] == null || currentProducts[i] == null)
            {
                continue;
            }

            cardUis[i].Bind(currentProducts[i], soldOut[i]);
            Button button = cardUis[i].Button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => TryBuyCard(index));
        }
    }

    private void BindLeaveButton()
    {
        if (leaveButton == null)
        {
            return;
        }

        leaveButton.onClick.RemoveAllListeners();
        leaveButton.onClick.AddListener(LeaveMerchantRoom);
        leaveButton.interactable = true;
    }

    private void RefreshEnergyText()
    {
        if (energyText == null)
        {
            return;
        }

        energyText.text = playerEnergyStore != null ? $"Energy: {Mathf.FloorToInt(playerEnergyStore.currentEnergy)}" : "Energy: --";
    }

    private void SetMessage(string value)
    {
        if (messageText == null)
        {
            return;
        }

        messageText.text = value;
        messageText.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private void EnsureUiCanReceiveInput()
    {
        Canvas canvas = merchantRoomPanel != null ? merchantRoomPanel.GetComponentInParent<Canvas>() : FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private void HideLegacyProductButtons()
    {
        string[] legacyNames = { "ProductButton_1", "ProductButton_2", "ProductButton_3", "GoodsItem1", "GoodsItem2", "GoodsItem3", "MerchantRoot", "LeaveButton", "TitleText", "DialogueText" };
        for (int i = 0; i < legacyNames.Length; i++)
        {
            HideLegacyObjectsByName(merchantRoomPanel.transform, legacyNames[i]);
        }
    }

    private void HideLegacyObjectsByName(Transform root, string objectName)
    {
        if (root == null)
        {
            return;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            HideLegacyObjectsByName(child, objectName);
        }

        if (root.name != objectName || root == tablecloth || root == goodsContainer || root.IsChildOf(tablecloth))
        {
            return;
        }

        root.gameObject.SetActive(false);
    }

    private void LogAssignedReferences()
    {
        Debug.Log($"[MERCHANT VERIFY] merchantRoomPanel assigned = {merchantRoomPanel != null}");
        Debug.Log($"[MERCHANT VERIFY] product cards assigned = {cardUis[0] != null && cardUis[1] != null && cardUis[2] != null}");
        Debug.Log($"[MERCHANT VERIFY] leaveButton assigned = {leaveButton != null}");
    }

    private void BuildProductPool()
    {
        if (productPool.Count > 0)
        {
            return;
        }

        Color energy = new Color(0.95f, 0.76f, 0.22f, 1f);
        Color attack = new Color(0.78f, 0.18f, 0.16f, 1f);
        Color absorb = new Color(0.28f, 0.7f, 0.36f, 1f);
        Color light = new Color(0.24f, 0.72f, 0.78f, 1f);
        Color defense = new Color(0.36f, 0.48f, 0.62f, 1f);
        Color utility = new Color(0.55f, 0.35f, 0.78f, 1f);

        productPool.Add(new MerchantProductData("max_energy_10", "Max Energy +10", "Increase max energy by 10.", 20, MerchantProductType.MaxEnergy, 10f, energy, "ENG"));
        productPool.Add(new MerchantProductData("max_energy_20", "Max Energy +20", "Increase max energy by 20.", 35, MerchantProductType.MaxEnergy, 20f, energy, "ENG"));
        productPool.Add(new MerchantProductData("max_energy_30", "Max Energy +30", "Increase max energy by 30.", 50, MerchantProductType.MaxEnergy, 30f, energy, "ENG"));
        productPool.Add(new MerchantProductData("restore_20", "Restore Energy 20", "Restore 20 current energy.", 14, MerchantProductType.RestoreEnergy, 20f, energy, "HEAL"));
        productPool.Add(new MerchantProductData("restore_40", "Restore Energy 40", "Restore 40 current energy.", 26, MerchantProductType.RestoreEnergy, 40f, energy, "HEAL"));
        productPool.Add(new MerchantProductData("energy_saver_1", "Energy Saver I", "Future hook: slower energy drain.", 32, MerchantProductType.EnergySaver, 0.5f, energy, "SAVE"));
        productPool.Add(new MerchantProductData("energy_saver_2", "Energy Saver II", "Future hook: much slower drain.", 48, MerchantProductType.EnergySaver, 1f, energy, "SAVE"));

        productPool.Add(new MerchantProductData("attack_1", "Attack +1", "Increase attack damage by 1.", 25, MerchantProductType.AttackDamage, 1f, attack, "ATK"));
        productPool.Add(new MerchantProductData("attack_2", "Attack +2", "Increase attack damage by 2.", 45, MerchantProductType.AttackDamage, 2f, attack, "ATK"));
        productPool.Add(new MerchantProductData("heavy_strike", "Heavy Strike", "Increase attack damage by 3.", 62, MerchantProductType.HeavyStrike, 3f, attack, "ATK"));
        productPool.Add(new MerchantProductData("quick_strike", "Quick Strike", "Slightly reduce attack cooldown.", 34, MerchantProductType.AttackSpeed, 0.04f, attack, "SPD"));
        productPool.Add(new MerchantProductData("attack_speed_1", "Attack Speed +", "Reduce attack cooldown.", 45, MerchantProductType.AttackSpeed, 0.06f, attack, "SPD"));
        productPool.Add(new MerchantProductData("attack_speed_2", "Attack Speed ++", "Greatly reduce attack cooldown.", 65, MerchantProductType.AttackSpeed, 0.09f, attack, "SPD"));

        productPool.Add(new MerchantProductData("bigger_absorb", "Bigger Absorb", "Increase absorb radius.", 28, MerchantProductType.AbsorbRadius, 1f, absorb, "ABS"));
        productPool.Add(new MerchantProductData("wide_absorb", "Wide Absorb", "Greatly increase absorb radius.", 48, MerchantProductType.AbsorbRadius, 1.8f, absorb, "ABS"));
        productPool.Add(new MerchantProductData("fast_absorb", "Fast Absorb", "Pull energy faster.", 30, MerchantProductType.AbsorbSpeed, 2f, absorb, "ABS"));
        productPool.Add(new MerchantProductData("magnet_pull", "Magnet Pull", "Increase radius and pull speed.", 54, MerchantProductType.AbsorbRadius, 1.5f, absorb, "MAG"));
        productPool.Add(new MerchantProductData("energy_collector", "Energy Collector", "Increase absorb radius by 2.", 58, MerchantProductType.AbsorbRadius, 2f, absorb, "MAG"));

        productPool.Add(new MerchantProductData("light_radius_1", "Light Radius +", "Future hook: larger visible circle.", 38, MerchantProductType.LightRadius, 0.8f, light, "LGT"));
        productPool.Add(new MerchantProductData("light_radius_2", "Light Radius ++", "Future hook: much larger light.", 58, MerchantProductType.LightRadius, 1.4f, light, "LGT"));
        productPool.Add(new MerchantProductData("stable_light", "Stable Light", "Future hook: light changes less sharply.", 42, MerchantProductType.StableLight, 1f, light, "LGT"));
        productPool.Add(new MerchantProductData("last_glow", "Last Glow", "Future hook: keep minimum light larger.", 52, MerchantProductType.StableLight, 1f, light, "LGT"));
        productPool.Add(new MerchantProductData("deep_vision", "Deep Vision", "Future hook: reveal more cave space.", 60, MerchantProductType.LightRadius, 2f, light, "LGT"));

        productPool.Add(new MerchantProductData("invincible_1", "Invincible Time +", "Increase hurt invincibility time.", 35, MerchantProductType.InvincibleTime, 0.25f, defense, "DEF"));
        productPool.Add(new MerchantProductData("invincible_2", "Invincible Time ++", "Greatly increase invincibility time.", 55, MerchantProductType.InvincibleTime, 0.5f, defense, "DEF"));
        productPool.Add(new MerchantProductData("damage_buffer", "Damage Buffer", "Future hook: block one hit.", 62, MerchantProductType.DamageBuffer, 1f, defense, "DEF"));
        productPool.Add(new MerchantProductData("emergency_spark", "Emergency Spark", "Future hook: survive at low energy.", 50, MerchantProductType.DamageBuffer, 1f, defense, "DEF"));

        productPool.Add(new MerchantProductData("cheap_upgrade", "Cheap Upgrade", "A modest upgrade for little energy.", 12, MerchantProductType.AttackDamage, 0.5f, utility, "UP"));
        productPool.Add(new MerchantProductData("lucky_crystal", "Lucky Crystal", "Future hook: improve crystal rewards.", 44, MerchantProductType.Utility, 1f, utility, "LUCK"));
        productPool.Add(new MerchantProductData("cave_blessing", "Cave Blessing", "Restore energy and gain max energy.", 70, MerchantProductType.MaxEnergy, 25f, utility, "BLESS"));
    }

    private Button EnsureButton(string name, Transform parent, Vector2 position, Vector2 size, string label)
    {
        RectTransform rect = GetOrCreateRect(name, parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = EnsureImage(rect.gameObject);
        image.color = new Color(0.19f, 0.12f, 0.08f, 0.96f);
        Button button = rect.GetComponent<Button>();
        if (button == null)
        {
            button = rect.gameObject.AddComponent<Button>();
        }

        button.targetGraphic = image;
        TMP_Text text = EnsureText("Text", rect, Vector2.zero, new Vector2(size.x - 20f, size.y - 8f), 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        text.text = label;
        return button;
    }

    private static RectTransform GetOrCreateRect(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null ? existing.GetComponent<RectTransform>() : null;
        if (rect == null)
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            rect = child.GetComponent<RectTransform>();
        }

        return rect;
    }

    private static TMP_Text EnsureText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        RectTransform rect = GetOrCreateRect(name, parent);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text == null)
        {
            text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
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

    private static RectTransform EnsureRect(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = target.AddComponent<RectTransform>();
        }

        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
