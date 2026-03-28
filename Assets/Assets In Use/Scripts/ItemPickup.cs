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
                // Burayı IsaacMovement olarak güncelledik!
                if (player.TryGetComponent<IsaacMovement>(out IsaacMovement movementController))
                {
                    // Not: Eğer IsaacMovement içindeki hız değişkeninizin adı 'speed' değilse 
                    // (örneğin 'moveSpeed' ise), aşağıdaki 'speed' yazısını da ona göre değiştirmelisiniz.
                    movementController.speed++; 
                }
                break;
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) {
            OnItemPickup(other.gameObject);
        }
    }

}
