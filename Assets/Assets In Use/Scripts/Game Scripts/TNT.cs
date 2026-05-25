using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class TNT : MonoBehaviour
{

    [Header("Explosion Animation")]
    public Sprite[] explosionFrames;
    public float explosionFPS = 12f;
    public Sprite afterExplosionSprite;
    public float afterExplosionLifetime = 0f;

    [Header("Explosion")]
    public float explosionRadius = 2.5f;
    public float explosionTriggerDuration = 0.45f;
    public GameObject explosionVFXPrefab;

    [Header("Sounds")]
    public AudioClip[] explosionClips;
    public int selectedExplosionClip = 0;

    [Header("Push Settings")]
    public string pushableByTag = "Player";
    public float maxPushSpeed = 2.5f;
    public float drag = 5f;

    [Header("Blocking Tags")]
    public string[] blockingTags = { "Wall", "Door", "Player", "Rock", "Destructible" };

    [Header("Enemy Layer")]
    public string enemyLayerName = "Enemy";

    [Header("Projectile Filter")]
    public bool ignoreBabyProjectile = true;
    public bool ignorePooterProjectile = true;

    [Header("Debug")]
    public bool showExplosionGizmo = true;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D    rb;
    private Collider2D     col;
    private bool           isExploding;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb             = GetComponent<Rigidbody2D>();
        col            = GetComponent<Collider2D>();

        rb.gravityScale           = 0f;
        rb.freezeRotation         = true;
        rb.linearDamping          = drag;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        int enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        int tntLayer   = gameObject.layer;
        if (enemyLayer >= 0 && tntLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, tntLayer, true);
    }

    private void FixedUpdate()
    {
        if (isExploding) return;

        if (rb.linearVelocity.sqrMagnitude > maxPushSpeed * maxPushSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxPushSpeed;
    }

    // ✅ YENİ — listede olan her şeyle çarpışır, listede olmayan geçilir
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isExploding) return;

        string hitTag = collision.gameObject.tag;

        // Geçilebilir tag'ler — örn: tetikleyiciler, zemin olmayan objeler
        foreach (string blocking in blockingTags)
        {
            if (hitTag == blocking) return; // bu tag'e sahipse çarpışmayı koru
        }

        // Listede yoksa çarpışmayı yoksay (düşmanlar, mermiler vs.)
        Physics2D.IgnoreCollision(col, collision.collider, true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isExploding) return;

        // ✅ Destructible'ı SADECE patlama sonrası explosion trigger'ı patlatır.
        // TNT'nin kendi collider'ı Destructible'a çarpınca bir şey yapmaz.
        // (Eski kodda burada direkt Explode() çağrılıyordu — bu itince patlatıyordu)

        // Sadece Explosion layer'ından gelen tetikleyiciler TNT'yi patlatır
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;

        if (ignoreBabyProjectile   && other.GetComponent<BabyProjectile>()   != null) return;
        if (ignorePooterProjectile && other.GetComponent<PooterProjectile>()  != null) return;

        StartCoroutine(ExplodeRoutine());
    }

    private IEnumerator ExplodeRoutine()
    {
        isExploding       = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType       = RigidbodyType2D.Static;
        col.enabled       = false;

        if (explosionClips != null && explosionClips.Length > 0 &&
            selectedExplosionClip < explosionClips.Length &&
            explosionClips[selectedExplosionClip] != null)
        {
            AudioSource.PlayClipAtPoint(
                explosionClips[selectedExplosionClip], 
                transform.position
            );
        }

        SpawnExplosionTrigger(transform.position);

        if (explosionVFXPrefab != null)
            Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);

        if (explosionFrames != null && explosionFrames.Length > 0)
            yield return StartCoroutine(PlayExplosionAnimation());

        if (afterExplosionSprite != null)
        {
            rb.simulated          = false;
            col.enabled           = false;
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

    private void OnDrawGizmosSelected()
    {
        if (!showExplosionGizmo) return;
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.28f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
        Gizmos.color = new Color(1f, 0.35f, 0f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}