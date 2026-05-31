using UnityEngine;

/// <summary>
/// Bir shop item'ına ekle. Inspector'dan fiyatı ve item tipini ayarla.
/// Oyuncu collider'ı bu objenin trigger collider'ıyla çakışırsa satın alma tetiklenir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopItem : MonoBehaviour
{
    [Header("Item Ayarları")]
    public string itemName = "Item";
    public int cost = 5;
    public ItemPickup.ItemType itemType = ItemPickup.ItemType.Heart;

    [Header("Key / Penny miktarı")]
    [Tooltip("Sadece Key veya Penny tipinde geçerli")]
    public int amount = 1;

    [Header("Görseller")]
    [Tooltip("Fiyat etiketi için isteğe bağlı TextMeshPro referansı")]
    public TMPro.TextMeshProUGUI priceLabel;

    [Header("Sounds")]
    public AudioClip purchaseClip;

    private bool _purchased = false;

    private void Start()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (priceLabel != null)
            priceLabel.text = cost.ToString() + "¢";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_purchased) return;
        if (!other.CompareTag("Player")) return;

        TryPurchase(other.gameObject);
    }

    private void TryPurchase(GameObject player)
    {
        var inventory = PlayerInventory.instance;
        if (inventory == null)
        {
            Debug.LogWarning("[ShopItem] PlayerInventory bulunamadı!");
            return;
        }

        if (inventory.pennies < cost)
        {
            Debug.Log($"[ShopItem] Yetersiz para! Gereken: {cost}, Mevcut: {inventory.pennies}");
            return;
        }

        inventory.AddPenny(-cost);
        ApplyItemEffect(player);

        _purchased = true;

        if (purchaseClip != null)
            AudioSource.PlayClipAtPoint(purchaseClip, transform.position);

        Destroy(gameObject);
    }

    private void ApplyItemEffect(GameObject player)
    {
        switch (itemType)
        {
            case ItemPickup.ItemType.Heart:
                if (player.TryGetComponent<PlayerHealth>(out PlayerHealth ph))
                    ph.Heal(2);
                break;

            case ItemPickup.ItemType.HalfHeart:
                if (player.TryGetComponent<PlayerHealth>(out PlayerHealth phHalf))
                    phHalf.Heal(1);
                break;

            case ItemPickup.ItemType.Key:
                PlayerInventory.instance.AddKey(amount);
                break;

            case ItemPickup.ItemType.Penny:
                PlayerInventory.instance.AddPenny(amount);
                break;

            case ItemPickup.ItemType.ExtraBomb:
                if (player.TryGetComponent<BombController>(out BombController bc))
                    bc.AddBomb();
                break;

            case ItemPickup.ItemType.BlastRadius:
                if (player.TryGetComponent<BombController>(out BombController bcr))
                    if (bcr.explosionRadius < 3)
                        bcr.explosionRadius++;
                break;

            case ItemPickup.ItemType.SpeedIncrease:
                if (player.TryGetComponent<IsaacMovement>(out IsaacMovement mv))
                {
                    mv.speed = Mathf.Min(mv.speed + 1f, IsaacMovement.MaxNormalSpeed);
                    var taurus = player.GetComponent<Taurus>();
                    if (taurus != null) taurus.OnSpeedPickup(mv.speed);
                }
                break;
        }

        Debug.Log($"[ShopItem] '{itemName}' satın alındı ({cost}¢). Tip: {itemType}");
    }
}
