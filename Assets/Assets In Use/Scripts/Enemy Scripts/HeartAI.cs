using System.Collections;
using UnityEngine;

/// <summary>
/// Mask of Infamy — Kalp Kısmı
///
/// Davranış:
///   - Sürekli wander yapar (BabyAI/LokiAI tarzı SmoothDamp + duvar kaçınma).
///   - Oyuncu panikDistance içine girince hafifçe kaçar.
///   - Bomba hasarı alır; bombsToKill kadar bomba yiyince ölür.
///   - Ölünce tunnelSpriteRenderer ve tunnelCollider2D aktif olur, GameObj destroy edilir.
///   - Random aralıklarla Loki tarzı projectile atar + ses çalar.
///   - Oyuncuya temas halinde hasar verir.
///   - Wall ve Door içinden geçmez.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HeartAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // CAN
    // ─────────────────────────────────────────────
    [Header("Can")]
    public int   bombsToKill     = 5;
    public float damageCooldown  = 0.5f;
    public float hitFlashDuration = 0.15f;
    public Color hitFlashColor   = Color.red;
    public GameObject deathEffectPrefab;

    [Header("Tunnel (Ölünce Açılır)")]
    public SpriteRenderer tunnelSpriteRenderer;
    public Collider2D     tunnelCollider2D;

    // ─────────────────────────────────────────────
    // HAREKET
    // ─────────────────────────────────────────────
    [Header("Hareket")]
    public float moveSpeed    = 1.0f;
    public float smoothTime   = 0.5f;

    [Header("Kaçış")]
    [Tooltip("Bu mesafe içine girince kaçmaya başlar.")]
    public float panicDistance        = 3.5f;
    [Tooltip("Kaçış hız çarpanı.")]
    public float panicSpeedMultiplier = 1.6f;

    [Header("Wander")]
    public float wanderArrivalRadius     = 0.6f;
    public float wanderChangeIntervalMin = 1.8f;
    public float wanderChangeIntervalMax = 3.5f;
    public float wanderRadius            = 2.8f;

    // ─────────────────────────────────────────────
    // DUVAR KAÇINMA
    // ─────────────────────────────────────────────
    [Header("Duvar Kaçınma")]
    public float wallAvoidDistance = 1.2f;
    public float wallAvoidWeight   = 2.5f;

    // ─────────────────────────────────────────────
    // ODA SINIRI
    // ─────────────────────────────────────────────
    [Header("Oda İnset")]
    public float roomInsetLeft   = 2.0f;
    public float roomInsetRight  = 2.5f;
    public float roomInsetBottom = 2.0f;
    public float roomInsetTop    = 2.0f;

    [Header("Duvar & Kapı Tag")]
    public string wallTag = "Wall";
    public string doorTag = "Door";

    // ─────────────────────────────────────────────
    // OYUNCU
    // ─────────────────────────────────────────────
    [Header("Oyuncu")]
    public string playerTag       = "Player";
    public float  contactCooldown = 0.8f;

    // ─────────────────────────────────────────────
    // SALDIRI (Loki tarzı)
    // ─────────────────────────────────────────────
    [Header("Saldırı")]
    public GameObject projectilePrefab;
    public Transform  firePoint;
    public float attackCooldownMin = 3f;
    public float attackCooldownMax = 6f;
    [Tooltip("Saldırı sırasında hareket çarpanı (0 = tamamen durur).")]
    [Range(0f, 1f)] public float attackMoveMultiplier = 0.2f;

    // ─────────────────────────────────────────────
    // SES
    // ─────────────────────────────────────────────
    [Header("Ses")]
    public AudioSource audioSource;
    [Tooltip("Projectile atarken çalınır.")]
    public AudioClip   attackClip;
    [Tooltip("Hasar alınca çalınır (isteğe bağlı).")]
    public AudioClip   hitClip;
    [Tooltip("Ölünce çalınır (isteğe bağlı).")]
    public AudioClip   deathClip;

    // ─────────────────────────────────────────────
    // GÖRSEL
    // ─────────────────────────────────────────────
    [Header("Görsel")]
    public SpriteRenderer spriteRenderer;

    // ─────────────────────────────────────────────
    // RUNTIME DEBUG
    // ─────────────────────────────────────────────
    [Header("Runtime (Debug — Read Only)")]
    [SerializeField] private int  currentBombHits;
    [SerializeField] private bool isDead;
    [SerializeField] private bool isAttacking;
    [SerializeField] private Rect roomSafeRect;

    // ─── privates ────────────────────────────────
    private Rigidbody2D rb;
    private Collider2D  col;
    private Transform   player;

    private Vector2 wanderTarget;
    private float   wanderChangeTimer;
    private Vector2 smoothDampVelocity;

    private float lastDamageTime  = -999f;
    private float lastContactTime = -999f;
    private float attackTimer;

    private Color     originalColor = Color.white;
    private Coroutine flashRoutine;

    private bool isActivated = false;

    // ═══════════════════════════════════════════
    #region Unity Callbacks

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

        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);

        // ✅ wanderTarget'ı burada AYARLAMA — Activate()'e bırak
        // wanderTarget ve wanderChangeTimer Activate() içinde set edilecek

        TryFindPlayer();
    }
    private void Start()
    {
        // GatherRoomBounds() burada çağrılmasın — Activate()'e bırak
        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = false;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = false;
    }

    private BossRoomTrigger bossRoomTrigger;

    public void SetBossRoomTrigger(BossRoomTrigger trigger)
    {
        bossRoomTrigger = trigger;
    }

    private void Update()
    {
        if (isDead || !isActivated) return;

        if (player == null) TryFindPlayer();
        if (player == null) return;

        // Wander hedef güncelle
        wanderChangeTimer -= Time.deltaTime;
        if (wanderChangeTimer <= 0f ||
            Vector2.Distance(rb.position, wanderTarget) < wanderArrivalRadius)
            PickNewWanderTarget();

        // Saldırı zamanlayıcısı
        if (!isAttacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
                StartCoroutine(AttackRoutine());
        }
    }

    private void FixedUpdate()
    {
        if (isDead || !isActivated || rb == null) return;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }

        float moveMul = isAttacking ? attackMoveMultiplier : 1f;

        Vector2 desired = ComputeDesiredVelocity() * moveMul;
        rb.linearVelocity = Vector2.SmoothDamp(
            rb.linearVelocity, desired,
            ref smoothDampVelocity, Mathf.Max(0.01f, smoothTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        // Kendi projectile'larını ve PooterProjectile'ı yoksay
        if (other.GetComponent<BabyProjectile>()  != null) return;
        if (other.GetComponent<PooterProjectile>() != null) return;

        // Yalnızca Explosion layer'ı
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;

        float now = Time.time;
        if (now - lastDamageTime < damageCooldown) return;
        lastDamageTime = now;

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeBombDamage(knockDir);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag(playerTag))
            TryDamagePlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag(playerTag))
            TryDamagePlayer(collision.gameObject);
    }

    private void OnDisable()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Hareket

    private Vector2 ComputeDesiredVelocity()
    {
        if (player == null) return Vector2.zero;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float   distance = toPlayer.magnitude;

        // Panik: oyuncu çok yakınsa kaç
        if (distance < panicDistance && distance > 0.01f)
        {
            Vector2 fleeDir = -(toPlayer / distance); // oyuncudan uzaklaş
            Vector2 avoidF  = ComputeWallAvoidance();
            Vector2 finalDir = fleeDir + avoidF * wallAvoidWeight;
            if (finalDir.sqrMagnitude > 1f) finalDir.Normalize();
            return finalDir * moveSpeed * panicSpeedMultiplier;
        }

        // Normal wander
        Vector2 toWander  = wanderTarget - rb.position;
        Vector2 wanderDir = toWander.sqrMagnitude > 0.001f ? toWander.normalized : Vector2.right;

        Vector2 avoidForce = ComputeWallAvoidance();
        Vector2 final      = wanderDir + avoidForce * wallAvoidWeight;
        if (final.sqrMagnitude > 1f) final.Normalize();

        return final * moveSpeed;
    }

    private Vector2 ComputeWallAvoidance()
    {
        Vector2      avoidForce = Vector2.zero;
        Collider2D[] nearby     = Physics2D.OverlapCircleAll(rb.position, wallAvoidDistance);
        foreach (var w in nearby)
        {
            if (w.isTrigger || w == col) continue;
            if (!w.CompareTag(wallTag) && !w.CompareTag(doorTag)) continue;
            Vector2 closest = w.ClosestPoint(rb.position);
            Vector2 diff    = rb.position - closest;
            float   dist    = diff.magnitude;
            if (dist > 0.001f && dist < wallAvoidDistance)
                avoidForce += (diff / dist) * ((wallAvoidDistance - dist) / wallAvoidDistance);
        }
        return avoidForce;
    }

    private void PickNewWanderTarget()
    {
        for (int i = 0; i < 20; i++)
        {
            float   angle     = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float   dist      = Random.Range(wanderRadius * 0.3f, wanderRadius);
            Vector2 candidate = rb.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

            if (IsPositionValid(candidate))
            {
                wanderTarget      = candidate;
                wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);
                return;
            }
        }
        wanderTarget      = roomSafeRect.center;
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Saldırı

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        PlayClip(attackClip);

        // Oyuncuya doğru tek mermi at (isteğe göre çapraz/artı da eklenebilir)
        FireAtPlayer();

        yield return new WaitForSeconds(0.3f);

        isAttacking = false;
        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);
    }

    private void FireAtPlayer()
    {
        if (projectilePrefab == null || player == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir      = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        BabyProjectile bp = go.GetComponent<BabyProjectile>();
        if (bp != null) { bp.Initialize(dir); return; }

        PooterProjectile pp = go.GetComponent<PooterProjectile>();
        if (pp != null) { pp.Initialize(dir); return; }

        Rigidbody2D projRb = go.GetComponent<Rigidbody2D>();
        if (projRb != null) projRb.linearVelocity = dir * 4f;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Hasar / Ölüm

    private void TakeBombDamage(Vector2 knockDir)
    {
        if (isDead) return;
        currentBombHits++;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        PlayClip(hitClip);

        if (currentBombHits >= bombsToKill)
            Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        SetColor(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) SetColor(originalColor);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        SetColor(originalColor);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Tunnel aç
        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = true;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = true;

        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;

        if (audioSource != null && deathClip != null)
            AudioSource.PlayClipAtPoint(deathClip, transform.position);

        if (bossRoomTrigger != null)
            bossRoomTrigger.OnBossDefeated();

        Destroy(gameObject);
    }

    private void TryDamagePlayer(GameObject playerObj)
    {
        if (Time.time - lastContactTime < contactCooldown) return;
        lastContactTime = Time.time;
        IsaacMovement isaac = playerObj.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(1);
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Oda Sınırı

    private void GatherRoomBounds()
    {
        Vector2 origin    = transform.position;
        float   search    = 40f;
        float   tolerance = 0.5f;

        float bestLeft  = origin.x - 8f;
        float bestRight = origin.x + 8f;
        float bestDown  = origin.y - 5f;
        float bestUp    = origin.y + 5f;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, search);
        foreach (var c in nearby)
        {
            if (c == col) continue;
            if (!c.CompareTag(wallTag)) continue;

            Bounds b = c.bounds;

            if (b.max.x <= origin.x && b.min.y <= origin.y + tolerance && b.max.y >= origin.y - tolerance)
                bestLeft = Mathf.Max(bestLeft, b.max.x);

            if (b.min.x >= origin.x && b.min.y <= origin.y + tolerance && b.max.y >= origin.y - tolerance)
                bestRight = Mathf.Min(bestRight, b.min.x);

            if (b.max.y <= origin.y && b.min.x <= origin.x + tolerance && b.max.x >= origin.x - tolerance)
                bestDown = Mathf.Max(bestDown, b.max.y);

            if (b.min.y >= origin.y && b.min.x <= origin.x + tolerance && b.max.x >= origin.x - tolerance)
                bestUp = Mathf.Min(bestUp, b.min.y);
        }

        roomSafeRect = Rect.MinMaxRect(
            bestLeft  + roomInsetLeft,
            bestDown  + roomInsetBottom,
            bestRight - roomInsetRight,
            bestUp    - roomInsetTop);
    }

    private bool IsPositionValid(Vector2 pos)
    {
        if (!roomSafeRect.Contains(pos)) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, 0.3f);
        foreach (var hit in hits)
        {
            if (hit == col) continue;
            if (hit.isTrigger) continue;
            if (hit.CompareTag(wallTag) || hit.CompareTag(doorTag)) return false;
        }
        return true;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Yardımcılar

    private void SetColor(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = c;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void TryFindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Aktivasyon

    public void Activate()
    {
        GatherRoomBounds();
        PickNewWanderTarget();
        isActivated = true;
    }

    public void Activate(Vector2 spawnPosition)
    {
        rb.position        = spawnPosition;
        transform.position = (Vector3)spawnPosition;
        GatherRoomBounds();
        PickNewWanderTarget();
        isActivated = true;
    }

    public void SetActive(bool active)
    {
        isActivated = active;
        if (!active && rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && roomSafeRect.width > 0)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(
                new Vector3(roomSafeRect.center.x, roomSafeRect.center.y, 0f),
                new Vector3(roomSafeRect.width, roomSafeRect.height, 0f));
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, panicDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wallAvoidDistance);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, wanderTarget);
            Gizmos.DrawWireSphere(wanderTarget, 0.2f);
        }
    }
#endif

    #endregion
}