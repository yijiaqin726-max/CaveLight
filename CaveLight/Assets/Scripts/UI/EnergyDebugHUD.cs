using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class EnergyDebugHUD : MonoBehaviour
{
    private PlayerEnergyStore playerEnergyStore;
    private CaveLevelGenerator caveLevelGenerator;
    private FieldInfo caveAmountField;
    private RectTransform energyBarRoot;
    private RectTransform energyBarFill;
    private Text energyLabel;
    private Text caveText;
    private bool loggedHudBinding;

    void Awake()
    {
        FindReferences();
        EnsureHudUi();
    }

    void Update()
    {
        if (playerEnergyStore == null || caveLevelGenerator == null)
        {
            FindReferences();
        }

        if (energyBarRoot == null || energyBarFill == null || energyLabel == null || caveText == null)
        {
            EnsureHudUi();
        }

        LogHudBindingOnce();
        UpdateEnergyBar();
        UpdateCaveText();
    }

    private void FindReferences()
    {
        if (playerEnergyStore == null)
        {
            playerEnergyStore = Object.FindFirstObjectByType<PlayerEnergyStore>();
        }

        if (caveLevelGenerator == null)
        {
            caveLevelGenerator = Object.FindFirstObjectByType<CaveLevelGenerator>();
            caveAmountField = null;
        }

        if (caveLevelGenerator != null && caveAmountField == null)
        {
            caveAmountField = typeof(CaveLevelGenerator).GetField("caveAmount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
    }

    private void EnsureHudUi()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
        }

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

        RectTransform hudPanel = GetOrCreateRect("HUDPanel", canvas.transform);
        Stretch(hudPanel);
        hudPanel.gameObject.SetActive(true);

        RectTransform energyRoot = GetOrCreateRect("EnergyBarRoot", hudPanel);
        energyRoot.anchorMin = new Vector2(0f, 1f);
        energyRoot.anchorMax = new Vector2(1f, 1f);
        energyRoot.pivot = new Vector2(0.5f, 1f);
        energyRoot.offsetMin = new Vector2(24f, energyRoot.offsetMin.y);
        energyRoot.offsetMax = new Vector2(-24f, energyRoot.offsetMax.y);
        energyRoot.anchoredPosition = new Vector2(0f, -24f);
        energyRoot.sizeDelta = new Vector2(-48f, 32f);
        energyBarRoot = energyRoot;

        RectTransform background = GetOrCreateRect("EnergyBarBackground", energyRoot);
        Stretch(background);
        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = background.gameObject.AddComponent<Image>();
        }
        backgroundImage.color = new Color(0f, 0f, 0f, 0.45f);

        RectTransform fill = GetOrCreateRect("EnergyBarFill", energyRoot);
        fill.anchorMin = new Vector2(0f, 0f);
        fill.anchorMax = new Vector2(1f, 1f);
        fill.pivot = new Vector2(0f, 0.5f);
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;

        Image fillImage = fill.GetComponent<Image>();
        if (fillImage == null)
        {
            fillImage = fill.gameObject.AddComponent<Image>();
        }
        fillImage.color = new Color(1f, 0.827f, 0.239f, 1f);
        energyBarFill = fill;

        RectTransform labelRect = GetOrCreateRect("EnergyLabel", energyRoot);
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(0f, 0.5f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.anchoredPosition = new Vector2(16f, 0f);
        labelRect.sizeDelta = new Vector2(150f, 32f);

        energyLabel = labelRect.GetComponent<Text>();
        if (energyLabel == null)
        {
            energyLabel = labelRect.gameObject.AddComponent<Text>();
        }
        energyLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        energyLabel.fontSize = 24;
        energyLabel.fontStyle = FontStyle.Bold;
        energyLabel.alignment = TextAnchor.MiddleLeft;
        energyLabel.color = Color.white;
        energyLabel.text = "ENERGY";
        energyLabel.raycastTarget = false;

        RectTransform caveRect = GetOrCreateRect("CaveText", hudPanel);
        caveRect.anchorMin = new Vector2(1f, 1f);
        caveRect.anchorMax = new Vector2(1f, 1f);
        caveRect.pivot = new Vector2(1f, 1f);
        caveRect.anchoredPosition = new Vector2(-32f, -70f);
        caveRect.sizeDelta = new Vector2(220f, 44f);

        caveText = caveRect.GetComponent<Text>();
        if (caveText == null)
        {
            caveText = caveRect.gameObject.AddComponent<Text>();
        }
        caveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        caveText.fontSize = 32;
        caveText.fontStyle = FontStyle.Bold;
        caveText.alignment = TextAnchor.MiddleRight;
        caveText.color = Color.white;
        caveText.raycastTarget = false;

        HideLegacyHudText(hudPanel);
    }

    private void UpdateEnergyBar()
    {
        if (energyBarFill == null)
        {
            return;
        }

        float percent = 0f;
        if (playerEnergyStore != null && playerEnergyStore.maxEnergy > 0f)
        {
            percent = Mathf.Clamp01(playerEnergyStore.currentEnergy / playerEnergyStore.maxEnergy);
        }

        energyBarFill.anchorMin = new Vector2(0f, 0f);
        energyBarFill.anchorMax = new Vector2(percent, 1f);
        energyBarFill.offsetMin = Vector2.zero;
        energyBarFill.offsetMax = Vector2.zero;

        Image fillImage = energyBarFill.GetComponent<Image>();
        if (fillImage != null)
        {
            fillImage.color = new Color(1f, 0.827f, 0.239f, 1f);
        }
    }

    private void LogHudBindingOnce()
    {
        if (loggedHudBinding)
        {
            return;
        }

        loggedHudBinding = true;
        Debug.Log($"[HUD] PlayerEnergyStore found = {playerEnergyStore != null}");
        Debug.Log($"[HUD] EnergyBarFill assigned = {energyBarFill != null}");
        Debug.Log($"[HUD] CaveText assigned = {caveText != null}");
        Debug.Log($"[HUD VERIFY] EnergyBarRoot stretch full width = {IsEnergyRootFullWidth()}");
        Debug.Log($"[HUD VERIFY] EnergyLabel text = {(energyLabel != null ? energyLabel.text : "null")}");
        Debug.Log($"[HUD VERIFY] CaveText anchored top right below bar = {IsCaveTextBelowBar()}");
        Debug.Log($"[HUD VERIFY] KillText hidden = {IsKillTextHidden()}");
    }

    private void UpdateCaveText()
    {
        if (caveText == null)
        {
            return;
        }

        caveText.text = caveLevelGenerator != null && caveAmountField != null
            ? $"CAVE {caveAmountField.GetValue(caveLevelGenerator)}"
            : "CAVE --";
    }

    private static RectTransform GetOrCreateRect(string objectName, Transform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out RectTransform existingRect))
        {
            return existingRect;
        }

        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void HideLegacyHudText(RectTransform hudPanel)
    {
        HideChildByName(hudPanel, "KillText");
        HideChildByName(hudPanel, "KillCountText");
        HideChildByName(hudPanel, "EnergyText");
    }

    private static void HideChildByName(Transform parent, string objectName)
    {
        Transform child = FindChildRecursive(parent, objectName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    private bool IsEnergyRootFullWidth()
    {
        return energyBarRoot != null
            && Mathf.Approximately(energyBarRoot.anchorMin.x, 0f)
            && Mathf.Approximately(energyBarRoot.anchorMax.x, 1f)
            && Mathf.Approximately(energyBarRoot.offsetMin.x, 24f)
            && Mathf.Approximately(energyBarRoot.offsetMax.x, -24f);
    }

    private bool IsCaveTextBelowBar()
    {
        return caveText != null
            && caveText.rectTransform.anchorMin == Vector2.one
            && caveText.rectTransform.anchorMax == Vector2.one
            && caveText.rectTransform.anchoredPosition.x <= -32f
            && caveText.rectTransform.anchoredPosition.y <= -70f;
    }

    private bool IsKillTextHidden()
    {
        Transform hudPanel = energyBarRoot != null ? energyBarRoot.parent : null;
        Transform killText = FindChildRecursive(hudPanel, "KillText");
        Transform killCountText = FindChildRecursive(hudPanel, "KillCountText");
        return (killText == null || !killText.gameObject.activeSelf)
            && (killCountText == null || !killCountText.gameObject.activeSelf);
    }

    private static Transform FindChildRecursive(Transform parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == objectName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, objectName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
