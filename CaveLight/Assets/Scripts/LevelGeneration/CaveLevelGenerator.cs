using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveLevelGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap wallTilemap;
    public Tilemap groundTilemap;

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
    public Vector2Int playerSpawn = new Vector2Int(2, 2);

    [Header("Exit & Progression")]
    public GameObject exitPrefab;
    public Transform levelObjectsRoot;
    private int caveAmount = 0;
    private GameObject currentExit;

    void Start()
    {
        caveAmount = 1;
        GenerateLevel();
        Debug.Log("Enter cave: 1");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            GenerateLevel();
            Debug.Log("[CaveLevelGenerator] Regenerated map on R key.");
        }
    }

    public void GoToNextCave()
    {
        caveAmount += 1;
        Debug.Log("Enter cave: " + caveAmount);
        GenerateLevel();
    }

    public void GenerateLevel()
    {
        if (wallTilemap == null || groundTilemap == null || wallTile == null || groundTile == null)
        {
            Debug.LogWarning("[CaveLevelGenerator] Missing references. Please assign Tilemaps and Tiles in the Inspector.");
            return;
        }

        // Clear existing tiles
        wallTilemap.ClearAllTiles();
        groundTilemap.ClearAllTiles();

        // Clean up old exit
        if (currentExit != null)
        {
            Destroy(currentExit);
            currentExit = null;
        }

        // Outer ring of walls
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                }
            }
        }

        // Bottom floor (redundant with outer ring but ensures continuous floor)
        for (int x = 1; x < width - 1; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, 0, 0), wallTile);
        }

        // Random short platforms in the middle (placed on wallTilemap so collision is handled there)
        int platformCount = Random.Range(3, 6); // 3 to 5
        for (int i = 0; i < platformCount; i++)
        {
            int platWidth = Random.Range(3, 7); // 3 to 6
            int minY = 2;
            int maxY = Mathf.Max(2, height - 3);
            int y = Random.Range(minY, maxY + 1);

            int startMin = 1;
            int startMax = width - platWidth - 1; // ensure within inner bounds
            if (startMax < startMin) startMax = startMin;
            int xStart = Random.Range(startMin, startMax + 1);

            for (int x = xStart; x < xStart + platWidth; x++)
            {
                // place platform tiles on wallTilemap so they act as solid collision
                wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
            }
        }

        // Force a continuous safe floor segment under the player area: x=1..5 at y=1
        int safeFloorY = 1;
        for (int x = 1; x <= 5; x++)
        {
            wallTilemap.SetTile(new Vector3Int(x, safeFloorY, 0), wallTile);
        }

        // Define spawn and ground cells explicitly per requirements
        Vector3Int spawnCell = new Vector3Int(3, 3, 0);
        Vector3Int groundCell = new Vector3Int(3, 1, 0);

        // Ensure the groundCell has a wall tile (solid collision)
        wallTilemap.SetTile(groundCell, wallTile);

        // Place player at spawnCell center using Tilemap conversion and offset upward
        if (player != null)
        {
            Vector3 cellCenter = wallTilemap.GetCellCenterWorld(spawnCell);

            // compute half height from BoxCollider2D if available
            BoxCollider2D box = player.GetComponent<BoxCollider2D>();
            float halfHeight = 0f;
            if (box != null)
            {
                halfHeight = box.size.y * 0.5f * Mathf.Abs(player.localScale.y);
            }
            else
            {
                Collider2D col = player.GetComponent<Collider2D>();
                if (col != null)
                    halfHeight = col.bounds.size.y * 0.5f;
            }

            float yOffset = halfHeight + 0.02f;
            Vector3 spawnWorld = new Vector3(cellCenter.x, cellCenter.y + yOffset, player.position.z);

            player.position = spawnWorld;

            // Reset player's Rigidbody2D velocities
            Rigidbody2D prb = player.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                prb.linearVelocity = Vector2.zero;
                prb.angularVelocity = 0f;
            }

            // Debug info
            Object tileAtGround = wallTilemap.GetTile(groundCell);
            Debug.Log($"[CaveLevelGenerator] spawnCell={spawnCell}");
            Debug.Log($"[CaveLevelGenerator] groundCell={groundCell}");
            Debug.Log($"[CaveLevelGenerator] player world pos={spawnWorld}");
            Debug.Log($"[CaveLevelGenerator] wallTile at groundCell is null? { (tileAtGround == null) }");
        }

        Debug.Log("[CaveLevelGenerator] Map generation completed.");

        // Spawn exit at right side of the map
        if (exitPrefab != null)
        {
            Vector3Int exitCell = new Vector3Int(width - 3, 3, 0);

            // Force a safe platform under the exit
            int exitFloorY = 1;
            for (int x = width - 5; x < width - 1; x++)
            {
                wallTilemap.SetTile(new Vector3Int(x, exitFloorY, 0), wallTile);
            }

            // Force the floor directly under the exit cell
            wallTilemap.SetTile(new Vector3Int(exitCell.x, exitFloorY, 0), wallTile);

            // Calculate world position for exit
            Vector3 exitWorldPos = wallTilemap.GetCellCenterWorld(exitCell);

            // Instantiate exit prefab
            currentExit = Instantiate(exitPrefab, exitWorldPos, Quaternion.identity, levelObjectsRoot);
            currentExit.name = "ExitPlaceholder";

            // Add or retrieve CaveExitTrigger component
            CaveExitTrigger trigger = currentExit.GetComponent<CaveExitTrigger>();
            if (trigger == null)
            {
                trigger = currentExit.AddComponent<CaveExitTrigger>();
            }
            trigger.SetLevelGenerator(this);

            Debug.Log($"[CaveLevelGenerator] Exit spawned at cell {exitCell}, world pos {exitWorldPos}");
        }
        else
        {
            Debug.LogWarning("[CaveLevelGenerator] Exit prefab not assigned. Skipping exit spawn.");
        }
    }
}
