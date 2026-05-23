using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string MainMenuSceneName = "MainMenuScene";

    public GameObject gameOverPanel;
    public Text titleText;
    public TMP_Text titleTmpText;
    public Text cavesClearedText;
    public TMP_Text cavesClearedTmpText;
    public Text monstersKilledText;
    public TMP_Text monstersKilledTmpText;
    public Text purchasedItemsText;
    public TMP_Text purchasedItemsTmpText;
    public Button returnMainMenuButton;
    public PlayerEnergyStore playerEnergyStore;

    private static Font cachedUiFont;
    private static bool sceneLoadHookRegistered;
    private bool warnedMissingEnergyStore;
    private bool gameOverVisible;

    public bool IsGameOverVisible => gameOverVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHook()
    {
        if (sceneLoadHookRegistered)
        {
            return;
        }

        sceneLoadHookRegistered = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != GameSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<GameOverController>() != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.AddComponent<GameOverController>();
    }

    private void Awake()
    {
        EnsureGameOverUi();
    }

    private void Start()
    {
        EnsureGameOverUi();
        ResolvePlayerEnergyStore();
        SubscribeEnergyStore();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (playerEnergyStore != null)
        {
            playerEnergyStore.OnEnergyDepleted -= ShowGameOverPanel;
        }
    }

    public void ShowGameOverPanel()
    {
        EnsureGameOverUi();

        gameOverVisible = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SetText(titleText, titleTmpText, "\u63a2\u7d22\u7ed3\u675f");

        RunStatsManager stats = RunStatsManager.Instance;
        SetText(cavesClearedText, cavesClearedTmpText, $"\u901a\u8fc7\u5173\u5361\uff1a{stats.cavesCleared}");
        SetText(monstersKilledText, monstersKilledTmpText, $"\u51fb\u8d25\u654c\u4eba\uff1a{stats.monstersKilled}");
        SetText(purchasedItemsText, purchasedItemsTmpText, $"\u6700\u8fd1\u8d2d\u4e70\uff1a{stats.GetRecentPurchasedItemsText()}");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.transform.SetAsLastSibling();
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    private void ResolvePlayerEnergyStore()
    {
        if (playerEnergyStore != null)
        {
            return;
        }

        playerEnergyStore = FindFirstObjectByType<PlayerEnergyStore>();
        if (playerEnergyStore == null && !warnedMissingEnergyStore)
        {
            warnedMissingEnergyStore = true;
            Debug.LogWarning("[GameOverController] PlayerEnergyStore not found. Game over panel will not auto-open.");
        }
    }

    private void SubscribeEnergyStore()
    {
        if (playerEnergyStore == null)
        {
            return;
        }

        playerEnergyStore.OnEnergyDepleted -= ShowGameOverPanel;
        playerEnergyStore.OnEnergyDepleted += ShowGameOverPanel;
    }

    private void EnsureGameOverUi()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        EnsureEventSystem();

        if (gameOverPanel == null)
        {
            Transform existingPanel = transform.Find("GameOverPanel");
            gameOverPanel = existingPanel != null ? existingPanel.gameObject : CreateGameOverPanel(transform).gameObject;
        }

        if (returnMainMenuButton != null)
        {
            returnMainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            returnMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private RectTransform CreateGameOverPanel(Transform parent)
    {
        RectTransform panel = CreateUiObject("GameOverPanel", parent);
        SetAnchoredRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1152f, 648f));

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.02f, 0.025f, 0.03f, 0.94f);

        RectTransform background = CreateUiObject("Background", panel);
        Stretch(background);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.45f);

        titleText = CreateText("TitleText", panel, "\u63a2\u7d22\u7ed3\u675f", 56, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetAnchoredRect(titleText.rectTransform, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), Vector2.zero, new Vector2(900f, 80f));

        cavesClearedText = CreateText("CavesClearedText", panel, "\u901a\u8fc7\u5173\u5361\uff1a0", 34, FontStyle.Normal, TextAnchor.MiddleCenter);
        SetAnchoredRect(cavesClearedText.rectTransform, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.63f), Vector2.zero, new Vector2(900f, 58f));

        monstersKilledText = CreateText("MonstersKilledText", panel, "\u51fb\u8d25\u654c\u4eba\uff1a0", 34, FontStyle.Normal, TextAnchor.MiddleCenter);
        SetAnchoredRect(monstersKilledText.rectTransform, new Vector2(0.5f, 0.51f), new Vector2(0.5f, 0.51f), Vector2.zero, new Vector2(900f, 58f));

        purchasedItemsText = CreateText("PurchasedItemsText", panel, "\u6700\u8fd1\u8d2d\u4e70\uff1a\u6682\u65e0\u8d2d\u4e70\u5546\u54c1", 30, FontStyle.Normal, TextAnchor.MiddleCenter);
        SetAnchoredRect(purchasedItemsText.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(980f, 90f));

        returnMainMenuButton = CreateButton("ReturnMainMenuButton", panel, "\u8fd4\u56de\u4e3b\u83dc\u5355");
        SetAnchoredRect(returnMainMenuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(360f, 74f));

        panel.gameObject.SetActive(false);
        return panel;
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.28f, 0.38f, 1f);

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.16f, 0.28f, 0.38f, 1f);
        colors.highlightedColor = new Color(0.26f, 0.42f, 0.54f, 1f);
        colors.pressedColor = new Color(0.1f, 0.2f, 0.28f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText("Text", rect, label, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
        return button;
    }

    private static Text CreateText(string objectName, Transform parent, string content, int fontSize, FontStyle style, TextAnchor alignment)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = content;
        text.font = GetUiFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void SetText(Text text, TMP_Text tmpText, string value)
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

    private static Font GetUiFont()
    {
        if (cachedUiFont != null)
        {
            return cachedUiFont;
        }

        cachedUiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 32);
        if (cachedUiFont == null)
        {
            cachedUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return cachedUiFont;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void SetAnchoredRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
