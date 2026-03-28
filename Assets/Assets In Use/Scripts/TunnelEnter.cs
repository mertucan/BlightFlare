using UnityEngine;

public class TunnelEnter : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    // Buraya hangi sahneye/odaya geçeceğini eklersin
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        
        // Geçiş işlemi buraya — örneğin:
        // SceneManager.LoadScene("NextScene");
        // veya RoomTransitionManager'ına bir metot çağırırsın
        Debug.Log("Tünele girildi!");
    }
}