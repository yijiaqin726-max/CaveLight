using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DarknessMaskController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string ShaderName = "UI/DarknessMask";

    public Transform player;
    public Camera mainCamera;
    public PlayerEnergyStore energyStore;
    public Image darknessImage;
    public Material darknessMaterialInstance;

    [Header("Mask")]
    public float maxRadius = 0.32f;
    public float minRadius = 0.10f;
    public float softness = 0.08f;
    public float opacity = 1f;

    [Header("Low Energy Flicker")]
    public float lowEnergyThreshold = 0.25f;
    public float flickerAmount = 0.025f;
    public float flickerSpeed = 8f;

    private static bool sceneLoadHookRegistered;
    private Vector2 lastValidCenter = new Vector2(0.5f, 0.5f);
    private bool warnedMissingPlayer;
    private bool warnedMissingEnergyStore;
    private bool warnedMissingImage;
    private bool warnedMissingShader;

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

        if (FindFirstObjectByType<DarknessMaskController>() != null)
        {
            return;
        }

        Canvas canvas = CreateDarknessCanvas();
        Image image = CreateDarknessImage(canvas.transform);
        DarknessMaskController controller = canvas.gameObject.AddComponent<DarknessMaskController>();
        controller.darknessImage = image;
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureMaterialInstance();
    }

    private void Start()
    {
        ResolveReferences();
        EnsureMaterialInstance();
    }

    private void OnValidate()
    {
        maxRadius = Mathf.Max(0.01f, maxRadius);
        minRadius = Mathf.Clamp(minRadius, 0.001f, maxRadius);
        softness = Mathf.Max(0.001f, softness);
        opacity = Mathf.Clamp01(opacity);
        lowEnergyThreshold = Mathf.Clamp01(lowEnergyThreshold);
        flickerAmount = Mathf.Max(0f, flickerAmount);
        flickerSpeed = Mathf.Max(0f, flickerSpeed);
    }

    private void Update()
    {
        ResolveReferences();

        if (darknessImage == null)
        {
            WarnOnce(ref warnedMissingImage, "[DarknessMaskController] Missing darkness Image.");
            return;
        }

        bool shouldHide = IsSceneUiActive("MerchantRoomPanel") || IsSceneUiActive("PauseMenuPanel");
        darknessImage.enabled = !shouldHide;
        if (shouldHide)
        {
            return;
        }

        EnsureMaterialInstance();
        if (darknessMaterialInstance == null)
        {
            return;
        }

        Vector2 center = GetPlayerViewportCenter();
        float percent = GetEnergyPercent();
        float radius = Mathf.Lerp(minRadius, maxRadius, percent);

        if (percent < lowEnergyThreshold)
        {
            radius += Mathf.Sin(Time.time * flickerSpeed) * flickerAmount;
        }

        radius = Mathf.Max(minRadius * 0.7f, radius);
        float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 1.777f;

        darknessMaterialInstance.SetVector("_Center", new Vector4(center.x, center.y, 0f, 0f));
        darknessMaterialInstance.SetFloat("_Radius", radius);
        darknessMaterialInstance.SetFloat("_Softness", softness);
        darknessMaterialInstance.SetFloat("_Opacity", opacity);
        darknessMaterialInstance.SetFloat("_Aspect", aspect);
    }

    private void ResolveReferences()
    {
        if (darknessImage == null)
        {
            darknessImage = GetComponent<Image>();
            if (darknessImage == null)
            {
                darknessImage = GetComponentInChildren<Image>(true);
            }
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
        }

        if (energyStore == null)
        {
            energyStore = FindFirstObjectByType<PlayerEnergyStore>();
        }

        if (player == null)
        {
            if (energyStore != null)
            {
                player = energyStore.transform;
            }
            else
            {
                GameObject playerObject = GameObject.Find("PlayerPlaceholder");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }
        }

        if (darknessImage != null)
        {
            darknessImage.raycastTarget = false;
            darknessImage.color = Color.white;
        }
    }

    private void EnsureMaterialInstance()
    {
        if (darknessImage == null || darknessMaterialInstance != null)
        {
            return;
        }

        Material sourceMaterial = darknessImage.material;
        if (sourceMaterial != null && sourceMaterial.shader != null && sourceMaterial.shader.name == ShaderName)
        {
            darknessMaterialInstance = new Material(sourceMaterial);
        }
        else
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                WarnOnce(ref warnedMissingShader, "[DarknessMaskController] Shader UI/DarknessMask not found. Darkness mask disabled.");
                darknessImage.enabled = false;
                return;
            }

            darknessMaterialInstance = new Material(shader);
        }

        darknessMaterialInstance.name = "DarknessMask_Runtime";
        darknessImage.material = darknessMaterialInstance;
    }

    private Vector2 GetPlayerViewportCenter()
    {
        if (player == null)
        {
            WarnOnce(ref warnedMissingPlayer, "[DarknessMaskController] Missing player reference.");
            return lastValidCenter;
        }

        if (mainCamera == null)
        {
            return lastValidCenter;
        }

        Vector3 viewport = mainCamera.WorldToViewportPoint(player.position);
        if (viewport.z >= 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f)
        {
            lastValidCenter = new Vector2(viewport.x, viewport.y);
        }

        return lastValidCenter;
    }

    private float GetEnergyPercent()
    {
        if (energyStore == null)
        {
            WarnOnce(ref warnedMissingEnergyStore, "[DarknessMaskController] Missing PlayerEnergyStore reference.");
            return 1f;
        }

        return Mathf.Clamp01(energyStore.GetEnergyPercent());
    }

    private static Canvas CreateDarknessCanvas()
    {
        GameObject canvasObject = new GameObject("DarknessMaskCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static Image CreateDarknessImage(Transform parent)
    {
        GameObject imageObject = new GameObject("DarknessMaskImage", typeof(RectTransform), typeof(Image));
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        Shader shader = Shader.Find(ShaderName);
        if (shader != null)
        {
            Material material = new Material(shader);
            material.SetFloat("_Opacity", 1f);
            material.SetFloat("_Radius", 0.25f);
            material.SetFloat("_Softness", 0.08f);
            image.material = material;
        }

        return image;
    }

    private static bool IsSceneUiActive(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject obj = objects[i];
            if (obj == null || obj.name != objectName || !obj.scene.IsValid())
            {
                continue;
            }

            if (obj.activeInHierarchy)
            {
                return true;
            }
        }

        return false;
    }

    private static void WarnOnce(ref bool warned, string message)
    {
        if (warned)
        {
            return;
        }

        warned = true;
        Debug.LogWarning(message);
    }
}
