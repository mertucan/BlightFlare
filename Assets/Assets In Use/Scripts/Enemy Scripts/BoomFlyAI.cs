using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binding of Isaac — Boomfly AI
/// - Çapraz yönlerde hareket eder (DOF_AI'dan alınan mekanik)
/// - Oyuncuya değince TNT tarzı patlama yapar ve ölür
/// - Bombayla öldürülünce de aynı patlama tetiklenir
/// - Animatör yok: iki sprite arasında ping-pong yapar
/// - Patlama: TNT.SpawnExplosionTrigger mantığıyla Explosion layer trigger'ı açar
/// </summary>
public class BoomFlyAI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    // Inspector alanları
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Sprite Animasyonu (Animator yok)")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite sprite1;
    [SerializeField] private Sprite sprite2;
    [SerializeField] private float spriteSwitchInterval = 0.15f;   // saniye başına frame

    [Header("Can & Hasar")]
    [SerializeField] private int    bombsToKill     = 3;
    [SerializeField] private float  knockbackForce  = 5f;
    [SerializeField] private float  hitFlashDuration = 0.15f;

    [Header("Patlama")]
    [SerializeField] private float  explosionRadius          = 2.5f;
    [SerializeField] private float  explosionTriggerDuration = 0.45f;
    [SerializeField] private Sprite[] explosionFrames;            // TNT'deki gibi
    [SerializeField] private float    explosionFPS             = 12f;
    [SerializeField] private Sprite   afterExplosionSprite;       // kül sprite (isteğe bağlı)
    [SerializeField] private float    afterExplosionLifetime    = 0f;
    [SerializeField] private GameObject explosionVFXPrefab;       // partikül efekti (isteğe bağlı)

    [Header("Ses")]
    [SerializeField] private AudioClip[] explosionClips;
    [SerializeField] private int         selectedExplosionClip = 0;

    [Header("Tag'ler")]
    [SerializeField] private string playerTag = "Player";

    [Header("Layer'lar")]
    [SerializeField] private string enemyLayerName = "Enemy";

    // ─────────────────────────────────────────────────────────────────────────
    // Private değişkenler
    // ─────────────────────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private Collider2D  col;

    private Vector2  moveDirection;
    private bool     isActivated  = false;
    private bool     isDead       = false;
    private int      currentBombHits = 0;

    private Color    originalColor;
    private Coroutine flashRoutine;

    private int enemyLayer = -1;

    // ─────────────────────────────────────────────────────────────────────────
    // Unity yaşam döngüsü
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        enemyLayer = LayerMask.NameToLayer(enemyLayerName);
    }

    private void Start()
    {
        // DOF_AI'daki çapraz hareket mekaniği
        float[] diagonals = { 45f, 135f, 225f, 315f };
        float angle = diagonals[Random.Range(0, diagonals.Length)] * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

        StartCoroutine(SpriteAnimation());
    }

    private void FixedUpdate()
    {
        if (!isActivated || isDead) return;
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Sprite animasyonu (Animator olmadan ping-pong)
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator SpriteAnimation()
    {
        bool toggle = false;
        while (!isDead)
        {
            if (spriteRenderer != null)
                spriteRenderer.sprite = toggle ? sprite2 : sprite1;
            toggle = !toggle;
            yield return new WaitForSeconds(spriteSwitchInterval);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Çarpışma: duvar yansıması + oyuncuya temas patlaması
    // ─────────────────────────────────────────────────────────────────────────

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Duvar, kapı veya Enemy layer'ındaki nesnelerden sekme
        if (collision.gameObject.CompareTag("Wall") ||
            collision.gameObject.CompareTag("Door") ||
            (enemyLayer != -1 && collision.gameObject.layer == enemyLayer))
        {
            Vector2 normal = collision.contacts[0].normal;
            moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
            return;
        }

        // Oyuncuya değince → patlama + hasar
        if (collision.gameObject.CompareTag(playerTag))
        {
            TryDamagePlayer(collision.gameObject);
            StartCoroutine(ExplodeRoutine());
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Stay üzerinde sadece hasar — patlama zaten Enter'da başlatıldı
        // (isDead kontrolü ExplodeRoutine içindeki flag ile yapılır)
        if (isDead) return;

        if (collision.gameObject.CompareTag(playerTag))
            TryDamagePlayer(collision.gameObject);
    }

    private void TryDamagePlayer(GameObject player)
    {
        var isaac = player.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Bomba hasarı (Explosion layer trigger)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        // Sadece Explosion layer'ından gelen trigger'lar hasar verir
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;

        // PooterProjectile gibi şeyleri filtrele (isteğe bağlı genişletilebilir)
        if (other.GetComponent<PooterProjectile>() != null) return;

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeBombDamage(knockDir);
    }

    private void TakeBombDamage(Vector2 knockDir)
    {
        currentBombHits++;

        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        if (currentBombHits >= bombsToKill)
            StartCoroutine(ExplodeRoutine());   // bombayla öldürülünce de patlama
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) spriteRenderer.color = originalColor;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Patlama dizisi (TNT.ExplodeRoutine mantığıyla aynı)
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator ExplodeRoutine()
    {
        if (isDead) yield break;
        isDead = true;

        // Fizik & çarpışmayı kapat
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Static;
        if (col != null) col.enabled = false;

        // Ses
        PlayExplosionSound();

        // TNT tarzı Explosion trigger oluştur (diğer düşmanlara/bombalara hasar verir)
        SpawnExplosionTrigger(transform.position);

        // VFX (isteğe bağlı)
        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        // Sprite patlama animasyonu
        if (explosionFrames != null && explosionFrames.Length > 0)
            yield return StartCoroutine(PlayExplosionAnimation());

        // Kül sprite varsa beklet, yoksa yok et
        if (afterExplosionSprite != null)
        {
            rb.simulated          = false;
            spriteRenderer.sprite = afterExplosionSprite;

            if (afterExplosionLifetime > 0f)
                Destroy(gameObject, afterExplosionLifetime);

            yield break;
        }

        Destroy(gameObject);
    }

    private IEnumerator PlayExplosionAnimation()
    {
        if (spriteRenderer == null || explosionFrames == null || explosionFrames.Length == 0)
            yield break;

        float frameDuration = explosionFPS > 0f ? 1f / explosionFPS : 0.083f;

        foreach (Sprite frame in explosionFrames)
        {
            if (spriteRenderer == null) yield break;
            spriteRenderer.sprite = frame;
            yield return new WaitForSeconds(frameDuration);
        }
    }

    /// <summary>
    /// TNT.SpawnExplosionTrigger ile birebir aynı mantık.
    /// Explosion layer'ında kısa ömürlü bir CircleCollider2D trigger oluşturur.
    /// </summary>
    private void SpawnExplosionTrigger(Vector2 position)
    {
        GameObject expGO         = new GameObject("Boomfly_ExplosionTrigger");
        expGO.transform.position = position;
        expGO.layer              = LayerMask.NameToLayer("Explosion");

        Rigidbody2D expRb  = expGO.AddComponent<Rigidbody2D>();
        expRb.bodyType     = RigidbodyType2D.Kinematic;
        expRb.gravityScale = 0f;

        CircleCollider2D expCol = expGO.AddComponent<CircleCollider2D>();
        expCol.isTrigger        = true;
        expCol.radius           = explosionRadius;

        Destroy(expGO, explosionTriggerDuration);
    }

    private void PlayExplosionSound()
    {
        if (explosionClips == null || explosionClips.Length == 0) return;
        if (selectedExplosionClip < 0 || selectedExplosionClip >= explosionClips.Length) return;
        if (explosionClips[selectedExplosionClip] == null) return;
        AudioSource.PlayClipAtPoint(explosionClips[selectedExplosionClip], transform.position);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Aktivasyon (DOF_AI ile uyumlu)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Pozisyonsuz aktivasyon — prefab doğru yerde spawn edilmişse.</summary>
    public void Activate()
    {
        isActivated = true;
    }

    /// <summary>Pozisyonlu aktivasyon — oda ortası / spawn noktasından çağrılır.</summary>
    public void Activate(Vector2 spawnPosition)
    {
        rb.position       = spawnPosition;
        transform.position = spawnPosition;
        isActivated       = true;
    }

    public void SetActive(bool active)
    {
        isActivated = active;
        if (!active && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.28f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}