using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target; // Buraya Player objesini sürükleyeceksin
    
    [Header("Ayarlar")]
    public float smoothSpeed = 0.125f; // Takip yumuşaklığı (0 ile 1 arası)
    // Eğer kameranın oyuncuyu "anında" takip etmesini istersen bunu 1 yap.

    private void LateUpdate() // Kamera takibi için LateUpdate en iyisidir (titremeyi önler)
    {
        if (target == null) return;

        // Hedef pozisyon: Oyuncunun X ve Y'si, ama kameranın kendi Z'si (derinlik bozulmasın diye)
        Vector3 desiredPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
        
        // Mevcut pozisyondan hedef pozisyona yumuşakça kay
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        transform.position = smoothedPosition;
    }
}
