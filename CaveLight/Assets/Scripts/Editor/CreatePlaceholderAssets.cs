using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using System.IO;

public class CreatePlaceholderAssets
{
    private const string PLACEHOLDER_SPRITE_PATH = "Assets/Art/Placeholder";
    private const string TILES_PATH = "Assets/Tiles";
    private const string PREFABS_PATH = "Assets/Prefabs";
    private const int SPRITE_PPU = 32;

    [MenuItem("CaveLight/Create Placeholder Assets")]
    public static void CreateAllPlaceholderAssets()
    {
        Debug.Log("[CaveLight] Starting placeholder assets generation...");

        // Create directories if they don't exist
        CreateDirectoryIfNotExists(PLACEHOLDER_SPRITE_PATH);
        CreateDirectoryIfNotExists(TILES_PATH);
        CreateDirectoryIfNotExists(PREFABS_PATH);
        CreateDirectoryIfNotExists($"{PREFABS_PATH}/Player");
        CreateDirectoryIfNotExists($"{PREFABS_PATH}/Monster");
        CreateDirectoryIfNotExists($"{PREFABS_PATH}/Energy");
        CreateDirectoryIfNotExists($"{PREFABS_PATH}/Level");

        // Generate sprites
        GenerateSprites();

        // Generate tiles
        GenerateTiles();

        // Generate prefabs
        GeneratePrefabs();

        // Generate documentation
        GenerateDocumentation();

        AssetDatabase.Refresh();
        Debug.Log("[CaveLight] Placeholder assets generation completed!");
    }

    private static void CreateDirectoryIfNotExists(string path)
    {
        string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }

    private static void GenerateSprites()
    {
        Debug.Log("[CaveLight] Generating placeholder sprites...");

        // Player (32x64, yellow/orange)
        GenerateSprite("placeholder_player.png", 32, 64, new Color(1f, 0.8f, 0f, 1f));

        // Monster (32x32, purple/red)
        GenerateSprite("placeholder_monster.png", 32, 32, new Color(0.8f, 0.2f, 0.6f, 1f));

        // Energy (16x16, light yellow)
        GenerateSprite("placeholder_energy.png", 16, 16, new Color(1f, 1f, 0.5f, 1f));

        // Cave Energy Node (32x32, bright yellow)
        GenerateSprite("placeholder_cave_energy_node.png", 32, 32, new Color(1f, 1f, 0f, 1f));

        // Exit (32x64, green)
        GenerateSprite("placeholder_exit.png", 32, 64, new Color(0.2f, 0.8f, 0.2f, 1f));

        // Wall Tile (32x32, dark gray)
        GenerateSprite("placeholder_wall_tile.png", 32, 32, new Color(0.3f, 0.3f, 0.3f, 1f));

        // Ground Tile (32x32, brownish gray)
        GenerateSprite("placeholder_ground_tile.png", 32, 32, new Color(0.6f, 0.5f, 0.4f, 1f));
    }

    private static void GenerateSprite(string filename, int width, int height, Color color)
    {
        string path = $"{PLACEHOLDER_SPRITE_PATH}/{filename}";
        string fullPath = Path.Combine(Application.dataPath, path.Replace("Assets/", ""));

        // Create texture
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();

        // Save as PNG
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);
        Object.DestroyImmediate(texture);

        // Configure import settings
        AssetDatabase.Refresh();
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = SPRITE_PPU;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        Debug.Log($"[CaveLight] Generated sprite: {path}");
    }

    private static void GenerateTiles()
    {
        Debug.Log("[CaveLight] Generating placeholder tiles...");

        // Load sprites
        Sprite wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PLACEHOLDER_SPRITE_PATH}/placeholder_wall_tile.png");
        Sprite groundSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PLACEHOLDER_SPRITE_PATH}/placeholder_ground_tile.png");

        if (wallSprite == null || groundSprite == null)
        {
            Debug.LogError("[CaveLight] Failed to load sprites for tiles!");
            return;
        }

        // Create Wall Tile
        Tile wallTile = ScriptableObject.CreateInstance<Tile>();
        wallTile.sprite = wallSprite;
        AssetDatabase.CreateAsset(wallTile, $"{TILES_PATH}/PlaceholderWallTile.asset");

        // Create Ground Tile
        Tile groundTile = ScriptableObject.CreateInstance<Tile>();
        groundTile.sprite = groundSprite;
        AssetDatabase.CreateAsset(groundTile, $"{TILES_PATH}/PlaceholderGroundTile.asset");

        Debug.Log("[CaveLight] Generated tiles: PlaceholderWallTile.asset, PlaceholderGroundTile.asset");
    }

    private static void GeneratePrefabs()
    {
        Debug.Log("[CaveLight] Generating placeholder prefabs...");

        // Player Placeholder
        CreatePlayerPrefab();

        // Monster Placeholder
        CreateMonsterPrefab();

        // Energy Placeholder
        CreateEnergyPrefab();

        // Cave Energy Node Placeholder
        CreateCaveEnergyNodePrefab();

        // Exit Placeholder
        CreateExitPrefab();
    }

    private static void CreatePlayerPrefab()
    {
        GameObject playerGO = new GameObject("PlayerPlaceholder");
        playerGO.transform.localScale = Vector3.one;

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = playerGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PLACEHOLDER_SPRITE_PATH}/placeholder_player.png");

        // Add Rigidbody2D
        Rigidbody2D rb = playerGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Add BoxCollider2D
        playerGO.AddComponent<BoxCollider2D>();

        // Save as prefab
        SavePrefab(playerGO, $"{PREFABS_PATH}/Player/PlayerPlaceholder.prefab");
    }

    private static void CreateMonsterPrefab()
    {
        GameObject monsterGO = new GameObject("MonsterPlaceholder");

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = monsterGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PLACEHOLDER_SPRITE_PATH}/placeholder_monster.png");

        // Add Rigidbody2D (Kinematic)
        Rigidbody2D rb = monsterGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Add BoxCollider2D
        monsterGO.AddComponent<BoxCollider2D>();

        // Save as prefab
        SavePrefab(monsterGO, $"{PREFABS_PATH}/Monster/MonsterPlaceholder.prefab");
    }

    private static void CreateEnergyPrefab()
    {
        GameObject energyGO = new GameObject("EnergyPlaceholder");

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = energyGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PLACEHOLDER_SPRITE_PATH}/placeholder_energy.png");

        // Add Rigidbody2D
        Rigidbody2D rb = energyGO.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;

        // Add CircleCollider2D (Is Trigger)
        CircleCollider2D circleCollider = energyGO.AddComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        // Save as prefab
        SavePrefab(energyGO, $"{PREFABS_PATH}/Energy/EnergyPlaceholder.prefab");
    }

    private static void CreateCaveEnergyNodePrefab()
    {
        GameObject nodeGO = new GameObject("CaveEnergyNodePlaceholder");

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = nodeGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PLACEHOLDER_SPRITE_PATH}/placeholder_cave_energy_node.png");

        // Add BoxCollider2D
        nodeGO.AddComponent<BoxCollider2D>();

        // Save as prefab
        SavePrefab(nodeGO, $"{PREFABS_PATH}/Energy/CaveEnergyNodePlaceholder.prefab");
    }

    private static void CreateExitPrefab()
    {
        GameObject exitGO = new GameObject("ExitPlaceholder");

        // Add SpriteRenderer
        SpriteRenderer spriteRenderer = exitGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{PLACEHOLDER_SPRITE_PATH}/placeholder_exit.png");

        // Add BoxCollider2D (Is Trigger)
        BoxCollider2D boxCollider = exitGO.AddComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;

        // Save as prefab
        SavePrefab(exitGO, $"{PREFABS_PATH}/Level/ExitPlaceholder.prefab");
    }

    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[CaveLight] Generated prefab: {path}");
    }

    private static void GenerateDocumentation()
    {
        Debug.Log("[CaveLight] Generating documentation...");

        string docPath = "Assets/Docs/PLACEHOLDER_ASSETS.md";
        string content = @"# 占位符资源说明

此文档说明 CaveLight 项目的占位符（Placeholder）资源规格及用途。

## 概述

这些占位符资源仅用于灰盒快速验证玩法机制。后续正式美术可直接替换 Sprite，无需修改 Prefab 名字或项目结构。

## 资源规格

### Tile（瓦片）
- 单格尺寸：1 x 1 Unity Unit
- 像素尺寸：32 x 32 pixels
- 类型：PlaceholderWallTile、PlaceholderGroundTile

### 角色与物体

| 资源 | 尺寸 | 像素尺寸 | 文件名 |
|------|------|---------|--------|
| Player | 1 x 2 Unity Unit | 32 x 64 pixels | placeholder_player.png |
| Monster | 1 x 1 Unity Unit | 32 x 32 pixels | placeholder_monster.png |
| Energy | 0.3 x 0.3 Unity Unit | 16 x 16 pixels | placeholder_energy.png |
| CaveEnergyNode | 1 x 1 Unity Unit | 32 x 32 pixels | placeholder_cave_energy_node.png |
| Exit | 1 x 2 Unity Unit | 32 x 64 pixels | placeholder_exit.png |

### Sprite 导入设置
- Texture Type：Sprite
- Pixels Per Unit：32
- Filter Mode：Point

## Prefab 清单

### Player
- **PlayerPlaceholder**：玩家占位预制件
  - Components：SpriteRenderer、Rigidbody2D (Dynamic, Freeze Rotation Z)、BoxCollider2D

### Monster
- **MonsterPlaceholder**：怪物占位预制件
  - Components：SpriteRenderer、Rigidbody2D (Kinematic)、BoxCollider2D

### Energy
- **EnergyPlaceholder**：能源掉落物占位预制件
  - Components：SpriteRenderer、Rigidbody2D (Dynamic)、CircleCollider2D (Is Trigger)

- **CaveEnergyNodePlaceholder**：洞穴能源节点占位预制件
  - Components：SpriteRenderer、BoxCollider2D

### Level
- **ExitPlaceholder**：出口占位预制件
  - Components：SpriteRenderer、BoxCollider2D (Is Trigger)

## Tile 清单

- **PlaceholderWallTile**：墙壁瓦片（深灰色）
- **PlaceholderGroundTile**：地面瓦片（灰褐色）

## 美术替换指南

1. 用原始 PNG 文件替换 `Assets/Art/Placeholder/` 中的占位符图片
2. 确保 Sprite 导入设置保持一致（PPU = 32）
3. 无需修改任何 Prefab 或脚本
4. Tile 资源会自动使用新的 Sprite

## 注意事项

- 所有物体尺寸以 1 Unity Unit = 32 pixels 标准设计
- Collider 尺寸已根据 Sprite 尺寸配置，无需手动调整
- 占位符使用纯色填充，方便快速识别，正式美术需要替换为详细的像素艺术
";

        File.WriteAllText(Path.Combine(Application.dataPath, docPath.Replace("Assets/", "")), content);
        Debug.Log($"[CaveLight] Generated documentation: {docPath}");
    }
}
