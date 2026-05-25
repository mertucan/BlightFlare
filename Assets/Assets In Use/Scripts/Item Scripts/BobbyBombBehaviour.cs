using System.Collections;
using UnityEngine;

/// <summary>
/// Bobby-Bomb'un bomba davranışı.
///
/// Prefab Rigidbody2D ayarları:
///   bodyType     = Dynamic
///   gravityScale = 0
///   freezeRotation = true
///   Linear Damping = 2
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class BobbyBombBehaviour : MonoBehaviour
{
    [Header("Homing")]
    public float moveSpeed   = 2.5f;
    public float searchRadius = 5f;

    [Tooltip("İlk arama öncesi bekleme. EnemyRoomTrigger'ın aktive etmesi için.")]
    public float searchDelay = 0.15f;

    [Tooltip("Düşman bulunamazsa kaç kez tekrar denensin.")]
    public int   searchRetries  = 5;

    [Tooltip("Her yeniden deneme arasındaki süre (saniye).")]
    public float retryInterval  = 0.1f;

    [Header("Blocking")]
    public string[]   blockingTags   = { "Wall", "Door", "Destructible", "TNT" };
    public LayerMask  blockingLayers;
    public float      blockCheckDistance = 0.35f;

    [Header("Fuse")]
    public float fuseTime = 3f;

    [Header("Blink")]
    public float initialBlinkRate = 0.45f;
    public float finalBlinkRate   = 0.07f;
    public Color blinkColor = new Color(1f, 0.6f, 0f, 1f);
    public Color baseColor  = Color.white;

    // ── İç durum ──────────────────────────────────────────────────────────
    private SpriteRenderer sr;
    private Rigidbody2D    rb;
    private Transform      target;
    private bool           moving  = false;
    private bool           blocked = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.freezeRotation = true;
        // bodyType'a dokunma — prefab'da Dynamic kalmalı
    }

    private void OnEnable()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        rb.linearVelocity  = Vector2.zero;
        rb.angularVelocity = 0f;
        moving  = false;
        blocked = false;
        target  = null;

        StartCoroutine(HomingRoutine());
        StartCoroutine(BlinkRoutine());
    }

    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator HomingRoutine()
    {
        // İlk bekleme — EnemyRoomTrigger düşmanları aktive etsin
        yield return new WaitForSeconds(searchDelay);

        // Düşman bulunamazsa birkaç kez daha dene
        for (int attempt = 0; attempt <= searchRetries; attempt++)
        {
            target = FindNearestActiveEnemy();

            if (target != null)
            {
                Debug.Log($"[BobbyBomb] Hedef bulundu (deneme {attempt}): {target.name}");
                moving = true;
                yield break;
            }

            Debug.Log($"[BobbyBomb] Deneme {attempt}: aktif düşman bulunamadı.");

            if (attempt < searchRetries)
                yield return new WaitForSeconds(retryInterval);
        }

        Debug.Log("[BobbyBomb] Tüm denemeler bitti, yerinde kalıyor.");
    }

    // ─────────────────────────────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (!moving || blocked) return;

        if (target == null || !target.gameObject.activeInHierarchy)
        {
            moving = false;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 dir = ((Vector2)target.position - rb.position).normalized;

        if (IsBlockedInDirection(dir))
        {
            blocked = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = dir * moveSpeed;
    }

    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator BlinkRoutine()
    {
        float elapsed = 0f;
        bool  lit     = false;

        while (elapsed < fuseTime)
        {
            float t         = elapsed / fuseTime;
            float blinkRate = Mathf.Lerp(initialBlinkRate, finalBlinkRate, t);
            sr.color = lit ? blinkColor : baseColor;
            lit = !lit;
            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate;
        }

        sr.enabled = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    private bool IsBlockedInDirection(Vector2 dir)
    {
        if (blockingLayers != 0)
        {
            var hit = Physics2D.Raycast(rb.position, dir, blockCheckDistance, blockingLayers);
            if (hit.collider != null && hit.collider.gameObject != gameObject)
                return true;
        }

        var hits = Physics2D.RaycastAll(rb.position, dir, blockCheckDistance);
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.gameObject == gameObject) continue;
            foreach (var tag in blockingTags)
                if (h.collider.CompareTag(tag)) return true;
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D col) => TryBlock(col.gameObject);
    private void OnTriggerEnter2D(Collider2D other)  => TryBlock(other.gameObject);

    private void TryBlock(GameObject other)
    {
        if (!moving || blocked) return;

        if (blockingLayers != 0 && ((1 << other.layer) & blockingLayers) != 0)
        {
            blocked = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        foreach (var tag in blockingTags)
        {
            if (other.CompareTag(tag))
            {
                blocked = true;
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    private Transform FindNearestActiveEnemy()
    {
        var enemies = GameObject.FindGameObjectsWithTag("Enemy");

        int activeCount = 0;
        foreach (var e in enemies)
            if (e != null && e.activeInHierarchy) activeCount++;

        Debug.Log($"[BobbyBomb] FindNearestActiveEnemy → toplam:{enemies.Length} aktif:{activeCount} searchRadius:{searchRadius}");

        Transform nearest    = null;
        float     nearestDst = float.MaxValue;

        foreach (var e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;

            float dist = Vector2.Distance(transform.position, e.transform.position);
            if (dist > searchRadius) continue;

            if (dist < nearestDst)
            {
                nearestDst = dist;
                nearest    = e.transform;
            }
        }

        return nearest;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sadece Editor'da görünür, build'e dahil olmaz
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Search radius — sarı daire
        UnityEditor.Handles.color = new Color(1f, 0.92f, 0.016f, 0.25f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, searchRadius);
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, searchRadius);

        // Block check ray — kırmızı çizgi (hedef varsa)
        if (target != null)
        {
            Gizmos.color = Color.red;
            Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + dir * blockCheckDistance);

            // Hedefe çizgi — yeşil
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);

            // Hedef üzerine X işareti
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(target.position, 0.2f);
        }

        // Bomba merkezi — turuncu nokta
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawSphere(transform.position, 0.12f);
    }
    #endif
}