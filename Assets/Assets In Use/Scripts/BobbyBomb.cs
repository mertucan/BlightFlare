using UnityEngine;

public class BobbyBomb : MonoBehaviour, IItem
{
    [Header("Bobby-Bomb Ayarları")]
    public float moveSpeed     = 2.5f;
    public float searchRadius  = 5f;
    public float fuseTime      = 3f;
    public float searchDelay   = 0.15f;
    public int   searchRetries = 5;
    public float retryInterval = 0.1f;

    [Header("UI Icon")]
    public Sprite iconSprite;           // ← EKLE

    public void Pickup(GameObject player)
    {
        var bombCtrl = player.GetComponent<BombController>();
        if (bombCtrl == null)
        {
            Debug.LogWarning("[BobbyBomb] Player'da BombController bulunamadı!");
            return;
        }

        bombCtrl.hasBobbyBomb       = true;
        bombCtrl.bobbyMoveSpeed     = moveSpeed;
        bombCtrl.bobbySearchRadius  = searchRadius;
        bombCtrl.bobbySearchDelay   = searchDelay;
        bombCtrl.bobbySearchRetries = searchRetries;
        bombCtrl.bobbyRetryInterval = retryInterval;

        Debug.Log($"[BobbyBomb] Aktif. searchRadius={searchRadius}");
        ItemIconUI.Instance?.AddItemIcon(iconSprite, "BobbyBomb");
    }
}