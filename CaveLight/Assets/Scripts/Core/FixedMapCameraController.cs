using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class FixedMapCameraController : MonoBehaviour
{
    public float padding = 0.2f;
    public float minOrthographicSize = 5f;
    public float maxOrthographicSize = 20f;
    public float cameraZ = -10f;

    private Camera targetCamera;

    void Awake()
    {
        targetCamera = GetComponent<Camera>();
        DisableFollowComponent();
        ConfigureOrthographicCamera();
    }

    void OnValidate()
    {
        padding = Mathf.Max(0f, padding);
        minOrthographicSize = Mathf.Max(0.1f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
        cameraZ = -10f;
    }

    public void FitToTilemap(Tilemap wallTilemap)
    {
        FitToTilemap(wallTilemap, padding);
    }

    public void FitToTilemap(Tilemap wallTilemap, float paddingOverride)
    {
        if (wallTilemap == null)
        {
            Debug.LogWarning("[FixedMapCameraController] Missing wallTilemap. Camera fit skipped.");
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("[FixedMapCameraController] Missing Camera component. Camera fit skipped.");
            return;
        }

        ConfigureOrthographicCamera();

        wallTilemap.CompressBounds();
        Bounds localBounds = wallTilemap.localBounds;
        if (localBounds.size.x <= 0f || localBounds.size.y <= 0f)
        {
            Debug.LogWarning("[FixedMapCameraController] WallTilemap has empty bounds. Camera fit skipped.");
            return;
        }

        Vector3 center = wallTilemap.transform.TransformPoint(localBounds.center);
        Vector3 worldSize = wallTilemap.transform.TransformVector(localBounds.size);
        float mapWidth = Mathf.Abs(worldSize.x);
        float mapHeight = Mathf.Abs(worldSize.y);
        float aspect = GetCameraAspect(targetCamera);
        float safePadding = Mathf.Max(0f, paddingOverride);

        float sizeByHeight = mapHeight * 0.5f + safePadding;
        float sizeByWidth = mapWidth / (2f * aspect) + safePadding;
        float orthographicSize = Mathf.Clamp(Mathf.Max(sizeByHeight, sizeByWidth), minOrthographicSize, maxOrthographicSize);

        targetCamera.orthographicSize = orthographicSize;
        transform.position = new Vector3(center.x, center.y, cameraZ);

        float mapAspect = mapHeight > 0f ? mapWidth / mapHeight : 0f;
        bool nearSixteenNine = Mathf.Abs(mapAspect - 16f / 9f) < 0.05f;
        Debug.Log($"[FixedMapCameraController] Fit camera to map. center={center}, mapWidth={mapWidth}, mapHeight={mapHeight}, mapAspect={mapAspect:0.000}, near16x9={nearSixteenNine}, padding={safePadding}, cameraAspect={aspect:0.000}, orthographicSize={orthographicSize}, cameraPosition={transform.position}, tilemapBounds={localBounds}");
    }

    private void ConfigureOrthographicCamera()
    {
        if (targetCamera == null)
        {
            return;
        }

        targetCamera.orthographic = true;
    }

    private void DisableFollowComponent()
    {
        CameraFollow2D follow = GetComponent<CameraFollow2D>();
        if (follow != null && follow.enabled)
        {
            follow.enabled = false;
            Debug.Log("[FixedMapCameraController] Disabled CameraFollow2D on Main Camera.");
        }
    }

    private static float GetCameraAspect(Camera camera)
    {
        if (camera.pixelHeight > 0)
        {
            return Mathf.Max(0.01f, camera.pixelWidth / (float)camera.pixelHeight);
        }

        if (Screen.height > 0)
        {
            return Mathf.Max(0.01f, Screen.width / (float)Screen.height);
        }

        return 16f / 9f;
    }
}
