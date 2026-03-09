using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Ayarlar")]
    public float smoothSpeed = 0.125f;

    [Header("Room Snap")]
    public bool roomSnapping = true;
    public float snapSpeed = 10f;

    private Vector3 snapTarget;
    private bool hasSnapTarget;

    private void LateUpdate()
    {
        if (roomSnapping && hasSnapTarget)
        {
            transform.position = Vector3.Lerp(transform.position, snapTarget, snapSpeed * Time.deltaTime);
        }
        else if (target != null)
        {
            Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

    public void SnapToPosition(Vector2 position)
    {
        snapTarget = new Vector3(position.x, position.y, transform.position.z);
        hasSnapTarget = true;
    }
}
