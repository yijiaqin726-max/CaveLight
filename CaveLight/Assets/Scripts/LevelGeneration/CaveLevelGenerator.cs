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
    public Tile platformTile;
    public Tile bottomTile;
    public Tile ceilingTile;
    public Tile leftWallTile;
    public Tile rightWallTile;

    [Header("Map Size")]
    public int width = 32;
    public int height = 18;

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
    private readonly List<SpawnPoint> platformSpawnPoints = new List<SpawnPoint>();
    private readonly List<SpawnPoint> validMonsterSpawnPoints = new List<SpawnPoint>();
    private readonly List<SpawnPoint> rejectedMonsterSpawnPoints = new List<SpawnPoint>();
    private readonly List<Vector3> actualMonsterSpawnPositions = new List<Vector3>();
    private readonly List<Vector3> energyNodeSpawnPositions = new List<Vector3>();
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

    private struct SpawnPoint
    {
        public Vector3 worldPosition;
        public int platformIndex;
        public int platformStartX;
        public int platformEndX;
        public int platformY;
        public int cellX;
        public bool isEntrancePlatform;
        public bool isExitPlatform;
        public bool canSpawnMonster;
        public bool canSpawnEnergyNode;
        public string invalidReason;
    }

    void Start()
    {
        RunStatsManager.Instance.ResetRun();

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

        RunStatsManager.Instance.AddCaveCleared();
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
        FitCameraToGeneratedMap();
        LogMapAspectDebug();

        Debug.Log("[CaveLevelGenerator] Stable GameJam map generation completed.");
    }

    private void FitCameraToGeneratedMap()
    {
        FixedMapCameraController fixedMapCamera = FindFirstObjectByType<FixedMapCameraController>();
        if (fixedMapCamera == null && Camera.main != null)
        {
            fixedMapCamera = Camera.main.GetComponent<FixedMapCameraController>();
            if (fixedMapCamera == null)
            {
                fixedMapCamera = Camera.main.gameObject.AddComponent<FixedMapCameraController>();
            }
        }

        if (fixedMapCamera != null)
        {
            fixedMapCamera.FitToTilemap(wallTilemap);
        }
        else
        {
            Debug.LogWarning("[CaveLevelGenerator] FixedMapCameraController not found. Camera fit skipped.");
        }
    }

    private void GenerateStableGameJamMap()
    {
        macroRows = 5;
        macroCols = 7;
        macroCellWidth = 8;
        macroCellHeight = 5;
        width = 32;
        height = 18;

        stableMainPlatforms.Clear();
        occupiedStablePlatformIndices.Clear();
        platformSpawnPoints.Clear();
        validMonsterSpawnPoints.Clear();
        rejectedMonsterSpawnPoints.Clear();
        actualMonsterSpawnPositions.Clear();
        energyNodeSpawnPositions.Clear();
        BuildStableMainPlatforms();
        PaintStableCaveBackground();
        PaintStableMainPlatforms();
        BuildPlatformSpawnPoints();
        PaintStableDecorations();

        StablePlatform firstPlatform = stableMainPlatforms[0];
        StablePlatform lastPlatform = stableMainPlatforms[stableMainPlatforms.Count - 1];
        Vector3Int spawnCell = new Vector3Int(firstPlatform.xStart + 2, firstPlatform.y + 2, 0);
        Vector3Int exitCell = new Vector3Int(lastPlatform.CenterX, lastPlatform.y + 2, 0);

        PrepareSpawnArea(spawnCell);
        PrepareExitArea(exitCell);

        PlacePlayer(spawnCell, new Vector3Int(spawnCell.x, spawnCell.y - 2, 0));
        SpawnCaveEnergyNodes(exitCell);
        RefreshWallTilemapCollider();
        SpawnMonsters();
        SpawnExitOnStablePlatform(lastPlatform);

        int wallTileCount = CountWallTiles();
        Debug.Log($"[CaveLevelGenerator] Stable platforms={stableMainPlatforms.Count}, WallTilemap tile count={wallTileCount}.");
        LogStablePlatforms();
    }

    private void BuildStableMainPlatforms()
    {
        int[] safeHeights = { 4, 5, 6, 7, 8 };
        int platformCount = Random.Range(5, 8);
        int currentY = 4;
        StablePlatform previous = default;

        for (int i = 0; i < platformCount; i++)
        {
            int platformWidth = Random.Range(4, 8);
            int targetX;

            if (i == 0)
            {
                targetX = 2;
                platformWidth = Random.Range(5, 7);
                currentY = 4;
            }
            else if (i == platformCount - 1)
            {
                targetX = Random.Range(25, 27);
                platformWidth = Mathf.Min(Random.Range(4, 6), width - 2 - targetX);
                currentY = Mathf.Clamp(currentY + Random.Range(-1, 2), 4, 8);
            }
            else
            {
                float t = i / (float)(platformCount - 1);
                targetX = Mathf.RoundToInt(Mathf.Lerp(7, 22, t)) + Random.Range(-1, 2);
            }

            if (i > 0)
            {
                int maxReachableStart = previous.XEnd + 3;
                int minForwardStart = previous.xStart + 3;
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
            wallTilemap.SetTile(new Vector3Int(x, 0, 0), GetBottomTile());
            wallTilemap.SetTile(new Vector3Int(x, height - 1, 0), GetCeilingTile());
        }

        for (int y = 0; y < height; y++)
        {
            wallTilemap.SetTile(new Vector3Int(0, y, 0), GetLeftWallTile());
            wallTilemap.SetTile(new Vector3Int(width - 1, y, 0), GetRightWallTile());
        }

        for (int x = 3; x < width - 3; x += Random.Range(4, 8))
        {
            int ceilingDepth = Random.Range(1, 3);
            for (int y = 0; y < ceilingDepth; y++)
            {
                wallTilemap.SetTile(new Vector3Int(x, height - 2 - y, 0), GetCeilingTile());
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
            wallTilemap.SetTile(new Vector3Int(x, y, 0), GetPlatformTile());
        }
    }

    private void BuildPlatformSpawnPoints()
    {
        platformSpawnPoints.Clear();

        for (int platformIndex = 0; platformIndex < stableMainPlatforms.Count; platformIndex++)
        {
            StablePlatform platform = stableMainPlatforms[platformIndex];
            int startX = Mathf.Clamp(platform.xStart + 1, 1, width - 2);
            int endX = Mathf.Clamp(platform.XEnd - 1, startX, width - 2);

            for (int x = startX; x <= endX; x++)
            {
                Vector3Int groundCell = new Vector3Int(x, platform.y, 0);
                Vector3 groundCenter = wallTilemap.GetCellCenterWorld(groundCell);
                float groundTopY = groundCenter.y + wallTilemap.cellSize.y * 0.5f;
                Vector3 worldPosition = new Vector3(groundCenter.x, groundTopY, 0f);

                platformSpawnPoints.Add(new SpawnPoint
                {
                    worldPosition = worldPosition,
                    platformIndex = platformIndex,
                    platformStartX = platform.xStart,
                    platformEndX = platform.XEnd,
                    platformY = platform.y,
                    cellX = x,
                    isEntrancePlatform = platformIndex == 0,
                    isExitPlatform = platformIndex == stableMainPlatforms.Count - 1,
                    canSpawnMonster = false,
                    canSpawnEnergyNode = true,
                    invalidReason = string.Empty
                });
            }
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
        SetupImportantSpriteRenderer(currentExit, 999);

        SpriteRenderer spriteRenderer = currentExit.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color color = Color.white;
            color.a = 1f;
            spriteRenderer.color = color;
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
            Vector3Int groundCell = new Vector3Int(x, platform.y, 0);
            if (Mathf.Abs(nodeCell.x - exitCell.x) < 2)
            {
                continue;
            }

            Vector3 worldPos = wallTilemap.GetCellCenterWorld(nodeCell);
            GameObject node = Instantiate(caveEnergyNodePrefab, worldPos, Quaternion.identity, levelObjectsRoot);
            node.name = "CaveEnergyNodePlaceholder";
            SetupImportantSpriteRenderer(node, 999);
            occupiedStablePlatformIndices.Add(candidateIndices[i]);

            if (node.GetComponent<CaveEnergyNode>() == null)
            {
                Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing CaveEnergyNode script.");
            }

            ConfigureSpawnedCaveEnergyNodeVisual(node, nodeCell);
            Vector3 groundCenter = wallTilemap.GetCellCenterWorld(groundCell);
            float groundTopY = groundCenter.y + wallTilemap.cellSize.y * 0.5f;
            AlignColliderBottomToGround(node, groundTopY);
            energyNodeSpawnPositions.Add(node.transform.position);
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
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        BoxCollider2D boxCollider = node.GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] Cave energy node at {nodeCell} is missing BoxCollider2D.");
        }

        if (node.GetComponent<CaveEnergyNode>() == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing CaveEnergyNode script.");
        }

        Debug.Log($"[CaveLevelGenerator] Cave energy node generated at cell={nodeCell}, world={node.transform.position}, spriteRendererExists={spriteRenderer != null}, spriteRendererEnabled={(spriteRenderer != null && spriteRenderer.enabled)}, sortingOrder={(spriteRenderer != null ? spriteRenderer.sortingOrder : -1)}, colliderIsTrigger={(boxCollider != null && boxCollider.isTrigger)}, colliderSize={(boxCollider != null ? boxCollider.size : Vector2.zero)}");
    }

    private void AlignColliderBottomToGround(GameObject obj, float groundTopY)
    {
        Physics2D.SyncTransforms();

        Collider2D col = obj != null ? obj.GetComponent<Collider2D>() : null;
        if (col == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] {(obj != null ? obj.name : "null")} has no Collider2D, cannot align to ground.");
            return;
        }

        float colliderBottomY = col.bounds.min.y;
        float deltaY = groundTopY - colliderBottomY + 0.02f;
        obj.transform.position += new Vector3(0f, deltaY, 0f);

        Physics2D.SyncTransforms();

        Debug.Log($"[CaveLevelGenerator] Align {obj.name}: groundTopY={groundTopY}, oldBottom={colliderBottomY}, deltaY={deltaY}, newBottom={col.bounds.min.y}");
    }

    private bool TryFindGroundTopYBelow(Vector3 worldPos, out float groundTopY)
    {
        if (wallTilemap == null)
        {
            groundTopY = 0f;
            return false;
        }

        Vector3Int startCell = wallTilemap.WorldToCell(worldPos);

        for (int y = startCell.y + 3; y >= startCell.y - 8; y--)
        {
            Vector3Int cell = new Vector3Int(startCell.x, y, 0);
            if (wallTilemap.HasTile(cell))
            {
                Vector3 center = wallTilemap.GetCellCenterWorld(cell);
                groundTopY = center.y + wallTilemap.cellSize.y * 0.5f;
                return true;
            }
        }

        groundTopY = 0f;
        return false;
    }

    private Tile GetPlatformTile()
    {
        return platformTile != null ? platformTile : wallTile;
    }

    private Tile GetBottomTile()
    {
        return bottomTile != null ? bottomTile : GetPlatformTile();
    }

    private Tile GetCeilingTile()
    {
        return ceilingTile != null ? ceilingTile : GetPlatformTile();
    }

    private Tile GetLeftWallTile()
    {
        return leftWallTile != null ? leftWallTile : GetBottomTile();
    }

    private Tile GetRightWallTile()
    {
        return rightWallTile != null ? rightWallTile : GetBottomTile();
    }

    private void SetupImportantSpriteRenderer(GameObject instance, int sortingOrder)
    {
        if (instance == null)
        {
            return;
        }

        SpriteRenderer spriteRenderer = instance.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[CaveLevelGenerator] {instance.name} is missing SpriteRenderer. Cannot set important sorting order.");
            return;
        }

        spriteRenderer.enabled = true;
        spriteRenderer.sortingLayerName = SortingLayerExists("Important") ? "Important" : "Default";
        spriteRenderer.sortingOrder = sortingOrder;

        Color color = spriteRenderer.color;
        color.a = 1f;
        spriteRenderer.color = color;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            spriteRenderer.material = new Material(shader);
        }

        Debug.Log($"[CaveLevelGenerator] SetupImportantSpriteRenderer object={instance.name}, sortingOrder={spriteRenderer.sortingOrder}");
    }

    private static bool SortingLayerExists(string sortingLayerName)
    {
        SortingLayer[] sortingLayers = SortingLayer.layers;
        for (int i = 0; i < sortingLayers.Length; i++)
        {
            if (sortingLayers[i].name == sortingLayerName)
            {
                return true;
            }
        }

        return false;
    }

    private void SpawnMonsters()
    {
        if (monsterPrefab == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Monster prefab not assigned. Skipping monsters.");
            return;
        }

        validMonsterSpawnPoints.Clear();
        rejectedMonsterSpawnPoints.Clear();
        actualMonsterSpawnPositions.Clear();

        float monsterHalfHeight = GetPrefabColliderHalfHeight(monsterPrefab);
        float monsterHalfWidth = GetPrefabColliderHalfWidth(monsterPrefab);

        for (int i = 0; i < platformSpawnPoints.Count; i++)
        {
            SpawnPoint spawnPoint = platformSpawnPoints[i];
            spawnPoint.canSpawnMonster = ValidateMonsterSpawnPoint(spawnPoint, monsterHalfWidth, monsterHalfHeight, out string invalidReason, out Vector3 finalPosition);
            spawnPoint.invalidReason = invalidReason;
            spawnPoint.worldPosition = finalPosition;

            if (spawnPoint.canSpawnMonster)
            {
                validMonsterSpawnPoints.Add(spawnPoint);
            }
            else
            {
                rejectedMonsterSpawnPoints.Add(spawnPoint);
            }

            Debug.Log($"[CaveLevelGenerator] Monster candidate platformIndex={spawnPoint.platformIndex}, position={spawnPoint.worldPosition}, valid={spawnPoint.canSpawnMonster}, reason={(string.IsNullOrEmpty(spawnPoint.invalidReason) ? "ok" : spawnPoint.invalidReason)}");
        }

        Debug.Log($"[CaveLevelGenerator] Monster spawn point summary: platforms={stableMainPlatforms.Count}, candidates={platformSpawnPoints.Count}, validCandidates={validMonsterSpawnPoints.Count}, rejectedCandidates={rejectedMonsterSpawnPoints.Count}");

        if (validMonsterSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[CaveLevelGenerator] No valid monster spawn point found.");
            Debug.Log("[CaveLevelGenerator] Spawned 0 monsters.");
            return;
        }

        Shuffle(validMonsterSpawnPoints);
        int effectiveMinMonsters = Mathf.Max(1, minMonsters);
        int effectiveMaxMonsters = Mathf.Max(effectiveMinMonsters, maxMonsters);

        int distinctPlatformCount = CountDistinctMonsterPlatforms(validMonsterSpawnPoints);
        int targetMonsterCount = Mathf.Clamp(Random.Range(effectiveMinMonsters, effectiveMaxMonsters + 1), 1, Mathf.Min(3, distinctPlatformCount));
        int spawnedCount = 0;
        HashSet<int> usedPlatformIndices = new HashSet<int>();

        for (int i = 0; i < validMonsterSpawnPoints.Count && spawnedCount < targetMonsterCount; i++)
        {
            SpawnPoint spawnPoint = validMonsterSpawnPoints[i];
            if (usedPlatformIndices.Contains(spawnPoint.platformIndex))
            {
                continue;
            }

            Vector3Int monsterCell = new Vector3Int(spawnPoint.cellX, spawnPoint.platformY + 1, 0);
            GameObject monster = Instantiate(monsterPrefab, spawnPoint.worldPosition, Quaternion.identity, levelObjectsRoot);
            monster.name = "MonsterPlaceholder";
            ConfigureSpawnedMonster(monster, monsterCell);

            bool insideGround = ResolveMonsterGroundOverlap(monster);
            if (insideGround)
            {
                Destroy(monster);
                Debug.LogWarning("[CaveLevelGenerator] Monster spawn failed: inside ground.");
                continue;
            }

            usedPlatformIndices.Add(spawnPoint.platformIndex);
            actualMonsterSpawnPositions.Add(monster.transform.position);
            Debug.Log($"[CaveLevelGenerator] Monster spawned platform index={spawnPoint.platformIndex}, cellX={spawnPoint.cellX}, final={monster.transform.position}, groundY={spawnPoint.worldPosition.y - monsterHalfHeight - 0.05f:0.###}, halfHeight={monsterHalfHeight:0.###}, insideGround={insideGround}");
            spawnedCount++;
        }

        if (spawnedCount == 0)
        {
            Debug.LogWarning("[CaveLevelGenerator] No valid monster spawn point found.");
        }

        Debug.Log($"[CaveLevelGenerator] Spawned {spawnedCount} monsters.");
    }

    private bool ValidateMonsterSpawnPoint(SpawnPoint spawnPoint, float monsterHalfWidth, float monsterHalfHeight, out string invalidReason, out Vector3 finalPosition)
    {
        invalidReason = string.Empty;
        finalPosition = spawnPoint.worldPosition;

        int platformWidth = spawnPoint.platformEndX - spawnPoint.platformStartX + 1;
        if (spawnPoint.isEntrancePlatform)
        {
            invalidReason = "entrance platform";
            return false;
        }

        if (spawnPoint.isExitPlatform)
        {
            invalidReason = "exit platform";
            return false;
        }

        if (platformWidth < 4)
        {
            invalidReason = "platform width < 4";
            return false;
        }

        if (spawnPoint.platformStartX <= 0 || spawnPoint.platformEndX >= width - 1)
        {
            invalidReason = "platform touches map edge";
            return false;
        }

        if (!IsSolidCell(spawnPoint.cellX, spawnPoint.platformY))
        {
            invalidReason = "no wall tile under spawn point";
            return false;
        }

        for (int y = spawnPoint.platformY + 1; y <= spawnPoint.platformY + 3; y++)
        {
            if (IsSolidCell(spawnPoint.cellX, y))
            {
                invalidReason = "less than 3 cells of headroom";
                return false;
            }
        }

        Vector3 groundCenter = wallTilemap.GetCellCenterWorld(new Vector3Int(spawnPoint.cellX, spawnPoint.platformY, 0));
        float groundTopY = groundCenter.y + wallTilemap.cellSize.y * 0.5f;
        finalPosition = new Vector3(groundCenter.x, groundTopY + monsterHalfHeight + 0.05f, 0f);

        for (int i = 0; i < energyNodeSpawnPositions.Count; i++)
        {
            if (Vector3.Distance(finalPosition, energyNodeSpawnPositions[i]) < 2f)
            {
                invalidReason = "too close to energy node";
                return false;
            }
        }

        if (hasCurrentLevelSpawnPosition && Vector3.Distance(finalPosition, currentSpawnWorldPosition) < 4f)
        {
            invalidReason = "too close to player spawn";
            return false;
        }

        if (stableMainPlatforms.Count > 0)
        {
            StablePlatform exitPlatform = stableMainPlatforms[stableMainPlatforms.Count - 1];
            Vector3 exitGroundCenter = wallTilemap.GetCellCenterWorld(new Vector3Int(exitPlatform.CenterX, exitPlatform.y, 0));
            float exitGroundTopY = exitGroundCenter.y + wallTilemap.cellSize.y * 0.5f;
            Vector3 exitApproxPosition = new Vector3(exitGroundCenter.x, exitGroundTopY + 1f, 0f);
            if (Vector3.Distance(finalPosition, exitApproxPosition) < 4f)
            {
                invalidReason = "too close to exit";
                return false;
            }
        }

        if (WouldMonsterOverlapWallCells(finalPosition, monsterHalfWidth, monsterHalfHeight))
        {
            invalidReason = "monster body overlaps wall tile";
            return false;
        }

        return true;
    }

    private bool WouldMonsterOverlapWallCells(Vector3 finalPosition, float monsterHalfWidth, float monsterHalfHeight)
    {
        float epsilon = 0.02f;
        Vector3 bottomLeft = new Vector3(finalPosition.x - monsterHalfWidth + epsilon, finalPosition.y - monsterHalfHeight + epsilon, 0f);
        Vector3 topRight = new Vector3(finalPosition.x + monsterHalfWidth - epsilon, finalPosition.y + monsterHalfHeight - epsilon, 0f);
        Vector3Int minCell = wallTilemap.WorldToCell(bottomLeft);
        Vector3Int maxCell = wallTilemap.WorldToCell(topRight);

        for (int x = minCell.x; x <= maxCell.x; x++)
        {
            for (int y = minCell.y; y <= maxCell.y; y++)
            {
                if (IsSolidCell(x, y))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private int CountDistinctMonsterPlatforms(List<SpawnPoint> spawnPoints)
    {
        HashSet<int> platformIndices = new HashSet<int>();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            platformIndices.Add(spawnPoints[i].platformIndex);
        }

        return platformIndices.Count;
    }

    private bool TrySnapSpawnPositionToGround(Vector3 desiredPosition, GameObject prefab, out Vector3 snappedPosition, out Vector2 groundHitPoint, out float monsterHalfHeight)
    {
        snappedPosition = desiredPosition;
        groundHitPoint = Vector2.zero;
        monsterHalfHeight = GetPrefabColliderHalfHeight(prefab);

        if (wallTilemap == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] WallTilemap missing. Cannot snap monster spawn to ground.");
            return false;
        }

        Vector2 rayStart = desiredPosition + Vector3.up * 3f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, 8f);
        RaycastHit2D bestHit = default;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || (!hit.collider.transform.IsChildOf(wallTilemap.transform) && hit.collider.transform != wallTilemap.transform))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestHit = hit;
                bestDistance = hit.distance;
            }
        }

        if (bestHit.collider == null)
        {
            return false;
        }

        groundHitPoint = bestHit.point;
        snappedPosition = new Vector3(desiredPosition.x, groundHitPoint.y + monsterHalfHeight + 0.05f, desiredPosition.z);
        return true;
    }

    private float GetPrefabColliderHalfHeight(GameObject prefab)
    {
        if (prefab == null)
        {
            return 0.5f;
        }

        BoxCollider2D boxCollider = prefab.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            return Mathf.Max(0.1f, Mathf.Abs(boxCollider.size.y * prefab.transform.lossyScale.y) * 0.5f);
        }

        CapsuleCollider2D capsuleCollider = prefab.GetComponent<CapsuleCollider2D>();
        if (capsuleCollider != null)
        {
            return Mathf.Max(0.1f, Mathf.Abs(capsuleCollider.size.y * prefab.transform.lossyScale.y) * 0.5f);
        }

        Collider2D collider = prefab.GetComponent<Collider2D>();
        if (collider != null && collider.bounds.size.y > 0f)
        {
            return Mathf.Max(0.1f, collider.bounds.extents.y);
        }

        return 0.5f;
    }

    private float GetPrefabColliderHalfWidth(GameObject prefab)
    {
        if (prefab == null)
        {
            return 0.5f;
        }

        BoxCollider2D boxCollider = prefab.GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            return Mathf.Max(0.1f, Mathf.Abs(boxCollider.size.x * prefab.transform.lossyScale.x) * 0.5f);
        }

        CapsuleCollider2D capsuleCollider = prefab.GetComponent<CapsuleCollider2D>();
        if (capsuleCollider != null)
        {
            return Mathf.Max(0.1f, Mathf.Abs(capsuleCollider.size.x * prefab.transform.lossyScale.x) * 0.5f);
        }

        Collider2D collider = prefab.GetComponent<Collider2D>();
        if (collider != null && collider.bounds.size.x > 0f)
        {
            return Mathf.Max(0.1f, collider.bounds.extents.x);
        }

        return 0.5f;
    }

    private bool ResolveMonsterGroundOverlap(GameObject monster)
    {
        Collider2D monsterCollider = monster != null ? monster.GetComponent<Collider2D>() : null;
        if (monsterCollider == null || wallTilemap == null)
        {
            return false;
        }

        for (int attempt = 0; attempt < 10 && IsMonsterInsideGround(monsterCollider); attempt++)
        {
            monster.transform.position += Vector3.up * 0.1f;
        }

        return IsMonsterInsideGround(monsterCollider);
    }

    private bool IsMonsterInsideGround(Collider2D monsterCollider)
    {
        if (monsterCollider == null || wallTilemap == null)
        {
            return false;
        }

        Collider2D[] wallColliders = wallTilemap.GetComponents<Collider2D>();
        for (int i = 0; i < wallColliders.Length; i++)
        {
            Collider2D wallCollider = wallColliders[i];
            if (wallCollider == null || !wallCollider.enabled)
            {
                continue;
            }

            ColliderDistance2D distance = monsterCollider.Distance(wallCollider);
            if (distance.isOverlapped)
            {
                return true;
            }
        }

        return false;
    }

    private void ConfigureSpawnedMonster(GameObject monster, Vector3Int monsterCell)
    {
        SpriteRenderer spriteRenderer = monster.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.sortingLayerName = "Gameplay";
            spriteRenderer.sortingOrder = 8;
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

    private void LogMapAspectDebug()
    {
        float mapAspect = height > 0 ? width / (float)height : 0f;
        float targetAspect = 16f / 9f;
        bool nearSixteenNine = Mathf.Abs(mapAspect - targetAspect) < 0.05f;
        Bounds wallBounds = wallTilemap != null ? wallTilemap.localBounds : default;

        Camera main = Camera.main;
        float cameraSize = main != null ? main.orthographicSize : 0f;
        Vector3 cameraPosition = main != null ? main.transform.position : Vector3.zero;

        Debug.Log($"[CaveLevelGenerator] mapWidth={width}, mapHeight={height}, mapAspect={mapAspect:0.000}, targetAspect={targetAspect:0.000}, near16x9={nearSixteenNine}, cameraOrthographicSize={cameraSize:0.000}, cameraPosition={cameraPosition}, tilemapBoundsCenter={wallBounds.center}, tilemapBoundsSize={wallBounds.size}");
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
                wallTilemap.SetTile(new Vector3Int(x, y, 0), GetBottomTile());
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

            wallTilemap.SetTile(new Vector3Int(x, platformY, 0), GetPlatformTile());
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
            wallTilemap.SetTile(new Vector3Int(x, y, 0), GetPlatformTile());
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
                wallTilemap.SetTile(new Vector3Int(x, y, 0), GetPlatformTile());
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
            wallTilemap.SetTile(new Vector3Int(x, y, 0), GetPlatformTile());
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
            wallTilemap.SetTile(new Vector3Int(x, 0, 0), GetBottomTile());
            wallTilemap.SetTile(new Vector3Int(x, height - 1, 0), GetCeilingTile());
        }

        for (int y = 0; y < height; y++)
        {
            wallTilemap.SetTile(new Vector3Int(0, y, 0), GetLeftWallTile());
            wallTilemap.SetTile(new Vector3Int(width - 1, y, 0), GetRightWallTile());
        }

        for (int col = 1; col < macroCols - 1; col++)
        {
            if (Random.value < 0.5f)
            {
                int x = col * macroCellWidth + Random.Range(2, macroCellWidth - 2);
                wallTilemap.SetTile(new Vector3Int(x, height - 2, 0), GetCeilingTile());
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
                wallTilemap.SetTile(new Vector3Int(x, platformY, 0), GetPlatformTile());
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
        SetupImportantSpriteRenderer(currentExit, 999);

        SpriteRenderer spriteRenderer = currentExit.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color color = Color.white;
            color.a = 1f;
            spriteRenderer.color = color;
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
                wallTilemap.SetTile(new Vector3Int(x, platformY, 0), GetPlatformTile());
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
            SetupImportantSpriteRenderer(node, 999);

            if (node.GetComponent<CaveEnergyNode>() == null)
            {
                Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing CaveEnergyNode script.");
            }

            if (node.GetComponent<BoxCollider2D>() == null)
            {
                Debug.LogWarning($"[CaveLevelGenerator] Spawned cave energy node at {nodeCell} is missing BoxCollider2D.");
            }

            SpriteRenderer spriteRenderer = node.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                Color color = spriteRenderer.color;
                color.a = 1f;
                spriteRenderer.color = color;
            }

            if (TryFindGroundTopYBelow(worldPos, out float groundTopY))
            {
                AlignColliderBottomToGround(node, groundTopY);
            }
            else
            {
                Debug.LogWarning($"[CaveLevelGenerator] Cannot find ground below crystal spawn position {worldPos}");
            }

            Debug.Log($"[CaveLevelGenerator] Cave energy node macro={macroCell}, cell={nodeCell}, world pos={node.transform.position}");
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

    private void Shuffle(List<SpawnPoint> values)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            SpawnPoint temp = values[i];
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
        if (wallTilemap == null)
        {
            return;
        }

        if (debugDrawSolidCells)
        {
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

        Gizmos.color = Color.gray;
        for (int i = 0; i < rejectedMonsterSpawnPoints.Count; i++)
        {
            Gizmos.DrawSphere(rejectedMonsterSpawnPoints[i].worldPosition, 0.12f);
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < validMonsterSpawnPoints.Count; i++)
        {
            Gizmos.DrawSphere(validMonsterSpawnPoints[i].worldPosition, 0.15f);
        }

        Gizmos.color = new Color(0.75f, 0f, 1f, 1f);
        for (int i = 0; i < actualMonsterSpawnPositions.Count; i++)
        {
            Gizmos.DrawSphere(actualMonsterSpawnPositions[i], 0.2f);
        }
    }
}
