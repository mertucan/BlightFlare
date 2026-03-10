using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Ayarlar")]
    public float smoothSpeed = 0.125f;
    public float smoothTime = 0.05f;

    [Header("Room Snap")]
    public bool roomSnapping = true;

    private bool hasSnapTarget;
    private bool followInRoom;
    private Vector2 roomCenter;
    private Vector2 roomHalfSize;
    private Camera cam;
    private Vector3 smoothVelocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (roomSnapping && hasSnapTarget)
        {
            if (followInRoom && target != null)
            {
                Vector3 targetPos = GetClampedCameraPosition(target.position);
                transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref smoothVelocity, smoothTime);
            }
            return;
        }

        if (target != null)
        {
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

    public void SnapToRoom(Vector2 center, Vector2 halfSize, bool isLargeRoom)
    {
        roomCenter = center;
        roomHalfSize = halfSize;
        followInRoom = isLargeRoom;
        hasSnapTarget = true;
        smoothVelocity = Vector3.zero;

        if (isLargeRoom && target != null)
        {
            transform.position = GetClampedCameraPosition(target.position);
        }
        else
        {
            transform.position = new Vector3(center.x, center.y, transform.position.z);
        }
    }

    private Vector3 GetClampedCameraPosition(Vector3 targetPos)
    {
        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = camHalfHeight * cam.aspect;

        float minX = roomCenter.x - roomHalfSize.x + camHalfWidth;
        float maxX = roomCenter.x + roomHalfSize.x - camHalfWidth;
        float x = (minX >= maxX) ? roomCenter.x : Mathf.Clamp(targetPos.x, minX, maxX);

        float minY = roomCenter.y - roomHalfSize.y + camHalfHeight;
        float maxY = roomCenter.y + roomHalfSize.y - camHalfHeight;
        float y = (minY >= maxY) ? roomCenter.y : Mathf.Clamp(targetPos.y, minY, maxY);

        return new Vector3(x, y, transform.position.z);
    }
}
