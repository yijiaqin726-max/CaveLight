using System.Collections.Generic;
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
    public int maxJumpHeight = 2; // max single jump height in tiles
    public int minPlatformWidth = 4;
    public int maxPlatformWidth = 7;

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

    private const int SpawnPlatformXStart = 1;
    private const int SpawnPlatformY = 1;
    private const int SpawnPlatformWidth = 6;
    private const int RequiredClearanceAbovePlatform = 3;
    private const int MaxNextPlatformAttempts = 20;

    private readonly int[] safePlatformHeights = { 1, 3, 5, 7 };

    private struct PlatformData
    {
        public int xStart;
        public int y;
        public int width;

        public int XEnd => xStart + width - 1;
        public int CenterX => xStart + width / 2;

        public PlatformData(int xStart, int y, int width)
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
    }

    void Update()
    {
        if (!inMerchantRoom && Input.GetKeyDown(KeyCode.R))
        {
            GenerateLevel();
            Debug.Log("[CaveLevelGenerator] Regenerated map on R key.");
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

        ClearAllLevelTilemaps();

        EnsureLevelObjectsRoot();
        ClearDynamicLevelObjects();

        DrawBoundsAndBaseFloor();

        List<PlatformData> mainPathPlatforms = GenerateReachableMainPath();
        Vector3Int spawnCell = new Vector3Int(3, 3, 0);
        Vector3Int groundCell = new Vector3Int(3, 1, 0);

        wallTilemap.SetTile(groundCell, wallTile);
        PlacePlayer(spawnCell, groundCell);

        PlatformData exitPlatform = mainPathPlatforms[mainPathPlatforms.Count - 1];
        Vector3Int exitCell = SpawnExitOnLastPlatform(exitPlatform);
        SpawnCaveEnergyNodes(mainPathPlatforms, exitCell);

        LogMainPath(mainPathPlatforms);
        Debug.Log("[CaveLevelGenerator] Map generation completed.");
    }

    private void ClearAllLevelTilemaps()
    {
        if (wallTilemap != null)
        {
            wallTilemap.ClearAllTiles();
        }

        if (groundTilemap != null)
        {
            groundTilemap.ClearAllTiles();
        }

        if (decorationTilemap != null)
        {
            decorationTilemap.ClearAllTiles();
        }
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

    private void DrawBoundsAndBaseFloor()
    {
        int ceilingY = height - 1;

        for (int x = 0; x < width; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, ceilingY, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(x, 0, 0), wallTile);
        }

        for (int y = 0; y < height; y++)
        {
            wallTilemap.SetTile(new Vector3Int(0, y, 0), wallTile);
            wallTilemap.SetTile(new Vector3Int(width - 1, y, 0), wallTile);
        }
    }

    private List<PlatformData> GenerateReachableMainPath()
    {
        List<PlatformData> platforms = new List<PlatformData>();

        PlatformData spawnPlatform = new PlatformData(SpawnPlatformXStart, SpawnPlatformY, SpawnPlatformWidth);
        platforms.Add(spawnPlatform);
        DrawPlatform(spawnPlatform);

        int platformCount = Random.Range(4, 7);

        for (int i = 1; i < platformCount; i++)
        {
            PlatformData previous = platforms[platforms.Count - 1];
            PlatformData next;

            if (!TryPickReachablePlatform(previous, i, platformCount, out next))
            {
                next = GetFallbackPlatform(previous, i, platformCount);
                Debug.LogWarning($"[CaveLevelGenerator] Failed to pick random reachable platform after {MaxNextPlatformAttempts} attempts. Using fallback for index={i}.");
            }

            platforms.Add(next);
            DrawPlatform(next);
        }

        return platforms;
    }

    private bool TryPickReachablePlatform(PlatformData previous, int index, int platformCount, out PlatformData platform)
    {
        for (int attempt = 0; attempt < MaxNextPlatformAttempts; attempt++)
        {
            int platformWidth = Random.Range(minPlatformWidth, maxPlatformWidth + 1);
            int platformY = safePlatformHeights[Random.Range(0, safePlatformHeights.Length)];

            int minStartX = previous.xStart + 1;
            int maxStartX = Mathf.Min(previous.XEnd + 5, width - platformWidth - 1);

            if (minStartX > maxStartX)
            {
                continue;
            }

            int platformXStart = Random.Range(minStartX, maxStartX + 1);
            PlatformData candidate = new PlatformData(platformXStart, platformY, platformWidth);

            if (IsReachableNextPlatform(previous, candidate) && HasRoomForRemainingPath(candidate, index, platformCount))
            {
                platform = candidate;
                return true;
            }
        }

        platform = default;
        return false;
    }

    private bool IsReachableNextPlatform(PlatformData previous, PlatformData candidate)
    {
        if (candidate.xStart <= previous.xStart)
        {
            return false;
        }

        if (candidate.XEnd <= previous.XEnd)
        {
            return false;
        }

        int emptyGap = candidate.xStart - previous.XEnd - 1;
        bool overlapsPrevious = emptyGap < 0;

        if (overlapsPrevious && candidate.y != previous.y)
        {
            return false;
        }

        if (emptyGap > 4)
        {
            return false;
        }

        if (emptyGap == 1)
        {
            return false;
        }

        if (Mathf.Abs(candidate.y - previous.y) > maxJumpHeight)
        {
            return false;
        }

        if (!IsSafePlatformHeight(candidate.y))
        {
            return false;
        }

        return candidate.y + RequiredClearanceAbovePlatform < height - 1;
    }

    private bool HasRoomForRemainingPath(PlatformData candidate, int index, int platformCount)
    {
        int remainingPlatforms = platformCount - index - 1;

        return candidate.XEnd + remainingPlatforms <= width - 2;
    }

    private PlatformData GetFallbackPlatform(PlatformData previous, int index, int platformCount)
    {
        int remainingPlatforms = platformCount - index - 1;
        int platformWidth = Mathf.Clamp(minPlatformWidth, 1, Mathf.Max(1, width - 2));
        int maxEndForThisPlatform = width - 2 - remainingPlatforms;
        int targetEnd = Mathf.Clamp(previous.XEnd + platformWidth, previous.XEnd + 1, maxEndForThisPlatform);
        int xStart = Mathf.Max(previous.xStart + 1, targetEnd - platformWidth + 1);

        if (xStart + platformWidth - 1 > maxEndForThisPlatform)
        {
            xStart = maxEndForThisPlatform - platformWidth + 1;
        }

        xStart = Mathf.Max(previous.xStart + 1, xStart);

        if (xStart - previous.XEnd - 1 == 1)
        {
            xStart -= 1;
        }

        int y = GetClosestSafeHeight(previous.y);

        return new PlatformData(xStart, y, platformWidth);
    }

    private int GetClosestSafeHeight(int targetY)
    {
        int bestY = safePlatformHeights[0];
        int bestDelta = Mathf.Abs(targetY - bestY);

        for (int i = 1; i < safePlatformHeights.Length; i++)
        {
            int candidateY = safePlatformHeights[i];
            int delta = Mathf.Abs(targetY - candidateY);

            if (delta < bestDelta && candidateY + RequiredClearanceAbovePlatform < height - 1)
            {
                bestY = candidateY;
                bestDelta = delta;
            }
        }

        return bestY;
    }

    private bool IsSafePlatformHeight(int y)
    {
        for (int i = 0; i < safePlatformHeights.Length; i++)
        {
            if (safePlatformHeights[i] == y)
            {
                return true;
            }
        }

        return false;
    }

    private void DrawPlatform(PlatformData platform)
    {
        for (int x = platform.xStart; x <= platform.XEnd; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, platform.y, 0), wallTile);

            for (int y = platform.y + 1; y <= platform.y + RequiredClearanceAbovePlatform && y < height - 1; y++)
            {
                wallTilemap.SetTile(new Vector3Int(x, y, 0), null);
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

        Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            prb.linearVelocity = Vector2.zero;
            prb.angularVelocity = 0f;
        }

        Object tileAtGround = wallTilemap.GetTile(groundCell);
        Debug.Log($"[CaveLevelGenerator] spawnCell={spawnCell}");
        Debug.Log($"[CaveLevelGenerator] groundCell={groundCell}");
        Debug.Log($"[CaveLevelGenerator] player world pos={spawnWorld}");
        Debug.Log($"[CaveLevelGenerator] wallTile at groundCell is null? {tileAtGround == null}");
    }

    private Vector3Int SpawnExitOnLastPlatform(PlatformData lastPlatform)
    {
        Vector3Int exitCell = new Vector3Int(lastPlatform.xStart + lastPlatform.width / 2, lastPlatform.y + 2, 0);
        Vector3 exitWorldPos = wallTilemap.GetCellCenterWorld(exitCell);

        if (exitPrefab == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Exit prefab not assigned. Skipping exit spawn.");
            Debug.Log($"[CaveLevelGenerator] Exit target cell={exitCell}, world pos={exitWorldPos}, last platform xStart={lastPlatform.xStart}, width={lastPlatform.width}, y={lastPlatform.y}");
            return exitCell;
        }

        currentExit = Instantiate(exitPrefab, exitWorldPos, Quaternion.identity, levelObjectsRoot);
        currentExit.name = "ExitPlaceholder";

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

        Debug.Log($"[CaveLevelGenerator] Exit spawned at cell={exitCell}, world pos={exitWorldPos}, last platform xStart={lastPlatform.xStart}, width={lastPlatform.width}, y={lastPlatform.y}");
        return exitCell;
    }

    private void SpawnCaveEnergyNodes(List<PlatformData> platforms, Vector3Int exitCell)
    {
        if (caveEnergyNodePrefab == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Cave energy node prefab not assigned. Skipping cave energy nodes.");
            return;
        }

        List<int> candidatePlatformIndices = new List<int>();
        for (int i = 1; i < platforms.Count; i++)
        {
            PlatformData platform = platforms[i];
            if (platform.width < 3)
            {
                continue;
            }

            if (platform.XEnd <= SpawnPlatformXStart + SpawnPlatformWidth + 1)
            {
                continue;
            }

            candidatePlatformIndices.Add(i);
        }

        Shuffle(candidatePlatformIndices);

        int targetNodeCount = Random.Range(minCaveEnergyNodes, maxCaveEnergyNodes + 1);
        targetNodeCount = Mathf.Clamp(targetNodeCount, 0, candidatePlatformIndices.Count);
        int spawnedCount = 0;

        for (int i = 0; i < candidatePlatformIndices.Count && spawnedCount < targetNodeCount; i++)
        {
            int platformIndex = candidatePlatformIndices[i];
            PlatformData platform = platforms[platformIndex];

            if (!TryGetEnergyNodeCell(platform, exitCell, out Vector3Int nodeCell))
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

            Debug.Log($"[CaveLevelGenerator] Cave energy node platform index={platformIndex}, cell={nodeCell}, world pos={worldPos}");
            spawnedCount++;
        }

        Debug.Log($"[CaveLevelGenerator] Spawned {spawnedCount} cave energy nodes.");
    }

    private bool TryGetEnergyNodeCell(PlatformData platform, Vector3Int exitCell, out Vector3Int nodeCell)
    {
        int minX = platform.xStart + 1;
        int maxX = platform.xStart + platform.width - 2;

        if (minX > maxX)
        {
            nodeCell = default;
            return false;
        }

        List<int> validXCells = new List<int>();
        for (int x = minX; x <= maxX; x++)
        {
            if (x == exitCell.x && platform.y + 2 == exitCell.y)
            {
                continue;
            }

            validXCells.Add(x);
        }

        if (validXCells.Count == 0)
        {
            nodeCell = default;
            return false;
        }

        int selectedX = validXCells[Random.Range(0, validXCells.Count)];
        nodeCell = new Vector3Int(selectedX, platform.y + 2, 0);
        return true;
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

    private void LogMainPath(List<PlatformData> platforms)
    {
        for (int i = 0; i < platforms.Count; i++)
        {
            PlatformData platform = platforms[i];
            Debug.Log($"[CaveLevelGenerator] MainPath index={i}, xStart={platform.xStart}, xEnd={platform.XEnd}, y={platform.y}, width={platform.width}");
        }
    }
}
