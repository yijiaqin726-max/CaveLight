using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [SerializeField] public Transform target;
    [SerializeField] public float smoothTime = 0.15f;
    [SerializeField] public Vector3 offset = new Vector3(0f, 1f, 0f);

    private Vector3 velocity = Vector3.zero;
    private bool targetWarningLogged = false;

    void LateUpdate()
    {
        if (target == null)
        {
            if (!targetWarningLogged)
            {
                Debug.LogWarning("[CameraFollow2D] Target is not assigned. Assign PlayerPlaceholder Transform in the Inspector.");
                targetWarningLogged = true;
            }
            return;
        }

        // Calculate desired position with offset
        Vector3 desiredPos = target.position + offset;
        
        // Keep camera's Z coordinate unchanged (typically -10)
        desiredPos.z = transform.position.z;

        // Smoothly interpolate camera position using SmoothDamp
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, smoothTime);
    }
}
