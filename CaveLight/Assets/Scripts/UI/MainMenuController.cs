using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuController : MonoBehaviour
{
    private const string MenuSceneName = "MainMenuScene";
    private const string GameSceneName = "GameScene";

    private static readonly Color BackgroundColor = new Color(0.02f, 0.04f, 0.07f, 1f);
    private static readonly Color PanelColor = new Color(0.07f, 0.11f, 0.16f, 0.95f);
    private static readonly Color ButtonColor = new Color(0.14f, 0.23f, 0.32f, 1f);
    private static readonly Color ButtonHoverColor = new Color(0.24f, 0.38f, 0.5f, 1f);
    private static Font cachedUiFont;
    private static bool sceneLoadHookRegistered;

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
        if (scene.name != MenuSceneName)
        {
            return;
        }

        if (FindFirstObjectByType<MainMenuController>() != null)
        {
            return;
        }

        new GameObject(nameof(MainMenuController)).AddComponent<MainMenuController>();
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name == MenuSceneName)
        {
            EnsureMenuUi();
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        RunStatsManager.Instance.ResetRun();
        SceneManager.LoadScene(GameSceneName);
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

    private void EnsureMenuUi()
    {
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

        Transform existingBackground = canvas.transform.Find("Background");
        if (existingBackground == null)
        {
            CreateBackground(canvas.transform);
        }

        Transform existingRoot = canvas.transform.Find("MainMenuRoot");
        if (existingRoot != null)
        {
            return;
        }

        RectTransform root = CreateUiObject("MainMenuRoot", canvas.transform);
        Stretch(root);

        Text title = CreateText("TitleText", root, "CaveLight", 74, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        SetAnchoredRect(title.rectTransform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.68f), new Vector2(0f, 0f), new Vector2(900f, 110f));

        Text subtitle = CreateText("SubtitleText", root, "\u6d1e\u7a74\u4f59\u5149", 36, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.78f, 0.9f, 1f, 1f));
        SetAnchoredRect(subtitle.rectTransform, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), new Vector2(0f, 0f), new Vector2(600f, 80f));

        Button startButton = CreateButton("StartButton", root, "\u5f00\u59cb\u6e38\u620f");
        SetAnchoredRect(startButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.41f), new Vector2(0.5f, 0.41f), Vector2.zero, new Vector2(360f, 76f));
        startButton.onClick.AddListener(StartGame);

        Button quitButton = CreateButton("QuitButton", root, "\u9000\u51fa\u6e38\u620f");
        SetAnchoredRect(quitButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.29f), new Vector2(0.5f, 0.29f), Vector2.zero, new Vector2(360f, 76f));
        quitButton.onClick.AddListener(QuitGame);
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

    private static void CreateBackground(Transform parent)
    {
        RectTransform background = CreateUiObject("Background", parent);
        Stretch(background);
        Image backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = BackgroundColor;

        for (int i = 0; i < 10; i++)
        {
            RectTransform shadow = CreateUiObject("CaveShadow_" + i, background);
            Vector2 anchor = new Vector2(0.1f + (i % 5) * 0.2f, 0.18f + (i / 5) * 0.54f);
            float width = 180f + (i % 3) * 80f;
            float height = 70f + (i % 4) * 35f;
            SetAnchoredRect(shadow, anchor, anchor, new Vector2((i % 2 == 0 ? -60f : 55f), (i % 3 - 1) * 34f), new Vector2(width, height));

            Image shadowImage = shadow.gameObject.AddComponent<Image>();
            shadowImage.color = new Color(0f, 0f, 0f, 0.18f);
        }
    }

    private static Button CreateButton(string objectName, Transform parent, string label)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = ButtonColor;

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = ButtonHoverColor;
        colors.pressedColor = new Color(0.09f, 0.16f, 0.22f, 1f);
        colors.selectedColor = ButtonHoverColor;
        button.colors = colors;

        Text text = CreateText("Text", rect, label, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
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
