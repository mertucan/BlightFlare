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
        Heart,
        HalfHeart,
    }

    public ItemType type;

    [Header("Key / Penny Settings")]
    [Tooltip("Key veya Penny tipinde kaç adet eklensin?")]
    public int amount = 1;

    [Header("Sounds")]
    public AudioClip[] pickupClips;
    public int selectedPickupClip = 0;

    [Header("Physics (Heart push)")]
    public float pushForce = 4f;
    public float friction   = 3f;   // itildikten sonra yavaşlama katsayısı

    private Rigidbody2D _rb;
    private bool _isHeart => type == ItemType.Heart || type == ItemType.HalfHeart;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (_isHeart && _rb != null)
        {
            _rb.gravityScale   = 0f;
            _rb.linearDamping  = friction;   // sürtünme etkisi
            _rb.angularDamping = 10f;        // dönmeyi engelle
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    // ── Trigger: sadece Player ile (pickup) ─────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (_isHeart)
            TryPickupHeart(other.gameObject);
        else
            OnItemPickup(other.gameObject);
    }

    // ── Collision: Wall / Door → sekme / dur ────────────────────────────
    // Kalp objesinin Collider2D'si:
    //   • IsTrigger = FALSE  olan bir Collider2D olmalı (duvarla fiziksel çarpışma için)
    //   • IsTrigger = TRUE   olan bir Collider2D Player pickup için
    // Yani prefab'a 2 ayrı Collider2D ekle:
    //   1) CircleCollider2D  – IsTrigger: true  (pickup)
    //   2) CircleCollider2D  – IsTrigger: false (duvar/kapı engeli) — biraz daha küçük radius
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (!_isHeart || _rb == null) return;

        if (col.gameObject.CompareTag("Wall") || col.gameObject.CompareTag("Door"))
        {
            // Duvardan yansıt: mevcut hızın normal doğrultusundaki bileşenini ters çevir
            Vector2 normal    = col.contacts[0].normal;
            Vector2 reflected = Vector2.Reflect(_rb.linearVelocity, normal);
            _rb.linearVelocity = reflected * 0.4f;   // 0.4 → sekme enerjisi (0 = dur, 1 = tam sekme)
        }
    }

    // ────────────────────────────────────────────────────────────────────

    private void TryPickupHeart(GameObject player)
    {
        if (!player.TryGetComponent<PlayerHealth>(out PlayerHealth ph)) return;

        if (ph.currentHearts >= ph.maxHearts)
        {
            PushAwayFrom(player);
            return;
        }

        int healsFor  = (type == ItemType.Heart) ? 2 : 1;
        int missing   = ph.maxHearts - ph.currentHearts;
        int actualHeal = Mathf.Min(healsFor, missing);

        ph.Heal(actualHeal);
        PlayPickupSound();
        Destroy(gameObject);
    }

    private void PushAwayFrom(GameObject player)
    {
        if (_rb == null) return;

        Vector2 dir = (transform.position - player.transform.position).normalized;
        if (dir == Vector2.zero) dir = Vector2.right;

        _rb.linearVelocity = Vector2.zero;               // önceki hızı sıfırla
        _rb.AddForce(dir * pushForce, ForceMode2D.Impulse);
    }

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
            AudioSource.PlayClipAtPoint(pickupClips[selectedPickupClip], transform.position);
        }
        else
        {
            Debug.LogWarning("Ses çalamadı! Clips: " +
                (pickupClips == null ? "null" : pickupClips.Length.ToString()) +
                " Index: " + selectedPickupClip);
        }
    }
}