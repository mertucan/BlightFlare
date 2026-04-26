using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType
    {
        ExtraBomb,
        BlastRadius,
        SpeedIncrease,
        Key,
        Penny,
    }

    public ItemType type;

    [Header("Key / Penny Settings")]
    [Tooltip("Key veya Penny tipinde kaç adet eklensin?")]
    public int amount = 1;

    [Header("Sounds")]
    public AudioClip[] pickupClips;
    public int selectedPickupClip = 0;

    private void OnItemPickup(GameObject player)
    {
        switch (type)
        {
            case ItemType.ExtraBomb:
                if (player.TryGetComponent<BombController>(out BombController bombController))
                    bombController.AddBomb();
                break;

            case ItemType.BlastRadius:
                if (player.TryGetComponent<BombController>(out BombController bombRadiusController))
                    bombRadiusController.explosionRadius++;
                break;

            case ItemType.SpeedIncrease:
                if (player.TryGetComponent<IsaacMovement>(out IsaacMovement movementController))
                {
                    movementController.speed++;
                    var taurus = player.GetComponent<Taurus>();
                    if (taurus != null)
                        taurus.OnSpeedPickup(movementController.speed);
                }
                break;

            // ─── Yeni tipler ───────────────────────────────────────────────
            case ItemType.Key:
                if (player.TryGetComponent<PlayerInventory>(out PlayerInventory invKey))
                    invKey.AddKey(amount);
                else
                    Debug.LogWarning("PlayerInventory bulunamadı! Oyuncu objesine ekleyin.");
                break;

            case ItemType.Penny:
                if (player.TryGetComponent<PlayerInventory>(out PlayerInventory invPenny))
                    invPenny.AddPenny(amount);
                else
                    Debug.LogWarning("PlayerInventory bulunamadı! Oyuncu objesine ekleyin.");
                break;
        }

        PlayPickupSound();
        Destroy(gameObject);
    }

    private void PlayPickupSound()
    {
        if (pickupClips != null && pickupClips.Length > 0 &&
            selectedPickupClip < pickupClips.Length &&
            pickupClips[selectedPickupClip] != null)
        {
            Debug.Log("Ses çalınıyor: " + pickupClips[selectedPickupClip].name);
            AudioSource.PlayClipAtPoint(pickupClips[selectedPickupClip], transform.position);
        }
        else
        {
            Debug.LogWarning("Ses çalamadı! Clips: " +
                (pickupClips == null ? "null" : pickupClips.Length.ToString()) +
                " Index: " + selectedPickupClip);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            OnItemPickup(other.gameObject);
    }
}