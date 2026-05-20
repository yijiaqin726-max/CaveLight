using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class CaveLevelGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap wallTilemap;
    public Tilemap groundTilemap;
    public Tilemap decorationTilemap;

    [Header("Tiles")]
    public Tile wallTile;
    public Tile groundTile;

    [Header("Map Size")]
    public int width = 24;
    public int height = 12;

    [Header("Level Design")]
    public int maxJumpHeight = 2;
    public int minPlatformWidth = 4;
    public int maxPlatformWidth = 7;

    [Header("Macro Tile Map")]
    public int macroRows = 5;
    public int macroCols = 7;
    public int macroCellWidth = 8;
    public int macroCellHeight = 5;
    public float platformReplaceChance = 0.4f;

    [Header("Player")]
    public Transform player;
    public Vector2Int playerSpawn = new Vector2Int(3, 3);

    [Header("Exit & Progression")]
    public GameObject exitPrefab;
    public Transform levelObjectsRoot;
    private int caveAmount = 0;
    private GameObject currentExit;

    [Header("Merchant Room")]
    public int cavesBeforeMerchant = 3;
    public int cavesClearedSinceMerchant = 0;
    public int totalCavesCleared = 0;
    public GameObject merchantRoomPanel;
    public Button enterNextCaveButton;
    public MerchantRoomController merchantRoomController;
    public bool inMerchantRoom = false;

    [Header("Energy Nodes")]
    public GameObject caveEnergyNodePrefab;
    public int minCaveEnergyNodes = 2;
    public int maxCaveEnergyNodes = 4;

    [Header("Monsters")]
    public GameObject monsterPrefab;
    public int minMonsters = 1;
    public int maxMonsters = 3;
    public int killAmount = 0;

    [Header("Debug")]
    public bool debugDrawSolidCells = true;

    private const int MaxMacroGenerationAttempts = 30;

    private readonly int[] entranceTypes = { 2, 3, 14 };
    private readonly int[] entranceSupportTypes = { 5, 10 };
    private readonly int[] exitTypes = { 1, 2, 14 };
    private readonly int[] exitSupportTypes = { 5, 11 };
    private readonly int[] firstColumnTypes = { 5, 6, 10, 12 };
    private readonly int[] seventhColumnTypes = { 4, 5, 11, 13 };
    private readonly int[] firstRowTypes = { 5, 8, 10, 11 };
    private readonly int[] fifthRowTypes = { 2, 5, 12, 13 };
    private readonly int[] platformTypes = { 1, 2, 3, 7, 8, 9 };

    private MacroTileCell[,] macroCells;
    private MacroConnection[,] macroConnections;
    private int[,] macroGrid;
    private Vector2Int entranceMacroCell;
    private Vector2Int exitMacroCell;
    private List<Vector2Int> reachableMacroCells = new List<Vector2Int>();
    private List<Vector2Int> mainMacroPath = new List<Vector2Int>();
    private int[] mainPathPlatformY;
    private bool[,] solidMap;
    private readonly List<StablePlatform> stableMainPlatforms = new List<StablePlatform>();
    private readonly HashSet<int> occupiedStablePlatformIndices = new HashSet<int>();
    private Vector3 currentSpawnWorldPosition;
    private bool hasCurrentLevelSpawnPosition;
    private PhysicsMaterial2D runtimeNoFrictionMaterial;
    private const float FallRespawnY = -5f;

    private struct MacroTileCell
    {
        public int row;
        public int col;
        public int typeId;
        public bool isEntrance;
        public bool isExit;

        public MacroTileCell(int row, int col, int typeId, bool isEntrance, bool isExit)
        {
            this.row = row;
            this.col = col;
            this.typeId = typeId;
            this.isEntrance = isEntrance;
            this.isExit = isExit;
        }
    }

    private struct MacroConnection
    {
        public bool openLeft;
        public bool openRight;
        public bool openUp;
        public bool openDown;
    }

    private struct StablePlatform
    {
        public int xStart;
        public int y;
        public int width;

        public int XEnd => xStart + width - 1;
        public int CenterX => xStart + width / 2;

        public StablePlatform(int xStart, int y, int width)
        {
            this.xStart = xStart;
            this.y = y;
            this.width = width;
        }
    }

    void Start()
    {
        caveAmount = 1;
        cavesClearedSinceMerchant = 0;
        totalCavesCleared = 0;
        inMerchantRoom = false;

        if (merchantRoomPanel != null)
        {
            merchantRoomPanel.SetActive(false);
        }

        if (enterNextCaveButton != null)
        {
            enterNextCaveButton.onClick.RemoveListener(ExitMerchantRoom);
            enterNextCaveButton.onClick.AddListener(ExitMerchantRoom);
        }

        if (merchantRoomController == null)
        {
            merchantRoomController = FindFirstObjectByType<MerchantRoomController>();
        }

        GenerateLevel();
        Debug.Log("Enter cave: 1");
    }

    void OnValidate()
    {
        cavesBeforeMerchant = Mathf.Max(1, cavesBeforeMerchant);
        cavesClearedSinceMerchant = Mathf.Max(0, cavesClearedSinceMerchant);
        totalCavesCleared = Mathf.Max(0, totalCavesCleared);
        minCaveEnergyNodes = Mathf.Max(0, minCaveEnergyNodes);
        maxCaveEnergyNodes = Mathf.Max(minCaveEnergyNodes, maxCaveEnergyNodes);
        minMonsters = Mathf.Max(0, minMonsters);
        maxMonsters = Mathf.Max(minMonsters, maxMonsters);
        macroRows = 5;
        macroCols = 7;
        macroCellWidth = 8;
        macroCellHeight = 5;
        platformReplaceChance = Mathf.Clamp01(platformReplaceChance);
    }

    void Update()
    {
        if (!inMerchantRoom && Input.GetKeyDown(KeyCode.R))
        {
            GenerateLevel();
            Debug.Log("[CaveLevelGenerator] Regenerated map on R key.");
        }

        if (!inMerchantRoom && player != null && hasCurrentLevelSpawnPosition && player.position.y < FallRespawnY)
        {
            RespawnPlayerAtEntrance();
        }
    }

    public void GoToNextCave()
    {
        CompleteCurrentCave();
    }

    public void CompleteCurrentCave()
    {
        if (inMerchantRoom)
        {
            return;
        }

        totalCavesCleared += 1;
        cavesClearedSinceMerchant += 1;
        Debug.Log($"[CaveLevelGenerator] Cave cleared. totalCavesCleared={totalCavesCleared}, cavesClearedSinceMerchant={cavesClearedSinceMerchant}");

        if (cavesClearedSinceMerchant >= cavesBeforeMerchant)
        {
            Debug.Log($"[CaveLevelGenerator] cavesClearedSinceMerchant reached {cavesBeforeMerchant}. Enter merchant room.");
            EnterMerchantRoom();
            return;
        }

        caveAmount += 1;
        Debug.Log("Enter cave: " + caveAmount);
        GenerateLevel();
    }

    public void EnterMerchantRoom()
    {
        inMerchantRoom = true;

        if (merchantRoomPanel != null)
        {
            merchantRoomPanel.SetActive(true);
        }

        if (merchantRoomController == null)
        {
            merchantRoomController = FindFirstObjectByType<MerchantRoomController>();
        }

        if (merchantRoomController != null)
        {
            merchantRoomController.OnEnterMerchantRoom();
        }

        EnsureLevelObjectsRoot();
        ClearDynamicLevelObjects();
        ClearAllLevelTilemaps();

        if (player != null)
        {
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                prb.linearVelocity = Vector2.zero;
                prb.angularVelocity = 0f;
            }

            player.gameObject.SetActive(false);
        }

        Debug.Log("Enter merchant room.");
    }

    public void ExitMerchantRoom()
    {
        inMerchantRoom = false;
        cavesClearedSinceMerchant = 0;

        if (merchantRoomPanel != null)
        {
            merchantRoomPanel.SetActive(false);
        }

        if (player != null)
        {
            player.gameObject.SetActive(true);
        }

        caveAmount += 1;
        GenerateLevel();
        Debug.Log("Leave merchant room. Generate new cave.");
        Debug.Log("Enter cave: " + caveAmount);
    }

    public void GenerateLevel()
    {
        if (inMerchantRoom)
        {
            return;
        }

        if (wallTilemap == null || groundTilemap == null || wallTile == null || groundTile == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Missing references. Please assign Tilemaps and Tiles in the Inspector.");
            return;
        }

        EnsureTilemapColliderSetup();
        EnsurePlayerColliderSetup();
        EnsureLevelObjectsRoot();
        ClearAllLevelTilemaps();
        ResetGeneratedMapData();
        ClearDynamicLevelObjects();

        GenerateStableGameJamMap();
        RefreshWallTilemapCollider();

        Debug.Log("[CaveLevelGenerator] Stable GameJam map generation completed.");
    }

    private void GenerateStableGameJamMap()
    {
        macroRows = 5;
        macroCols = 7;
        macroCellWidth = 8;
        macroCellHeight = 5;
        width = 56;
        height = 18;

        stableMainPlatforms.Clear();
        occupiedStablePlatformIndices.Clear();
        BuildStableMainPlatforms();
        PaintStableCaveBackground();
        PaintStableMainPlatforms();
        PaintStableDecorations();

        StablePlatform firstPlatform = stableMainPlatforms[0];
        StablePlatform lastPlatform = stableMainPlatforms[stableMainPlatforms.Count - 1];
        Vector3Int spawnCell = new Vector3Int(firstPlatform.xStart + 2, firstPlatform.y + 2, 0);
        Vector3Int exitCell = new Vector3Int(lastPlatform.CenterX, lastPlatform.y + 2, 0);

        PrepareSpawnArea(spawnCell);
        PrepareExitArea(exitCell);

        PlacePlayer(spawnCell, new Vector3Int(spawnCell.x, spawnCell.y - 2, 0));
        SpawnCaveEnergyNodes(exitCell);
        SpawnMonsters();
        SpawnExitOnStablePlatform(lastPlatform);

        int wallTileCount = CountWallTiles();
        Debug.Log($"[CaveLevelGenerator] Stable platforms={stableMainPlatforms.Count}, WallTilemap tile count={wallTileCount}.");
        LogStablePlatforms();
    }

    private void BuildStableMainPlatforms()
    {
        int[] safeHeights = { 3, 5, 7 };
        int platformCount = Random.Range(6, 8);
        int currentY = safeHeights[Random.Range(0, safeHeights.Length)];
        StablePlatform previous = default;

        for (int i = 0; i < platformCount; i++)
        {
            int platformWidth = Random.Range(6, 11);
            int targetX;

            if (i == 0)
            {
                targetX = 2;
            }
            else if (i == platformCount - 1)
            {
                targetX = width - 12;
                platformWidth = 9;
            }
            else
            {
                float t = i / (float)(platformCount - 1);
                targetX = Mathf.RoundToInt(Mathf.Lerp(2, width - 12, t)) + Random.Range(-1, 2);
            }

            if (i > 0)
            {
                int maxReachableStart = previous.XEnd + 4;
                int minForwardStart = previous.xStart + 4;
                targetX = Mathf.Clamp(targetX, minForwardStart, maxReachableStart);

                List<int> validHeights = new List<int>();
                for (int h = 0; h < safeHeights.Length; h++)
                {
                    if (Mathf.Abs(safeHeights[h] - currentY) <= 2)
                    {
                        validHeights.Add(safeHeights[h]);
                    }
                }

                currentY = validHeights[Random.Range(0, validHeights.Count)];
            }

            if (targetX + platformWidth > width - 2)
            {
                targetX = width - 2 - platformWidth;
            }

            previous = new StablePlatform(targetX, currentY, platformWidth);
            stableMainPlatforms.Add(previous);
        }
    }

    private void PaintStableCaveBackground()
    {
        for (int x = 0; x < width; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, 0, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(x, height - 1, 0), wallTile);

            if (groundTilemap != null)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            wallTilemap.SetTile(new Vector3Int(0, y, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(width - 1, y, 0), wallTile);
        }

        for (int x = 3; x < width - 3; x += Random.Range(4, 8))
        {
            int ceilingDepth = Random.Range(1, 3);
            for (int y = 0; y < ceilingDepth; y++)
            {
                wallTilemap.SetTile(new Vector3Int(x, height - 2 - y, 0), wallTile);
            }
        }
    }

    private void PaintStableMainPlatforms()
    {
        for (int i = 0; i < stableMainPlatforms.Count; i++)
        {
            StablePlatform platform = stableMainPlatforms[i];
            DrawStablePlatform(platform.xStart, platform.XEnd, platform.y);

            if (i < stableMainPlatforms.Count - 1)
            {
                StablePlatform next = stableMainPlatforms[i + 1];
                if (next.y > platform.y)
                {
                    DrawStablePlatform(platform.XEnd - 1, platform.XEnd + 2, platform.y + 1);
                    DrawStablePlatform(platform.XEnd + 1, platform.XEnd + 4, platform.y + 2);
                }
            }
        }
    }

    private void DrawStablePlatform(int xStart, int xEnd, int y)
    {
        xStart = Mathf.Clamp(xStart, 1, width - 2);
        xEnd = Mathf.Clamp(xEnd, 1, width - 2);
        for (int x = xStart; x <= xEnd; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
        }
    }

    private void PaintStableDecorations()
    {
        if (decorationTilemap == null)
        {
            return;
        }

        for (int i = 1; i < stableMainPlatforms.Count - 1; i++)
        {
            if (Random.value < 0.55f)
            {
                continue;
            }

            StablePlatform platform = stableMainPlatforms[i];
            int x = Random.Range(platform.xStart + 1, platform.XEnd);
            decorationTilemap.SetTile(new Vector3Int(x, platform.y + 1, 0), groundTile);
        }
    }

    private void SpawnExitOnStablePlatform(StablePlatform platform)
    {
        Vector3Int exitCell = new Vector3Int(platform.CenterX, platform.y + 2, 0);
        PrepareExitArea(exitCell);
        Vector3 exitWorldPos = wallTilemap.GetCellCenterWorld(exitCell);

        if (exitPrefab == null)
        {
            Debug.LogError($"[CaveLevelGenerator] Exit prefab not assigned. Exit spawn failed. exit small cell={exitCell}, exit world position={exitWorldPos}, currentExit is null? {currentExit == null}");
            return;
        }

        currentExit = Instantiate(exitPrefab, exitWorldPos, Quaternion.identity, levelObjectsRoot);
        currentExit.name = "ExitPlaceholder";

        SpriteRenderer spriteRenderer = currentExit.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 20;
            spriteRenderer.color = Color.green;
        }

        BoxCollider2D boxCollider = currentExit.GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = currentExit.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;

        CaveExitTrigger trigger = currentExit.GetComponent<CaveExitTrigger>();
        if (trigger == null)
        {
            trigger = currentExit.AddComponent<CaveExitTrigger>();
        }
        trigger.SetLevelGenerator(this);

        Debug.Log($"[CaveLevelGenerator] Exit spawned on stable platform. cell={exitCell}, world position={exitWorldPos}, currentExit is null? {currentExit == null}");
    }

    private void SpawnCaveEnergyNodes(Vector3Int exitCell)
    {
        if (caveEnergyNodePrefab == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Cave energy node prefab not assigned. Skipping cave energy nodes.");
            return;
        }

        List<int> candidateIndices = new List<int>();
        for (int i = 1; i < stableMainPlatforms.Count - 1; i++)
        {
            candidateIndices.Add(i);
        }

        Shuffle(candidateIndices);
        int targetNodeCount = Mathf.Clamp(Random.Range(minCaveEnergyNodes, maxCaveEnergyNodes + 1), 0, candidateIndices.Count);
        int spawnedCount = 0;

        for (int i = 0; i < candidateIndices.Count && spawnedCount < targetNodeCount; i++)
        {
            StablePlatform platform = stableMainPlatforms[candidateIndices[i]];
            int leftEdgeX = platform.xStart + 1;
            int rightEdgeX = platform.XEnd - 1;
            int x = Random.value < 0.5f ? leftEdgeX : rightEdgeX;
            Vector3Int nodeCell = new Vector3Int(x, platform.y + 2, 0);
            if (Mathf.Abs(nodeCell.x - exitCell.x) < 2)
            {
                continue;
            }

            Vector3 worldPos = wallTilemap.GetCellCenterWorld(nodeCell);
            GameObject node = Instantiate(caveEnergyNodePrefab, worldPos, Quaternion.identity, levelObjectsRoot);
            node.name = "CaveEnergyNodePlaceholder";
            occupiedStablePlatformIndices.Add(candidateIndices[i]);

            if (node.GetComponent<CaveEnergyNode>() == null)
            {
                Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing CaveEnergyNode script.");
            }

            ConfigureSpawnedCaveEnergyNodeVisual(node, nodeCell);
            spawnedCount++;
        }

        Debug.Log($"[CaveLevelGenerator] Spawned {spawnedCount} cave energy nodes on stable main route.");
    }

    private void ConfigureSpawnedCaveEnergyNodeVisual(GameObject node, Vector3Int nodeCell)
    {
        SpriteRenderer spriteRenderer = node.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] Cave energy node at {nodeCell} is missing SpriteRenderer. It may look like an invisible wall.");
        }
        else
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 30;
            spriteRenderer.color = new Color(1f, 0.92f, 0.1f, 1f);

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader != null)
            {
                spriteRenderer.material = new Material(shader);
            }
        }

        BoxCollider2D boxCollider = node.GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = node.AddComponent<BoxCollider2D>();
        }

        boxCollider.isTrigger = false;
        boxCollider.size = new Vector2(0.8f, 0.8f);
        boxCollider.offset = Vector2.zero;

        if (node.GetComponent<CaveEnergyNode>() == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing CaveEnergyNode script.");
        }

        node.transform.localScale = Vector3.one;
        Debug.Log($"[CaveLevelGenerator] Cave energy node generated at cell={nodeCell}, world={node.transform.position}, spriteRendererExists={spriteRenderer != null}, spriteRendererEnabled={(spriteRenderer != null && spriteRenderer.enabled)}, sortingOrder={(spriteRenderer != null ? spriteRenderer.sortingOrder : -1)}, colliderIsTrigger={boxCollider.isTrigger}, colliderSize={boxCollider.size}");
    }

    private void SpawnMonsters()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Monster prefab not assigned. Skipping monsters.");
            return;
        }

        List<int> candidateIndices = new List<int>();
        for (int i = 1; i < stableMainPlatforms.Count - 1; i++)
        {
            if (occupiedStablePlatformIndices.Contains(i))
            {
                continue;
            }

            candidateIndices.Add(i);
        }

        Shuffle(candidateIndices);
        int targetMonsterCount = Mathf.Clamp(Random.Range(minMonsters, maxMonsters + 1), 0, candidateIndices.Count);
        int spawnedCount = 0;

        for (int i = 0; i < candidateIndices.Count && spawnedCount < targetMonsterCount; i++)
        {
            StablePlatform platform = stableMainPlatforms[candidateIndices[i]];
            Vector3Int monsterCell = new Vector3Int(platform.CenterX, platform.y + 1, 0);
            Vector3 worldPos = wallTilemap.GetCellCenterWorld(monsterCell);

            GameObject monster = Instantiate(monsterPrefab, worldPos, Quaternion.identity, levelObjectsRoot);
            monster.name = "MonsterPlaceholder";
            ConfigureSpawnedMonster(monster, monsterCell);

            Debug.Log($"[CaveLevelGenerator] Monster spawned platform index={candidateIndices[i]}, cell={monsterCell}, world pos={worldPos}");
            spawnedCount++;
        }

        Debug.Log($"[CaveLevelGenerator] Spawned {spawnedCount} monsters.");
    }

    private void ConfigureSpawnedMonster(GameObject monster, Vector3Int monsterCell)
    {
        SpriteRenderer spriteRenderer = monster.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingOrder = 25;
            spriteRenderer.color = new Color(0.9f, 0.15f, 1f, 1f);
        }
        else
        {
            Debug.LogWarning($"[CaveLevelGenerator] Spawned monster at {monsterCell} is missing SpriteRenderer.");
        }

        Rigidbody2D rb = monster.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = monster.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        Collider2D[] monsterColliders = monster.GetComponents<Collider2D>();
        if (monsterColliders.Length == 0)
        {
            monsterColliders = new Collider2D[] { monster.AddComponent<BoxCollider2D>() };
        }

        for (int i = 0; i < monsterColliders.Length; i++)
        {
            monsterColliders[i].isTrigger = true;

            BoxCollider2D boxCollider = monsterColliders[i] as BoxCollider2D;
            if (boxCollider != null)
            {
                boxCollider.size = Vector2.one;
                boxCollider.offset = Vector2.zero;
            }
        }

        if (monster.GetComponent<MonsterHealth>() == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] Spawned monster at {monsterCell} is missing MonsterHealth.");
        }

        if (monster.GetComponent<MonsterPatrol>() == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] Spawned monster at {monsterCell} is missing MonsterPatrol.");
        }

        if (monster.GetComponent<MonsterDamageDealer>() == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] Spawned monster at {monsterCell} is missing MonsterDamageDealer.");
        }
    }

    public void AddKillCount(int amount)
    {
        killAmount += Mathf.Max(0, amount);
        Debug.Log($"[CaveLevelGenerator] KillAmount: {killAmount}");
    }

    private int CountWallTiles()
    {
        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (wallTilemap.GetTile(new Vector3Int(x, y, 0)) != null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private void LogStablePlatforms()
    {
        for (int i = 0; i < stableMainPlatforms.Count; i++)
        {
            StablePlatform platform = stableMainPlatforms[i];
            Debug.Log($"[CaveLevelGenerator] Stable platform index={i}, xStart={platform.xStart}, xEnd={platform.XEnd}, y={platform.y}, width={platform.width}");
        }
    }

    private void ClearAllLevelTilemaps()
    {
        if (wallTilemap != null)
        {
            wallTilemap.ClearAllTiles();
            wallTilemap.RefreshAllTiles();
        }

        if (groundTilemap != null)
        {
            groundTilemap.ClearAllTiles();
            groundTilemap.RefreshAllTiles();
        }

        if (decorationTilemap != null)
        {
            decorationTilemap.ClearAllTiles();
            decorationTilemap.RefreshAllTiles();
        }
    }

    private void ResetGeneratedMapData()
    {
        solidMap = null;
        reachableMacroCells.Clear();
        mainMacroPath.Clear();
        mainPathPlatformY = null;
        macroCells = null;
        macroConnections = null;
        macroGrid = null;
    }

    private void ClearDynamicLevelObjects()
    {
        if (levelObjectsRoot != null)
        {
            for (int i = levelObjectsRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = levelObjectsRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
        else if (currentExit != null)
        {
            if (Application.isPlaying)
            {
                Destroy(currentExit);
            }
            else
            {
                DestroyImmediate(currentExit);
            }
        }

        currentExit = null;
    }

    private void EnsureLevelObjectsRoot()
    {
        if (levelObjectsRoot != null)
        {
            return;
        }

        GameObject root = new GameObject("LevelObjectsRoot");
        levelObjectsRoot = root.transform;
    }

    private void EnsureTilemapColliderSetup()
    {
        EnsureWallTilemapColliderSetup();
        RemoveCollisionComponentsFromVisualTilemap(groundTilemap, "GroundTilemap");
        RemoveCollisionComponentsFromVisualTilemap(decorationTilemap, "DecorationTilemap");
    }

    private void EnsureWallTilemapColliderSetup()
    {
        if (wallTilemap == null)
        {
            return;
        }

        GameObject wallObject = wallTilemap.gameObject;
        TilemapCollider2D tilemapCollider = wallObject.GetComponent<TilemapCollider2D>();
        if (tilemapCollider == null)
        {
            tilemapCollider = wallObject.AddComponent<TilemapCollider2D>();
            Debug.Log("[CaveLevelGenerator] Added TilemapCollider2D to WallTilemap.");
        }
        tilemapCollider.sharedMaterial = GetNoFrictionMaterial();
#pragma warning disable 0618
        tilemapCollider.usedByComposite = true;
#pragma warning restore 0618

        Rigidbody2D rb = wallObject.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = wallObject.AddComponent<Rigidbody2D>();
            Debug.Log("[CaveLevelGenerator] Added Rigidbody2D to WallTilemap.");
        }

        rb.bodyType = RigidbodyType2D.Static;

        // CompositeCollider2D is allowed on WallTilemap. Existing composite settings are left intact.
        CompositeCollider2D composite = wallObject.GetComponent<CompositeCollider2D>();
        if (composite == null)
        {
            composite = wallObject.AddComponent<CompositeCollider2D>();
            Debug.Log("[CaveLevelGenerator] Added CompositeCollider2D to WallTilemap.");
        }

        composite.sharedMaterial = GetNoFrictionMaterial();
    }

    private void EnsurePlayerColliderSetup()
    {
        if (player == null)
        {
            return;
        }

        BoxCollider2D boxCollider = player.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        CapsuleCollider2D capsuleCollider = player.GetComponent<CapsuleCollider2D>();
        if (capsuleCollider == null)
        {
            capsuleCollider = player.gameObject.AddComponent<CapsuleCollider2D>();
        }

        capsuleCollider.enabled = true;
        capsuleCollider.isTrigger = false;
        capsuleCollider.direction = CapsuleDirection2D.Vertical;
        capsuleCollider.size = new Vector2(0.8f, 1.8f);
        capsuleCollider.offset = Vector2.zero;
        capsuleCollider.sharedMaterial = GetNoFrictionMaterial();

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.freezeRotation = true;
        }
    }

    private PhysicsMaterial2D GetNoFrictionMaterial()
    {
        if (runtimeNoFrictionMaterial == null)
        {
            runtimeNoFrictionMaterial = new PhysicsMaterial2D("NoFriction2D")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        return runtimeNoFrictionMaterial;
    }

    private void RemoveCollisionComponentsFromVisualTilemap(Tilemap tilemap, string label)
    {
        if (tilemap == null)
        {
            return;
        }

        RemoveComponentIfExists<TilemapCollider2D>(tilemap.gameObject, label);
        RemoveComponentIfExists<CompositeCollider2D>(tilemap.gameObject, label);
        RemoveComponentIfExists<Rigidbody2D>(tilemap.gameObject, label);
    }

    private void RemoveComponentIfExists<T>(GameObject target, string label) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
        {
            return;
        }

        Debug.LogWarning($"[CaveLevelGenerator] Removing {typeof(T).Name} from {label}. Only WallTilemap should have collision.");
        Collider2D collider = component as Collider2D;
        if (collider != null)
        {
            collider.enabled = false;
        }

        if (Application.isPlaying)
        {
            Destroy(component);
        }
        else
        {
            DestroyImmediate(component);
        }
    }

    private void GenerateReachableMacroGrid()
    {
        macroGrid = new int[macroRows, macroCols];
        macroCells = new MacroTileCell[macroRows, macroCols];
        int[] entranceRows = { 2, 3 };
        int[] exitRows = { 1, 2, 3 };
        entranceMacroCell = new Vector2Int(entranceRows[Random.Range(0, entranceRows.Length)], 0);
        exitMacroCell = new Vector2Int(exitRows[Random.Range(0, exitRows.Length)], macroCols - 1);

        FillFixedMacroBorders();

        bool reachable = false;
        for (int attempt = 1; attempt <= MaxMacroGenerationAttempts; attempt++)
        {
            RandomizeMiddleMacroArea();
            BuildReadableMainPath(false);
            BuildMacroCells();
            reachable = TryBuildReachableMacroCellsFromMainPath();

            if (reachable)
            {
                if (attempt > 1)
                {
                    Debug.Log($"[CaveLevelGenerator] Macro grid reachable after retry {attempt - 1}.");
                }
                break;
            }

            Debug.Log($"[CaveLevelGenerator] Macro grid unreachable. Retry {attempt}/{MaxMacroGenerationAttempts}.");
        }

        if (!reachable)
        {
            Debug.LogWarning("[CaveLevelGenerator] Macro grid failed reachability after 30 attempts. Using fallback layout.");
            ApplyFallbackMacroLayout();
            BuildReadableMainPath(true);
            BuildMacroCells();
            TryBuildReachableMacroCellsFromMainPath();
        }
    }

    private void FillFixedMacroBorders()
    {
        for (int row = 0; row < macroRows; row++)
        {
            for (int col = 0; col < macroCols; col++)
            {
                macroGrid[row, col] = 14;
            }
        }

        int entranceType = PickRandomType(entranceTypes);
        if (entranceMacroCell.x == macroRows - 1 && entranceType == 14)
        {
            entranceType = Random.value < 0.5f ? 2 : 3;
        }

        int exitType = PickRandomType(exitTypes);
        if (exitMacroCell.x == macroRows - 1 && exitType == 14)
        {
            exitType = Random.value < 0.5f ? 1 : 2;
        }

        for (int row = 0; row < macroRows; row++)
        {
            macroGrid[row, 0] = row == entranceMacroCell.x ? entranceType : PickRandomType(firstColumnTypes);
            macroGrid[row, macroCols - 1] = row == exitMacroCell.x ? exitType : PickRandomType(seventhColumnTypes);
        }

        for (int col = 1; col < macroCols - 1; col++)
        {
            macroGrid[0, col] = PickRandomType(firstRowTypes);
            macroGrid[macroRows - 1, col] = PickRandomType(fifthRowTypes);
        }

        if (entranceType == 14 && entranceMacroCell.x + 1 < macroRows)
        {
            macroGrid[entranceMacroCell.x + 1, 0] = PickRandomType(entranceSupportTypes);
        }

        if (exitType == 14 && exitMacroCell.x + 1 < macroRows)
        {
            macroGrid[exitMacroCell.x + 1, macroCols - 1] = PickRandomType(exitSupportTypes);
        }
    }

    private void RandomizeMiddleMacroArea()
    {
        for (int row = 1; row < macroRows - 1; row++)
        {
            for (int col = 1; col < macroCols - 1; col++)
            {
                macroGrid[row, col] = Random.value < platformReplaceChance ? PickRandomType(platformTypes) : 14;
            }
        }
    }

    private void ApplyFallbackMacroLayout()
    {
        for (int row = 1; row < macroRows - 1; row++)
        {
            for (int col = 1; col < macroCols - 1; col++)
            {
                macroGrid[row, col] = 14;
            }
        }

        int rowStep = entranceMacroCell.x <= exitMacroCell.x ? 1 : -1;
        for (int row = entranceMacroCell.x; row != exitMacroCell.x + rowStep; row += rowStep)
        {
            if (row >= 0 && row < macroRows)
            {
                macroGrid[row, 1] = 14;
            }
        }

        int corridorRow = exitMacroCell.x;
        for (int col = 1; col < macroCols - 1; col++)
        {
            macroGrid[corridorRow, col] = 14;
        }
    }

    private void BuildReadableMainPath(bool useFallback)
    {
        mainMacroPath.Clear();
        mainPathPlatformY = new int[macroCols];

        int currentRow = entranceMacroCell.x;
        int exitRow = exitMacroCell.x;
        AddMainPathCell(new Vector2Int(currentRow, 0));

        for (int col = 1; col < macroCols; col++)
        {
            int remainingColumns = macroCols - 1 - col;
            int rowDeltaToExit = exitRow - currentRow;
            bool mustAdjust = Mathf.Abs(rowDeltaToExit) > remainingColumns;
            bool mayAdjust = !useFallback && rowDeltaToExit != 0 && Random.value < 0.45f;
            bool fallbackAdjust = useFallback && (col == 2 || col == 4) && rowDeltaToExit != 0;

            if ((mustAdjust || mayAdjust || fallbackAdjust) && rowDeltaToExit != 0)
            {
                currentRow += rowDeltaToExit > 0 ? 1 : -1;
                currentRow = Mathf.Clamp(currentRow, 0, macroRows - 1);
            }

            AddMainPathCell(new Vector2Int(currentRow, col));
        }

        int startPlatformY = GetMacroOriginY(entranceMacroCell.x) + 1;
        int targetPlatformY = GetMacroOriginY(exitMacroCell.x) + 1;
        mainPathPlatformY[0] = startPlatformY;

        for (int col = 1; col < macroCols; col++)
        {
            int remaining = macroCols - 1 - col;
            int delta = targetPlatformY - mainPathPlatformY[col - 1];
            int step = 0;

            if (useFallback)
            {
                if (col == 2)
                {
                    step = 2;
                }
                else if (col == 4)
                {
                    step = -2;
                }
            }

            if (step == 0 && delta != 0)
            {
                bool mustMove = Mathf.Abs(delta) > remaining * 2;
                bool mayMove = !useFallback && Random.value < 0.5f;
                if (mustMove || mayMove)
                {
                    step = delta > 0 ? 2 : -2;
                }
            }

            mainPathPlatformY[col] = Mathf.Clamp(mainPathPlatformY[col - 1] + step, 2, height - 5);
        }

        mainPathPlatformY[macroCols - 1] = Mathf.Clamp(mainPathPlatformY[macroCols - 2] + Mathf.Clamp(targetPlatformY - mainPathPlatformY[macroCols - 2], -2, 2), 2, height - 5);

        int pathLength = mainMacroPath.Count;
        if (pathLength < 7 || pathLength > 11)
        {
            Debug.Log($"[CaveLevelGenerator] Main path length={pathLength}. GameJam target is 7-11 cells.");
        }
    }

    private void AddMainPathCell(Vector2Int cell)
    {
        if (mainMacroPath.Count > 0 && mainMacroPath[mainMacroPath.Count - 1] == cell)
        {
            return;
        }

        mainMacroPath.Add(cell);

        if (cell != entranceMacroCell && cell != exitMacroCell)
        {
            macroGrid[cell.x, cell.y] = 14;
        }
    }

    private void BuildMacroCells()
    {
        for (int row = 0; row < macroRows; row++)
        {
            for (int col = 0; col < macroCols; col++)
            {
                bool isEntrance = row == entranceMacroCell.x && col == entranceMacroCell.y;
                bool isExit = row == exitMacroCell.x && col == exitMacroCell.y;
                macroCells[row, col] = new MacroTileCell(row, col, macroGrid[row, col], isEntrance, isExit);
            }
        }
    }

    private bool TryBuildReachableMacroCells()
    {
        reachableMacroCells.Clear();
        mainMacroPath.Clear();

        bool[,] visited = new bool[macroRows, macroCols];
        Vector2Int[,] previous = new Vector2Int[macroRows, macroCols];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(entranceMacroCell);
        visited[entranceMacroCell.x, entranceMacroCell.y] = true;
        previous[entranceMacroCell.x, entranceMacroCell.y] = new Vector2Int(-1, -1);
        bool foundExit = false;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            reachableMacroCells.Add(current);

            if (current == exitMacroCell)
            {
                foundExit = true;
                break;
            }

            TryQueueMacroNeighbor(current, current.x - 1, current.y, visited, previous, queue);
            TryQueueMacroNeighbor(current, current.x + 1, current.y, visited, previous, queue);
            TryQueueMacroNeighbor(current, current.x, current.y - 1, visited, previous, queue);
            TryQueueMacroNeighbor(current, current.x, current.y + 1, visited, previous, queue);
        }

        if (!foundExit)
        {
            return false;
        }

        Vector2Int pathCell = exitMacroCell;
        while (pathCell.x >= 0)
        {
            mainMacroPath.Add(pathCell);
            pathCell = previous[pathCell.x, pathCell.y];
        }

        mainMacroPath.Reverse();
        return true;
    }

    private bool TryBuildReachableMacroCellsFromMainPath()
    {
        reachableMacroCells.Clear();

        if (mainMacroPath.Count == 0 || mainMacroPath[0] != entranceMacroCell || mainMacroPath[mainMacroPath.Count - 1] != exitMacroCell)
        {
            return false;
        }

        HashSet<Vector2Int> uniqueCells = new HashSet<Vector2Int>();
        for (int i = 0; i < mainMacroPath.Count; i++)
        {
            Vector2Int cell = mainMacroPath[i];
            if (cell.x < 0 || cell.x >= macroRows || cell.y < 0 || cell.y >= macroCols || !IsMacroCellPassable(cell.x, cell.y))
            {
                return false;
            }

            if (i > 0)
            {
                Vector2Int previous = mainMacroPath[i - 1];
                int rowDelta = Mathf.Abs(previous.x - cell.x);
                int colDelta = cell.y - previous.y;
                if (colDelta != 1 || rowDelta > 1)
                {
                    return false;
                }
            }

            if (uniqueCells.Add(cell))
            {
                reachableMacroCells.Add(cell);
            }
        }

        return true;
    }

    private void TryQueueMacroNeighbor(Vector2Int from, int row, int col, bool[,] visited, Vector2Int[,] previous, Queue<Vector2Int> queue)
    {
        if (row < 0 || row >= macroRows || col < 0 || col >= macroCols || visited[row, col])
        {
            return;
        }

        if (!IsMacroCellPassable(row, col))
        {
            return;
        }

        visited[row, col] = true;
        previous[row, col] = from;
        queue.Enqueue(new Vector2Int(row, col));
    }

    private bool IsMacroCellPassable(int row, int col)
    {
        MacroTileCell cell = macroCells[row, col];
        return cell.isEntrance || cell.isExit || IsPassableMacroType(cell.typeId);
    }

    private bool IsPassableMacroType(int typeId)
    {
        return typeId == 1 || typeId == 2 || typeId == 3 || typeId == 7 || typeId == 8 || typeId == 9 || typeId == 14;
    }

    private void BuildMacroConnections()
    {
        macroConnections = new MacroConnection[macroRows, macroCols];

        for (int i = 0; i < mainMacroPath.Count - 1; i++)
        {
            OpenConnection(mainMacroPath[i], mainMacroPath[i + 1]);
        }
    }

    private void TryOpenReachableNeighbor(Vector2Int from, Vector2Int to)
    {
        if (to.x < 0 || to.x >= macroRows || to.y < 0 || to.y >= macroCols)
        {
            return;
        }

        if (!IsMacroCellPassable(from.x, from.y) || !IsMacroCellPassable(to.x, to.y))
        {
            return;
        }

        OpenConnection(from, to);
    }

    private void OpenConnection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;

        if (delta.y < 0)
        {
            macroConnections[from.x, from.y].openLeft = true;
            macroConnections[to.x, to.y].openRight = true;
        }
        else if (delta.y > 0)
        {
            macroConnections[from.x, from.y].openRight = true;
            macroConnections[to.x, to.y].openLeft = true;
        }

        if (delta.x < 0)
        {
            macroConnections[from.x, from.y].openUp = true;
            macroConnections[to.x, to.y].openDown = true;
        }
        else if (delta.x > 0)
        {
            macroConnections[from.x, from.y].openDown = true;
            macroConnections[to.x, to.y].openUp = true;
        }
    }

    private void PaintMacroGrid()
    {
        for (int row = 0; row < macroRows; row++)
        {
            for (int col = 0; col < macroCols; col++)
            {
                PaintMacroTile(row, col, macroGrid[row, col]);
            }
        }
    }

    private void DrawMainPathRouteOverlay()
    {
        for (int i = 0; i < mainMacroPath.Count; i++)
        {
            Vector2Int cell = mainMacroPath[i];
            int originX = cell.y * macroCellWidth;
            int originY = GetMacroOriginY(cell.x);
            DrawMainPathMacroCell(cell.x, cell.y, originX, originY, macroConnections[cell.x, cell.y]);
        }
    }

    private void PaintMacroTile(int row, int col, int typeId)
    {
        int originX = col * macroCellWidth;
        int originY = GetMacroOriginY(row);
        bool passable = IsMacroCellPassable(row, col);
        bool isMainPath = IsMainPathCell(row, col);
        MacroConnection connection = macroConnections[row, col];

        for (int x = originX; x < originX + macroCellWidth; x++)
        {
            for (int y = originY; y < originY + macroCellHeight; y++)
            {
                if (groundTilemap != null)
                {
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                }

                wallTilemap.SetTile(new Vector3Int(x, y, 0), null);
            }
        }

        if (isMainPath)
        {
            DrawMainPathMacroCell(row, col, originX, originY, connection);
            return;
        }

        if (!passable)
        {
            DrawBlockedMacroCell(originX, originY, typeId);
            return;
        }

        DrawNonMainDecoration(originX, originY, typeId);
    }

    private void DrawBlockedMacroCell(int originX, int originY, int typeId)
    {
        int blockHeight = Mathf.Max(1, macroCellHeight / 2);
        for (int x = originX; x < originX + macroCellWidth; x++)
        {
            for (int y = originY; y < originY + blockHeight; y++)
            {
                wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
            }
        }

        int notchX = originX + Mathf.Clamp(typeId % macroCellWidth, 2, macroCellWidth - 3);
        wallTilemap.SetTile(new Vector3Int(notchX, originY + blockHeight - 1, 0), null);
        wallTilemap.SetTile(new Vector3Int(notchX + 1, originY + blockHeight - 1, 0), null);
    }

    private void DrawMainPathMacroCell(int row, int col, int originX, int originY, MacroConnection connection)
    {
        int platformY = GetMainPathPlatformY(col);
        int left = originX + 1;
        int right = originX + macroCellWidth - 2;

        for (int x = left; x <= right; x++)
        {
            if (connection.openDown && x >= originX + macroCellWidth / 2 - 1 && x <= originX + macroCellWidth / 2)
            {
                continue;
            }

            wallTilemap.SetTile(new Vector3Int(x, platformY, 0), wallTile);
        }

        int pathIndex = GetMainPathIndex(row, col);
        if (pathIndex > 0)
        {
            Vector2Int previous = mainMacroPath[pathIndex - 1];
            int previousY = GetMainPathPlatformY(previous.y);
            if (platformY > previousY + 2)
            {
                DrawStepPlatform(originX + 1, originX + 3, previousY + 2);
                DrawStepPlatform(originX + 3, originX + 5, previousY + 4);
            }
        }

        if (pathIndex < mainMacroPath.Count - 1)
        {
            Vector2Int next = mainMacroPath[pathIndex + 1];
            int nextY = GetMainPathPlatformY(next.y);
            if (nextY > platformY + 2)
            {
                DrawStepPlatform(originX + macroCellWidth - 5, originX + macroCellWidth - 3, platformY + 2);
                DrawStepPlatform(originX + macroCellWidth - 3, originX + macroCellWidth - 1, platformY + 4);
            }
        }

        DrawMainPathDecoration(originX, platformY, col);
    }

    private void DrawNonMainDecoration(int originX, int originY, int typeId)
    {
        if (typeId == 14 || Random.value < 0.65f)
        {
            return;
        }

        int platformWidth = Mathf.Clamp(3 + typeId % 3, 3, macroCellWidth - 2);
        int startX = originX + 1 + typeId % Mathf.Max(1, macroCellWidth - platformWidth - 1);
        int y = originY + 1;
        for (int x = startX; x < startX + platformWidth; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
        }
    }

    private void DrawStepPlatform(int startX, int endX, int y)
    {
        if (y < 1 || y >= height - 2)
        {
            return;
        }

        for (int x = startX; x <= endX; x++)
        {
            if (x > 0 && x < width - 1)
            {
                wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
            }
        }
    }

    private void DrawMainPathDecoration(int originX, int platformY, int col)
    {
        if (col == 0 || col == macroCols - 1 || Random.value < 0.75f)
        {
            return;
        }

        int x = originX + Random.Range(2, macroCellWidth - 2);
        int y = platformY + Random.Range(3, 5);
        if (y < height - 1)
        {
            wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
        }
    }

    private int GetMainPathPlatformY(int col)
    {
        if (mainPathPlatformY != null && col >= 0 && col < mainPathPlatformY.Length)
        {
            return mainPathPlatformY[col];
        }

        return GetMacroOriginY(2) + 1;
    }

    private int GetMainPathIndex(int row, int col)
    {
        for (int i = 0; i < mainMacroPath.Count; i++)
        {
            if (mainMacroPath[i].x == row && mainMacroPath[i].y == col)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsMainPathCell(int row, int col)
    {
        for (int i = 0; i < mainMacroPath.Count; i++)
        {
            if (mainMacroPath[i].x == row && mainMacroPath[i].y == col)
            {
                return true;
            }
        }

        return false;
    }

    private bool ValidateSmallTilePath(Vector3Int spawnCell, Vector3Int exitCell)
    {
        Vector2Int start = new Vector2Int(spawnCell.x - 1, spawnCell.y - 1);
        Vector2Int goal = new Vector2Int(exitCell.x - 1, exitCell.y - 1);

        if (!CanPlayerOccupy(start.x, start.y))
        {
            Debug.Log($"[CaveLevelGenerator] Small tile BFS failed: spawn clearance blocked at {spawnCell}.");
            return false;
        }

        if (!CanPlayerOccupy(goal.x, goal.y))
        {
            Debug.Log($"[CaveLevelGenerator] Small tile BFS failed: exit clearance blocked at {exitCell}.");
            return false;
        }

        bool[,] visited = new bool[width, height];
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(start);
        visited[start.x, start.y] = true;

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (Mathf.Abs(current.x - goal.x) <= 1 && Mathf.Abs(current.y - goal.y) <= 1)
            {
                Debug.Log("[CaveLevelGenerator] Small tile BFS succeeded.");
                return true;
            }

            TryQueueSmallTile(current.x + 1, current.y, visited, queue);
            TryQueueSmallTile(current.x - 1, current.y, visited, queue);
            TryQueueSmallTile(current.x, current.y + 1, visited, queue);
            TryQueueSmallTile(current.x, current.y - 1, visited, queue);
        }

        Debug.Log($"[CaveLevelGenerator] Small tile BFS failed: no clear 2x3 route from {spawnCell} to {exitCell}.");
        return false;
    }

    private void TryQueueSmallTile(int x, int y, bool[,] visited, Queue<Vector2Int> queue)
    {
        if (x < 0 || y < 0 || x >= width || y >= height || visited[x, y])
        {
            return;
        }

        if (!CanPlayerOccupy(x, y))
        {
            return;
        }

        visited[x, y] = true;
        queue.Enqueue(new Vector2Int(x, y));
    }

    private bool CanPlayerOccupy(int bottomLeftX, int bottomLeftY)
    {
        if (bottomLeftX < 0 || bottomLeftY < 0 || bottomLeftX + 1 >= width || bottomLeftY + 2 >= height)
        {
            return false;
        }

        for (int x = bottomLeftX; x <= bottomLeftX + 1; x++)
        {
            for (int y = bottomLeftY; y <= bottomLeftY + 2; y++)
            {
                if (IsSolidCell(x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsSolidCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return true;
        }

        return wallTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
    }

    private void RebuildSolidMapFromWallTilemap()
    {
        solidMap = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                solidMap[x, y] = wallTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
            }
        }
    }

    private void DebugValidateSolidMapSync()
    {
        if (!debugDrawSolidCells || solidMap == null || wallTilemap == null)
        {
            return;
        }

        int wallTileCount = 0;
        int solidMapCount = 0;
        int mismatchCount = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool hasWallTile = wallTilemap.GetTile(new Vector3Int(x, y, 0)) != null;
                bool solid = solidMap[x, y];

                if (hasWallTile)
                {
                    wallTileCount++;
                }

                if (solid)
                {
                    solidMapCount++;
                }

                if (solid && !hasWallTile)
                {
                    mismatchCount++;
                    Debug.LogWarning($"[CaveLevelGenerator] solidMap has solid but WallTilemap has no tile at ({x},{y}).");
                }
                else if (hasWallTile && !solid)
                {
                    mismatchCount++;
                    Debug.LogWarning($"[CaveLevelGenerator] WallTilemap has tile but solidMap is empty at ({x},{y}).");
                }
            }
        }

        Debug.Log($"[CaveLevelGenerator] WallTilemap tile count={wallTileCount}, solidMap count={solidMapCount}, mismatch count={mismatchCount}.");
    }

    private void RefreshWallTilemapCollider()
    {
        if (wallTilemap == null)
        {
            return;
        }

        wallTilemap.RefreshAllTiles();

        TilemapCollider2D tilemapCollider = wallTilemap.GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            tilemapCollider.enabled = false;
            tilemapCollider.enabled = true;
        }

        Rigidbody2D rb = wallTilemap.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Static;
        }

        CompositeCollider2D composite = wallTilemap.GetComponent<CompositeCollider2D>();
        if (composite != null)
        {
            composite.enabled = false;
            composite.enabled = true;
        }
    }

    private void DrawCaveBounds()
    {
        for (int x = 0; x < width; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, 0, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(x, height - 1, 0), wallTile);
        }

        for (int y = 0; y < height; y++)
        {
            wallTilemap.SetTile(new Vector3Int(0, y, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(width - 1, y, 0), wallTile);
        }

        for (int col = 1; col < macroCols - 1; col++)
        {
            if (Random.value < 0.5f)
            {
                int x = col * macroCellWidth + Random.Range(2, macroCellWidth - 2);
                wallTilemap.SetTile(new Vector3Int(x, height - 2, 0), wallTile);
            }
        }
    }

    private void PrepareSpawnArea(Vector3Int spawnCell)
    {
        ClearArea3x3(spawnCell);
        int platformY = spawnCell.y - 2;
        for (int x = spawnCell.x - 3; x <= spawnCell.x + 3; x++)
        {
            if (x > 0 && x < width - 1)
            {
                wallTilemap.SetTile(new Vector3Int(x, platformY, 0), wallTile);
            }
        }
    }

    private void PlacePlayer(Vector3Int spawnCell, Vector3Int groundCell)
    {
        if (player == null)
        {
            return;
        }

        Vector3 cellCenter = wallTilemap.GetCellCenterWorld(spawnCell);
        float halfHeight = 0f;
        CapsuleCollider2D capsule = player.GetComponent<CapsuleCollider2D>();

        if (capsule != null)
        {
            halfHeight = capsule.size.y * 0.5f * Mathf.Abs(player.localScale.y);
        }
        else
        {
            BoxCollider2D box = player.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                halfHeight = box.size.y * 0.5f * Mathf.Abs(player.localScale.y);
            }
            else
            {
                Collider2D col = player.GetComponent<Collider2D>();
                if (col != null)
                {
                    halfHeight = col.bounds.size.y * 0.5f;
                }
            }
        }

        float yOffset = halfHeight + 0.02f;
        Vector3 spawnWorld = new Vector3(cellCenter.x, cellCenter.y + yOffset, player.position.z);
        player.position = spawnWorld;
        currentSpawnWorldPosition = spawnWorld;
        hasCurrentLevelSpawnPosition = true;

        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.linearVelocity = Vector2.zero;
            prb.angularVelocity = 0f;
        }

        Object tileAtGround = wallTilemap.GetTile(groundCell);
        Debug.Log($"[CaveLevelGenerator] spawnMacroCell={entranceMacroCell}, spawnCell={spawnCell}");
        Debug.Log($"[CaveLevelGenerator] groundCell={groundCell}");
        Debug.Log($"[CaveLevelGenerator] player world pos={spawnWorld}");
        Debug.Log($"[CaveLevelGenerator] wallTile at groundCell is null? {tileAtGround == null}");
    }

    private void RespawnPlayerAtEntrance()
    {
        if (player == null)
        {
            return;
        }

        player.position = currentSpawnWorldPosition;
        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.linearVelocity = Vector2.zero;
            prb.angularVelocity = 0f;
        }

        Debug.Log("Player fell. Respawn at entrance.");
    }

    private Vector3Int SpawnExitAtMainPathEnd()
    {
        Vector2Int macroCell = mainMacroPath.Count > 0 ? mainMacroPath[mainMacroPath.Count - 1] : exitMacroCell;
        Vector3Int exitCell = GetMacroCenterCell(macroCell);
        PrepareExitArea(exitCell);
        Vector3 exitWorldPos = wallTilemap.GetCellCenterWorld(exitCell);

        if (exitPrefab == null)
        {
            Debug.LogError($"[CaveLevelGenerator] Exit prefab not assigned. Exit spawn failed. exit macro cell={macroCell}, exit small cell={exitCell}, exit world position={exitWorldPos}, currentExit is null? {currentExit == null}");
            return exitCell;
        }

        currentExit = Instantiate(exitPrefab, exitWorldPos, Quaternion.identity, levelObjectsRoot);
        currentExit.name = "ExitPlaceholder";

        SpriteRenderer spriteRenderer = currentExit.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = 20;
            spriteRenderer.color = Color.green;
        }

        BoxCollider2D boxCollider = currentExit.GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            boxCollider = currentExit.AddComponent<BoxCollider2D>();
        }
        boxCollider.isTrigger = true;

        CaveExitTrigger trigger = currentExit.GetComponent<CaveExitTrigger>();
        if (trigger == null)
        {
            trigger = currentExit.AddComponent<CaveExitTrigger>();
        }
        trigger.SetLevelGenerator(this);

        Debug.Log($"[CaveLevelGenerator] Exit spawned. exit macro cell={macroCell}, exit small cell={exitCell}, exit world position={exitWorldPos}, currentExit is null? {currentExit == null}");
        return exitCell;
    }

    private void PrepareExitArea(Vector3Int exitCell)
    {
        ClearArea3x3(exitCell);

        int platformY = exitCell.y - 2;
        for (int x = exitCell.x - 2; x <= exitCell.x + 2; x++)
        {
            if (x > 0 && x < width - 1)
            {
                wallTilemap.SetTile(new Vector3Int(x, platformY, 0), wallTile);
            }
        }
    }

    private void ClearArea3x3(Vector3Int centerCell)
    {
        for (int x = centerCell.x - 1; x <= centerCell.x + 1; x++)
        {
            for (int y = centerCell.y - 1; y <= centerCell.y + 1; y++)
            {
                if (x > 0 && x < width - 1 && y > 0 && y < height - 1)
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }
    }

    private void SpawnMacroCaveEnergyNodes(Vector3Int exitCell)
    {
        if (caveEnergyNodePrefab == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Cave energy node prefab not assigned. Skipping cave energy nodes.");
            return;
        }

        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int i = 0; i < reachableMacroCells.Count; i++)
        {
            Vector2Int cell = reachableMacroCells[i];
            if (cell == entranceMacroCell || cell == exitMacroCell)
            {
                continue;
            }

            candidates.Add(cell);
        }

        Shuffle(candidates);

        int targetNodeCount = Random.Range(minCaveEnergyNodes, maxCaveEnergyNodes + 1);
        targetNodeCount = Mathf.Clamp(targetNodeCount, 0, candidates.Count);
        int spawnedCount = 0;

        for (int i = 0; i < candidates.Count && spawnedCount < targetNodeCount; i++)
        {
            Vector2Int macroCell = candidates[i];
            Vector3Int nodeCell = GetMacroCenterCell(macroCell);
            if (nodeCell == exitCell || IsSolidCell(nodeCell.x, nodeCell.y) || !CanPlayerOccupy(nodeCell.x - 1, nodeCell.y - 1))
            {
                continue;
            }

            Vector3 worldPos = wallTilemap.GetCellCenterWorld(nodeCell);

            GameObject node = Instantiate(caveEnergyNodePrefab, worldPos, Quaternion.identity, levelObjectsRoot);
            node.name = "CaveEnergyNodePlaceholder";

            if (node.GetComponent<CaveEnergyNode>() == null)
            {
                Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing CaveEnergyNode script.");
            }

            if (node.GetComponent<BoxCollider2D>() == null)
            {
                Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing BoxCollider2D.");
            }

            Debug.Log($"[CaveLevelGenerator] Cave energy node macro={macroCell}, cell={nodeCell}, world pos={worldPos}");
            spawnedCount++;
        }

        Debug.Log($"[CaveLevelGenerator] Spawned {spawnedCount} cave energy nodes.");
    }

    private Vector3Int GetMacroCenterCell(Vector2Int macroCell)
    {
        int originX = macroCell.y * macroCellWidth;
        int platformY = GetMainPathPlatformY(macroCell.y);
        return new Vector3Int(originX + macroCellWidth / 2, platformY + 2, 0);
    }

    private Vector3Int GetMainPathExitCell()
    {
        Vector2Int macroCell = mainMacroPath.Count > 0 ? mainMacroPath[mainMacroPath.Count - 1] : exitMacroCell;
        return GetMacroCenterCell(macroCell);
    }

    private int GetMacroOriginY(int row)
    {
        return (macroRows - 1 - row) * macroCellHeight;
    }

    private int PickRandomType(int[] values)
    {
        return values[Random.Range(0, values.Length)];
    }

    private void Shuffle(List<int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = temp;
        }
    }

    private void Shuffle(List<Vector2Int> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            Vector2Int temp = values[i];
            values[i] = values[swapIndex];
            values[swapIndex] = temp;
        }
    }

    private void LogMacroGrid()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[CaveLevelGenerator] 5x7 macroGrid:");
        for (int row = 0; row < macroRows; row++)
        {
            builder.Append("row ");
            builder.Append(row);
            builder.Append(": ");

            for (int col = 0; col < macroCols; col++)
            {
                builder.Append(macroGrid[row, col].ToString("00"));
                if (col < macroCols - 1)
                {
                    builder.Append(" ");
                }
            }

            builder.AppendLine();
        }

        builder.AppendLine($"entrance={entranceMacroCell}, exit={exitMacroCell}, reachableCells={reachableMacroCells.Count}");
        Debug.Log(builder.ToString());
    }

    private void LogMacroPathAndConnections()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append("[CaveLevelGenerator] Main macro path: ");
        for (int i = 0; i < mainMacroPath.Count; i++)
        {
            builder.Append(mainMacroPath[i]);
            if (i < mainMacroPath.Count - 1)
            {
                builder.Append(" -> ");
            }
        }
        Debug.Log(builder.ToString());

        StringBuilder connectionBuilder = new StringBuilder();
        connectionBuilder.AppendLine("[CaveLevelGenerator] Macro cell openings:");
        for (int row = 0; row < macroRows; row++)
        {
            for (int col = 0; col < macroCols; col++)
            {
                if (!IsMacroCellPassable(row, col))
                {
                    continue;
                }

                MacroConnection connection = macroConnections[row, col];
                connectionBuilder.AppendLine($"cell=({row},{col}) type={macroGrid[row, col]} openLeft={connection.openLeft} openRight={connection.openRight} openUp={connection.openUp} openDown={connection.openDown}");
            }
        }

        Debug.Log(connectionBuilder.ToString());
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugDrawSolidCells || wallTilemap == null)
        {
            return;
        }

        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (wallTilemap.GetTile(new Vector3Int(x, y, 0)) == null)
                {
                    continue;
                }

                Vector3 center = wallTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
                Gizmos.DrawCube(center, new Vector3(0.9f, 0.9f, 0.05f));
            }
        }
    }
}
