using UnityEditor;
using UnityEngine;

public class FixPlayerCollider
{
    [MenuItem("CaveLight/Fix Player Collider (Box -> Capsule)")]
    public static void FixPlayerPlaceholderCollider()
    {
        // Find and load the PlayerPlaceholder prefab
        string prefabPath = "Assets/Prefabs/Player/PlayerPlaceholder.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab == null)
        {
            Debug.LogError("[FixPlayerCollider] PlayerPlaceholder prefab not found at " + prefabPath);
            return;
        }

        // Open the prefab for editing
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(prefabPath);

        // Remove existing BoxCollider2D
        BoxCollider2D boxCollider = prefabInstance.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            Object.DestroyImmediate(boxCollider);
            Debug.Log("[FixPlayerCollider] Removed BoxCollider2D");
        }

        // Add CapsuleCollider2D
        CapsuleCollider2D capsule = prefabInstance.AddComponent<CapsuleCollider2D>();
        capsule.direction = CapsuleDirection2D.Vertical;
        capsule.size = new Vector2(0.8f, 1.8f);
        Debug.Log("[FixPlayerCollider] Added CapsuleCollider2D (Vertical, Size: 0.8 x 1.8)");

        // Save the modified prefab
        PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        Debug.Log("[FixPlayerCollider] PlayerPlaceholder prefab updated successfully!");
    }
}
