using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class TNT : MonoBehaviour
{

    [Header("Explosion Animation")]
    public Sprite[] explosionFrames;
    public float explosionFPS = 12f;
    [Tooltip("Animasyon bittikten sonra kalan sprite. Boş bırakılırsa obje tamamen yok edilir.")]
    public Sprite afterExplosionSprite;
    [Tooltip("Kalıntı sprite kaç saniye sonra yok edilsin? 0 = sonsuza kadar kalır.")]
    public float afterExplosionLifetime = 0f;

    [Header("Explosion")]
    public float explosionRadius = 2.5f;
    public float explosionTriggerDuration = 0.45f;
    public GameObject explosionVFXPrefab;

    [Header("Push Settings")]
    public string pushableByTag = "Player";
    [Tooltip("Bu hızın üstüne çıkamaz (uçmayı önler).")]
    public float maxPushSpeed = 2.5f;
    [Tooltip("Yüksek değer = hızlı yavaşlama. 4-6 arası önerilir.")]
    public float drag = 5f;

    [Header("Blocking Tags")]
    [Tooltip("TNT bu tag'lere sahip objelerin üstünden geçemez.")]
    public string[] blockingTags = { "Wall", "Door", "Player" };

    [Header("Enemy Layer")]
    public string enemyLayerName = "Enemy";

    [Header("Projectile Filter")]
    public bool ignoreBabyProjectile = true;
    public bool ignorePooterProjectile = true;

    [Header("Debug")]
    public bool showExplosionGizmo = true;

    // ── Internal ──────────────────────────────────────────────────
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D    rb;
    private Collider2D     col;
    private bool           isExploding;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb             = GetComponent<Rigidbody2D>();
        col            = GetComponent<Collider2D>();

        rb.gravityScale           = 0f;
        rb.freezeRotation         = true;
        rb.linearDamping          = drag;   // ← Sürtünme: itti bırak, hızla durur
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;


        // Düşman layer'ı TNT layer'ıyla çarpışmasın (üstünden kayar)
        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        int tntLayer   = gameObject.layer;
        if (enemyLayer >= 0 && tntLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, tntLayer, true);
    }

    // ─── Hız Tavanı ──────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (isExploding) return;

        if (rb.linearVelocity.sqrMagnitude > maxPushSpeed * maxPushSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxPushSpeed;
    }

    // ─── Çarpışma Filtresi ────────────────────────────────────────
    // blockingTags listesindekiler → normal fizik (geçilemez)
    // Geri kalan her şey → ignore (üstünden kayılır)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isExploding) return;

        string hitTag = collision.gameObject.tag;

        foreach (string blocking in blockingTags)
        {
            if (hitTag == blocking) return; // Geçme, fizik çalışsın
        }

        // Listedeki tag değilse → ignore et (düşmanlar, nötr objeler vs.)
        Physics2D.IgnoreCollision(col, collision.collider, true);
    }

    // ─── Patlama Tetikleyici ─────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isExploding) return;

        // Destructible tag'li objeleri yok et
        if (other.CompareTag("Destructible"))
        {
            Destructible destructible = other.GetComponent<Destructible>();
            if (destructible != null)
            {
                destructible.Explode();
            }
            return;
        }

        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;

        if (ignoreBabyProjectile   && other.GetComponent<BabyProjectile>()   != null) return;
        if (ignorePooterProjectile && other.GetComponent<PooterProjectile>()  != null) return;

        StartCoroutine(ExplodeRoutine());
    }

    // ─── Patlama Sekansı ─────────────────────────────────────────
    private IEnumerator ExplodeRoutine()
    {
        isExploding       = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Static;
        col.enabled       = false;

        SpawnExplosionTrigger(transform.position);

        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        if (explosionFrames != null && explosionFrames.Length > 0)
            yield return StartCoroutine(PlayExplosionAnimation());

        // ─── Kalıntı ─────────────────────────────────────────────────
        if (afterExplosionSprite != null)
        {
            rb.simulated = false;   // Rigidbody2D'yi fiziğden çıkarır
            col.enabled  = false;   // Collider'ı kapatır

            spriteRenderer.sprite = afterExplosionSprite;

            if (afterExplosionLifetime > 0f)
                Destroy(gameObject, afterExplosionLifetime);

            // Coroutine burada biter, Destroy(gameObject) çağrılmaz
            yield break;
        }

        Destroy(gameObject);
    }

    // ─── Patlama Sprite Animasyonu ────────────────────────────────
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

    // ─── Explosion Trigger ────────────────────────────────────────
    private void SpawnExplosionTrigger(Vector2 position)
    {
        GameObject expGO         = new GameObject("TNT_ExplosionTrigger");
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

    // ─── Editor Gizmo ────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        if (!showExplosionGizmo) return;
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.28f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}