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

        Debug.Log($"[PAUSE] PausePanel active = {(pauseMenuPanel != null && pauseMenuPanel.activeSelf)}");
        Debug.Log($"[PAUSE] Time.timeScale = {Time.timeScale}");
    }

    public void ResumeGame()
    {
        Debug.Log("[PAUSE BUTTON] Resume clicked");
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Debug.Log($"[PAUSE] PausePanel active = {(pauseMenuPanel != null && pauseMenuPanel.activeSelf)}");
        Debug.Log($"[PAUSE] Time.timeScale = {Time.timeScale}");
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameSceneName);
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[PAUSE BUTTON] Main Menu clicked");
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("[PAUSE BUTTON] Quit clicked");
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
            ConfigurePausePanelBounds(pauseMenuPanel);
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

        Transform existingPanel = canvas.transform.Find("PausePanel");
        if (existingPanel == null)
        {
            existingPanel = canvas.transform.Find("PauseMenuPanel");
        }

        if (existingPanel != null)
        {
            pauseMenuPanel = existingPanel.gameObject;
            ConfigurePausePanelBounds(pauseMenuPanel);
            return;
        }

        pauseMenuPanel = CreatePauseMenu(canvas.transform);
        ConfigurePausePanelBounds(pauseMenuPanel);
        pauseMenuPanel.SetActive(false);
    }

    private GameObject CreatePauseMenu(Transform parent)
    {
        RectTransform root = CreateUiObject("PausePanel", parent);
        SetAnchoredRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 420f));

        Image panelImage = root.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);
        CreatePauseMenuContents(root);
        ConfigurePausePanelBounds(root.gameObject);

        return root.gameObject;
    }

    private void ConfigurePausePanelBounds(GameObject panelRoot)
    {
        RectTransform root = panelRoot.GetComponent<RectTransform>();
        if (root == null)
        {
            return;
        }

        SetAnchoredRect(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 420f));

        Image rootImage = root.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = root.gameObject.AddComponent<Image>();
        }
        rootImage.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);
        rootImage.raycastTarget = true;

        CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.ignoreParentGroups = false;

        Transform panelTransform = root.Find("PanelBackground");
        RectTransform panel = root;
        if (panelTransform == null)
        {
            CreatePauseMenuContents(root);
        }
        else if (panelTransform.TryGetComponent(out RectTransform existingPanel))
        {
            panel = existingPanel;
            SetAnchoredRect(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            VerticalLayoutGroup layout = panel.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(52, 52, 42, 42);
                layout.spacing = 28f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            Transform restartButton = panel.Find("RestartButton");
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }
        }

        BindExistingButton(panel, "ResumeButton", ResumeGame);
        BindExistingButton(panel, "MainMenuButton", ReturnToMainMenu);
        BindExistingButton(panel, "QuitButton", QuitGame);
    }

    private RectTransform CreatePauseMenuContents(RectTransform root)
    {
        if (root.Find("ResumeButton") != null)
        {
            return root;
        }

        Image panelImage = root.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = root.gameObject.AddComponent<Image>();
        }
        panelImage.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);
        panelImage.raycastTarget = true;

        RectTransform panel = root;

        Text title = CreateText("TitleText", panel, "Paused", 38, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 0.80f), new Vector2(0.5f, 0.80f), Vector2.zero, new Vector2(420f, 70f));

        Button resumeButton = CreateButton("ResumeButton", panel, "Resume");
        SetAnchoredRect(resumeButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(340f, 64f));

        Button mainMenuButton = CreateButton("MainMenuButton", panel, "Main Menu");
        SetAnchoredRect(mainMenuButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f), Vector2.zero, new Vector2(340f, 64f));

        Button quitButton = CreateButton("QuitButton", panel, "Quit");
        SetAnchoredRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.16f), new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(340f, 64f));

        return panel;
    }

    private void BindExistingButton(Transform parent, string buttonName, UnityEngine.Events.UnityAction action)
    {
        Transform buttonTransform = parent.Find(buttonName);
        if (buttonTransform == null)
        {
            return;
        }

        Button button = buttonTransform.GetComponent<Button>();
        if (button == null)
        {
            return;
        }

        button.enabled = true;
        button.interactable = true;
        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
        Debug.Log($"[PAUSE] Bound {buttonName} OnClick.");
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
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            BaseInputModule inputModule = eventSystem.GetComponent<BaseInputModule>();
            Debug.Log($"[PAUSE] EventSystem input module = {(inputModule != null ? inputModule.GetType().Name : "Missing")}");
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Debug.Log($"[PAUSE] EventSystem input module = {eventSystemObject.GetComponent<BaseInputModule>().GetType().Name}");
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
        button.targetGraphic = image;

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
