using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

public static class ApplyMainMenuChineseFont
{
    private const string FontPath = "Assets/Art/UI/Fonts/Chinese/NotoSansSC-Regular.otf";
    private const string FontAssetPath = "Assets/Art/UI/Fonts/Chinese/NotoSansSC-Regular SDF.asset";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";

    [MenuItem("CaveLight/Apply Main Menu Chinese Font")]
    public static void RunFromMenu()
    {
        Apply(false);
    }

    public static void Run()
    {
        Apply(true);
    }

    private static void Apply(bool exitWhenDone)
    {
        AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceUpdate);

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (fontAsset == null)
        {
            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[ApplyMainMenuChineseFont] Cannot load source font at {FontPath}");
                if (exitWhenDone)
                {
                    EditorApplication.Exit(1);
                }

                return;
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);

            fontAsset.name = "NotoSansSC-Regular SDF";
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.SaveAssets();
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        EditorUtility.SetDirty(fontAsset);

        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        if (!scene.IsValid())
        {
            Debug.LogError($"[ApplyMainMenuChineseFont] Cannot open scene {MainMenuScenePath}");
            if (exitWhenDone)
            {
                EditorApplication.Exit(1);
            }

            return;
        }

        AssignFont("SubtitleText", fontAsset);
        AssignFont("StartButton/Text", fontAsset);
        AssignFont("QuitButton/Text", fontAsset);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ApplyMainMenuChineseFont] Applied {FontAssetPath} to MainMenuScene Chinese text.");
        if (exitWhenDone)
        {
            EditorApplication.Exit(0);
        }
    }

    private static void AssignFont(string objectPath, TMP_FontAsset fontAsset)
    {
        GameObject target = GameObject.Find(objectPath);
        if (target == null)
        {
            Debug.LogWarning($"[ApplyMainMenuChineseFont] Missing text object: {objectPath}");
            return;
        }

        TMP_Text text = target.GetComponent<TMP_Text>();
        if (text == null)
        {
            Debug.LogWarning($"[ApplyMainMenuChineseFont] Missing TMP_Text on: {objectPath}");
            return;
        }

        text.font = fontAsset;
        text.fontSharedMaterial = fontAsset.material;
        EditorUtility.SetDirty(text);
    }
}
