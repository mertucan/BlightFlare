using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType
    {
        ExtraBomb,
        BlastRadius,
        SpeedIncrease,
    }

    public ItemType type;

    [Header("Sounds")]
    public AudioClip[] pickupClips;
    public int selectedPickupClip = 0;

    private void OnItemPickup(GameObject player)
    {
        switch (type)
        {
            case ItemType.ExtraBomb:
                if (player.TryGetComponent<BombController>(out BombController bombController))
                {
                    bombController.AddBomb();
                }
                break;

            case ItemType.BlastRadius:
                if (player.TryGetComponent<BombController>(out BombController bombRadiusController))
                {
                    bombRadiusController.explosionRadius++;
                }
                break;

            case ItemType.SpeedIncrease:
                if (player.TryGetComponent<IsaacMovement>(out IsaacMovement movementController))
                {
                    movementController.speed++;
                }
                break;
        }

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

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            OnItemPickup(other.gameObject);
        }
    }
}