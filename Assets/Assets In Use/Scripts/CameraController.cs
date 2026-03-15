using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Ayarlar")]
    public float smoothTime = 0.05f;

    [Header("Room Snap")]
    public bool roomSnapping = true;

    private bool hasSnapTarget;
    private bool followInRoom;
    private bool isTransitioning;
    private Vector2 roomCenter;
    private Vector2 roomHalfSize;
    private Camera cam;
    private Vector3 smoothVelocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (target != null && !hasSnapTarget)
        {
            transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
        }
    }

    private void LateUpdate()
    {
        if (isTransitioning) return;

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
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, smoothTime);
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

    public void SlideToRoom(Vector2 toCenter, Vector2 halfSize, bool isLargeRoom, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(SlideRoutine(toCenter, halfSize, isLargeRoom, duration));
    }

    private IEnumerator SlideRoutine(Vector2 toCenter, Vector2 halfSize, bool isLargeRoom, float duration)
    {
        isTransitioning = true;

        Vector3 from = transform.position;
        Vector3 to = new Vector3(toCenter.x, toCenter.y, transform.position.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        roomCenter = toCenter;
        roomHalfSize = halfSize;
        followInRoom = isLargeRoom;
        hasSnapTarget = true;
        smoothVelocity = Vector3.zero;
        transform.position = to;

        isTransitioning = false;
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
