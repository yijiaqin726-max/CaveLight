using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BuildMainMenuSceneTool
{
    private const string ScenePath = "Assets/Scenes/MainMenuScene.unity";
    private const string BackgroundSearchFolder = "Assets/Art/Background";
    private const string BlueButtonPath = "Assets/Art/UI/KenneyUI/Blue/Default/button_rectangle_depth_flat.png";
    private const string GreyButtonPath = "Assets/Art/UI/KenneyUI/Grey/Default/button_rectangle_depth_flat.png";
    private const string ClickClipPath = "Assets/Art/UI/KenneyUI/Sounds/click-a.ogg";

    [MenuItem("CaveLight/Build Main Menu Scene")]
    public static void BuildMainMenuScene()
    {
        OpenMainMenuScene();
        EnsureMainCamera();
        CleanupOldMenuObjects();

        Sprite backgroundSprite = LoadFirstBackgroundSprite();
        Sprite blueButtonSprite = LoadSpriteWithImportSettings(BlueButtonPath);
        Sprite greyButtonSprite = LoadSpriteWithImportSettings(GreyButtonPath);
        AudioClip clickClip = LoadClickClip();

        Canvas canvas = CreateCanvas();
        CreateBackground(canvas.transform, backgroundSprite);
        CreateDarkOverlay(canvas.transform);
        CreateTitle(canvas.transform);
        CreateSubtitle(canvas.transform);

        MainMenuController controller = CreateMainMenuController();
        UIAudioManager audioManager = CreateUiAudioManager(clickClip);

        Button startButton = CreateMenuButton(
            "StartButton",
            canvas.transform,
            new Vector2(0f, -25f),
            "\u5f00\u59cb\u6e38\u620f",
            blueButtonSprite,
            Color.white);
        UnityEventTools.AddPersistentListener(startButton.onClick, audioManager.PlayButtonClick);
        UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartGame);

        Button quitButton = CreateMenuButton(
            "QuitButton",
            canvas.transform,
            new Vector2(0f, -125f),
            "\u9000\u51fa\u6e38\u620f",
            greyButtonSprite != null ? greyButtonSprite : blueButtonSprite,
            greyButtonSprite != null ? Color.white : new Color(0.68f, 0.68f, 0.72f, 1f));
        UnityEventTools.AddPersistentListener(quitButton.onClick, audioManager.PlayButtonClick);
        UnityEventTools.AddPersistentListener(quitButton.onClick, controller.QuitGame);

        EnsureEventSystem();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[BuildMainMenuSceneTool] MainMenuScene UI rebuilt and saved.");
    }

    private static void OpenMainMenuScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path == ScenePath)
        {
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            throw new System.OperationCanceledException("Scene switch cancelled.");
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void EnsureMainCamera()
    {
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.name = "Main Camera";
            return;
        }

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CleanupOldMenuObjects()
    {
        string[] objectNames =
        {
            "Canvas",
            "BackgroundImage",
            "DarkOverlay",
            "TitleText",
            "SubtitleText",
            "StartButton",
            "QuitButton",
            "UIAudioManager"
        };

        for (int i = 0; i < objectNames.Length; i++)
        {
            GameObject obj = GameObject.Find(objectNames[i]);
            if (obj != null && obj.name != "Main Camera")
            {
                Object.DestroyImmediate(obj);
            }
        }
    }

    private static Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void CreateBackground(Transform parent, Sprite backgroundSprite)
    {
        RectTransform rect = CreateUiObject("BackgroundImage", parent);
        Stretch(rect);

        Image image = rect.gameObject.AddComponent<Image>();
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.sprite = backgroundSprite;
        image.color = backgroundSprite != null ? Color.white : new Color(0.015f, 0.025f, 0.045f, 1f);
    }

    private static void CreateDarkOverlay(Transform parent)
    {
        RectTransform rect = CreateUiObject("DarkOverlay", parent);
        Stretch(rect);

        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.45f);
        image.raycastTarget = false;
    }

    private static void CreateTitle(Transform parent)
    {
        TextMeshProUGUI title = CreateTmpText("TitleText", parent, "CaveLight", 76f, Color.white);
        SetAnchoredRect(title.rectTransform, new Vector2(0f, 190f), new Vector2(700f, 120f));
        title.fontStyle = FontStyles.Bold;
        AddShadow(title.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(3f, -3f));
    }

    private static void CreateSubtitle(Transform parent)
    {
        TextMeshProUGUI subtitle = CreateTmpText("SubtitleText", parent, "\u6d1e\u7a74\u4f59\u5149", 30f, new Color(0.86f, 0.88f, 0.9f, 1f));
        SetAnchoredRect(subtitle.rectTransform, new Vector2(0f, 115f), new Vector2(600f, 70f));
        AddShadow(subtitle.gameObject, new Color(0f, 0f, 0f, 0.55f), new Vector2(2f, -2f));
    }

    private static Button CreateMenuButton(string objectName, Transform parent, Vector2 anchoredPosition, string label, Sprite sprite, Color tint)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        SetAnchoredRect(rect, anchoredPosition, new Vector2(300f, 76f));

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = tint;

        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = tint;
        colors.highlightedColor = Color.Lerp(tint, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(tint, Color.black, 0.18f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        button.colors = colors;

        TextMeshProUGUI text = CreateTmpText("Text", rect, label, 32f, Color.white);
        Stretch(text.rectTransform);
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;
        AddShadow(text.gameObject, new Color(0f, 0f, 0f, 0.42f), new Vector2(1.5f, -1.5f));

        return button;
    }

    private static MainMenuController CreateMainMenuController()
    {
        GameObject controllerObject = GameObject.Find("MainMenuController");
        if (controllerObject == null)
        {
            controllerObject = new GameObject("MainMenuController");
        }

        MainMenuController controller = controllerObject.GetComponent<MainMenuController>();
        if (controller == null)
        {
            controller = controllerObject.AddComponent<MainMenuController>();
        }

        return controller;
    }

    private static UIAudioManager CreateUiAudioManager(AudioClip clickClip)
    {
        GameObject audioObject = new GameObject("UIAudioManager", typeof(AudioSource), typeof(UIAudioManager));
        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        UIAudioManager audioManager = audioObject.GetComponent<UIAudioManager>();
        audioManager.buttonClickClip = clickClip;
        return audioManager;
    }

    private static void EnsureEventSystem()
    {
        EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.name = "EventSystem";
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static TextMeshProUGUI CreateTmpText(string objectName, Transform parent, string text, float fontSize, Color color)
    {
        RectTransform rect = CreateUiObject(objectName, parent);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static RectTransform CreateUiObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
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

    private static void SetAnchoredRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void AddShadow(GameObject obj, Color color, Vector2 distance)
    {
        Shadow shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
        shadow.useGraphicAlpha = true;
    }

    private static Sprite LoadFirstBackgroundSprite()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { BackgroundSearchFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Sprite sprite = LoadSpriteWithImportSettings(path);
            if (sprite != null)
            {
                return sprite;
            }
        }

        return null;
    }

    private static Sprite LoadSpriteWithImportSettings(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AudioClip LoadClickClip()
    {
        AudioImporter importer = AssetImporter.GetAtPath(ClickClipPath) as AudioImporter;
        if (importer != null)
        {
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<AudioClip>(ClickClipPath);
    }
}
