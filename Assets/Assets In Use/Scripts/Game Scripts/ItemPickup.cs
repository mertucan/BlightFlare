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

    [Header("Physics")]
    public float friction = 3f;

    [Header("Knockback (Bomb)")]
    [Tooltip("Bomba knockback'i için bu tipler etkilenir: Key, Penny, Heart, HalfHeart")]
    public float knockbackForce = 6f;

    private Rigidbody2D  _rb;
    private Collider2D   _col;
    private bool         _isMoving = false; // knockback/push aktif mi?
    private bool         _isPickedUp = false;

    private bool _isHeart => type == ItemType.Heart || type == ItemType.HalfHeart;

    private bool _isKnockbackable =>
        type == ItemType.Key      ||
        type == ItemType.Penny    ||
        type == ItemType.Heart    ||
        type == ItemType.HalfHeart;

    private void Awake()
    {
        _col = GetComponent<Collider2D>();
        _rb  = GetComponent<Rigidbody2D>();

        if (_isHeart && _rb != null)
        {
            _rb.gravityScale   = 0f;
            _rb.linearDamping  = friction;
            _rb.angularDamping = 10f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if ((type == ItemType.Key || type == ItemType.Penny) && _rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody2D>();
            _rb.gravityScale   = 0f;
            _rb.linearDamping  = friction;
            _rb.angularDamping = 10f;
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        else if ((type == ItemType.Key || type == ItemType.Penny) && _rb != null)
        {
            _rb.gravityScale   = 0f;
            _rb.linearDamping  = friction;
            _rb.angularDamping = 10f;
            _rb.freezeRotation = true;
        }
    }

    private void Update()
    {
        // Hareket durduğunda collider'ı tekrar trigger'a çevir (pickup alınabilsin)
        if (_isMoving && _rb != null && _rb.linearVelocity.magnitude < 0.05f)
        {
            _isMoving = false;
            if (_col != null) _col.isTrigger = true;
        }
    }

    /// <summary>
    /// Pickup'ı <paramref name="explosionPos"/> patlamadan UZAĞA fırlatır.
    /// </summary>
    public void ApplyKnockbackToward(Vector2 explosionPos)
    {
        if (!_isKnockbackable) return;
        if (_rb == null) return;

        // Patlamadan uzağa doğru (tersine)
        Vector2 dir = ((Vector2)transform.position - explosionPos).normalized;
        if (dir == Vector2.zero) dir = Random.insideUnitCircle.normalized;

        // Hareket sırasında collider'ı solid yap → duvarlarla çarpışır
        if (_col != null) _col.isTrigger = false;
        _isMoving = true;

        _rb.linearVelocity = Vector2.zero;
        _rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
    }

    // ── Trigger: sadece Player ile (pickup) ─────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandlePlayerPickup(other.gameObject);
    }

    private void TryHandlePlayerPickup(GameObject player)
    {
        if (_isPickedUp) return;
        if (!player.CompareTag("Player")) return;

        StopMovingForPlayerPickup();

        if (_isHeart)
            TryPickupHeart(player);
        else
            OnItemPickup(player);
    }

    private void StopMovingForPlayerPickup()
    {
        _isMoving = false;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;
        }

        if (_col != null)
            _col.isTrigger = true;
    }

    // ── Collision: Wall / Door → sek ────────────────────────────────────
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (_rb == null) return;
        if (!_isKnockbackable) return;

        if (col.gameObject.CompareTag("Player"))
        {
            TryHandlePlayerPickup(col.gameObject);
            return;
        }

        if (col.gameObject.CompareTag("Wall") || col.gameObject.CompareTag("Door"))
        {
            Vector2 normal    = col.contacts[0].normal;
            Vector2 reflected = Vector2.Reflect(_rb.linearVelocity, normal);

            if (reflected.magnitude < 1.5f)
                reflected = normal * 1.5f;

            _rb.linearVelocity = reflected * 0.6f;
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (_rb == null) return;
        if (!_isKnockbackable) return;

        if (col.gameObject.CompareTag("Player"))
        {
            TryHandlePlayerPickup(col.gameObject);
            return;
        }

        if (col.gameObject.CompareTag("Wall") || col.gameObject.CompareTag("Door"))
        {
            Vector2 normal = col.contacts[0].normal;
            _rb.linearVelocity += normal * 2f;
        }
    }

    // ────────────────────────────────────────────────────────────────────

    private void TryPickupHeart(GameObject player)
    {
        if (!player.TryGetComponent<PlayerHealth>(out PlayerHealth ph)) return;

        if (ph.currentHearts >= ph.maxHearts)
        {
            return;
        }

        int healsFor   = (type == ItemType.Heart) ? 2 : 1;
        int missing    = ph.maxHearts - ph.currentHearts;
        int actualHeal = Mathf.Min(healsFor, missing);

        ph.Heal(actualHeal);
        _isPickedUp = true;
        PlayPickupSound();
        Destroy(gameObject);
    }

    private void OnItemPickup(GameObject player)
    {
        _isPickedUp = true;

        switch (type)
        {
            case ItemType.ExtraBomb:
                if (player.TryGetComponent<BombController>(out BombController bombController))
                    bombController.AddBomb();
                break;

            case ItemType.BlastRadius:
                if (player.TryGetComponent<BombController>(out BombController bombRadiusController))
                {
                    if (bombRadiusController.explosionRadius < 3)
                        bombRadiusController.explosionRadius++;
                }
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
