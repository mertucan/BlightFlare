using UnityEngine;

public class HotBombs : MonoBehaviour, IItem
{
    [Header("Hot Bomb Ayarları")]
    public GameObject fireHazardPrefab;   // Patlama sonrası ateş alanı prefabı

    public void Pickup(GameObject player)
    {
        var bombCtrl = player.GetComponent<BombController>();
        if (bombCtrl == null)
        {
            Debug.LogWarning("[HotBomb] Player'da BombController bulunamadı!");
            return;
        }

        bombCtrl.hasHotBomb       = true;
        bombCtrl.fireHazardPrefab = fireHazardPrefab;

        Debug.Log("[HotBomb] Aktif.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Pickup(other.gameObject);
    }
}