using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenuController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string MainMenuSceneName = "MainMenuScene";

    [SerializeField] private GameObject pauseMenuPanel;

    private static Font cachedUiFont;
    private static bool sceneLoadHookRegistered;
    private bool isPaused;

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

        if (FindFirstObjectByType<PauseMenuController>() != null)
        {
            return;
        }

        new GameObject(nameof(PauseMenuController)).AddComponent<PauseMenuController>();
    }

    private void Start()
    {
        EnsurePauseMenuUi();
        ResumeGame();
    }

    private void Update()
    {
        GameOverController gameOverController = FindFirstObjectByType<GameOverController>();
        if (gameOverController != null && gameOverController.IsGameOverVisible)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        EnsurePauseMenuUi();

        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling();
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneName);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void EnsurePauseMenuUi()
    {
        if (pauseMenuPanel != null)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas("Canvas");
        }
        else
        {
            ConfigureCanvas(canvas);
        }

        EnsureEventSystem();

        Transform existingPanel = canvas.transform.Find("PauseMenuPanel");
        if (existingPanel != null)
        {
            pauseMenuPanel = existingPanel.gameObject;
            return;
        }

        pauseMenuPanel = CreatePauseMenu(canvas.transform);
        pauseMenuPanel.SetActive(false);
    }

    private GameObject CreatePauseMenu(Transform parent)
    {
        RectTransform root = CreateUiObject("PauseMenuPanel", parent);
        Stretch(root);

        Image overlay = root.gameObject.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.55f);

        RectTransform panel = CreateUiObject("PanelBackground", root);
        SetAnchoredRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 560f));

        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(52, 52, 46, 46);
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Text title = CreateText("TitleText", panel, "\u6682\u505c\u6e38\u620f", 38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 78f;

        Button resumeButton = CreateButton("ResumeButton", panel, "\u7ee7\u7eed\u6e38\u620f");
        resumeButton.onClick.AddListener(ResumeGame);

        Button restartButton = CreateButton("RestartButton", panel, "\u91cd\u65b0\u5f00\u59cb\u672c\u5c40");
        restartButton.onClick.AddListener(RestartGame);

        Button mainMenuButton = CreateButton("MainMenuButton", panel, "\u8fd4\u56de\u4e3b\u83dc\u5355");
        mainMenuButton.onClick.AddListener(ReturnToMainMenu);

        Button quitButton = CreateButton("QuitButton", panel, "\u9000\u51fa\u6e38\u620f");
        quitButton.onClick.AddListener(QuitGame);

        return root.gameObject;
    }

    private static Canvas CreateCanvas(string objectName)
    {
        GameObject canvasObject = new GameObject(objectName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        ConfigureCanvas(canvas);
        return canvas;
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.14f, 0.2f, 0.27f, 1f);

        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 72f;
        layoutElement.minHeight = 64f;

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.14f, 0.2f, 0.27f, 1f);
        colors.highlightedColor = new Color(0.24f, 0.34f, 0.43f, 1f);
        colors.pressedColor = new Color(0.09f, 0.13f, 0.18f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        Text text = CreateText("Text", rect, label, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);

        return button;
    }

    private static Text CreateText(string objectName, Transform parent, string content, int fontSize, FontStyle style, TextAnchor anchor, Color color)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        Text text = rect.gameObject.AddComponent<Text>();
        text.text = content;
        text.font = GetUiFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
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
}
