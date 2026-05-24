using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class CaveBackgroundFitter : MonoBehaviour
{
    public Camera targetCamera;
    public SpriteRenderer spriteRenderer;
    public float backgroundZ = 10f;
    public int sortingOrder = -100;

    private Vector3 lastCameraPosition;
    private float lastOrthographicSize = -1f;
    private float lastAspect = -1f;

    void Reset()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        targetCamera = Camera.main;
        ConfigureRenderer();
    }

    void Awake()
    {
        ResolveReferences();
        ConfigureRenderer();
    }

    void Start()
    {
        FitToCamera();
    }

    void LateUpdate()
    {
        ResolveReferences();
        if (CameraStateChanged())
        {
            FitToCamera();
        }
    }

    public void FitToCamera()
    {
        ResolveReferences();
        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        targetCamera.orthographic = true;

        float visibleHeight = targetCamera.orthographicSize * 2f;
        float visibleWidth = visibleHeight * targetCamera.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scaleX = visibleWidth / spriteSize.x;
        float scaleY = visibleHeight / spriteSize.y;
        float scale = Mathf.Max(scaleX, scaleY);

        transform.position = new Vector3(targetCamera.transform.position.x, targetCamera.transform.position.y, backgroundZ);
        transform.localScale = new Vector3(scale, scale, 1f);
        ConfigureRenderer();
        StoreCameraState();
    }

    private void ResolveReferences()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void ConfigureRenderer()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private bool CameraStateChanged()
    {
        if (targetCamera == null)
        {
            return false;
        }

        float aspect = targetCamera.aspect;
        return targetCamera.transform.position != lastCameraPosition
            || !Mathf.Approximately(targetCamera.orthographicSize, lastOrthographicSize)
            || !Mathf.Approximately(aspect, lastAspect);
    }

    private void StoreCameraState()
    {
        if (targetCamera == null)
        {
            return;
        }

        lastCameraPosition = targetCamera.transform.position;
        lastOrthographicSize = targetCamera.orthographicSize;
        lastAspect = targetCamera.aspect;
    }
}
