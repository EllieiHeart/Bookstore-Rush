using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;    // drag your Player here in Inspector

    [Header("Settings")]
    public float smoothSpeed = 5f;  // how quickly the camera catches up
    public Vector3 offset;          // optional offset, e.g. (0,0,-10) for 2D

    void LateUpdate()
    {
        if (target == null) return;

        // Desired position (player + offset)
        Vector3 desiredPos = target.position + offset;

        // Smoothly interpolate camera position
        Vector3 smoothedPos = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);

        transform.position = smoothedPos;
    }
}
