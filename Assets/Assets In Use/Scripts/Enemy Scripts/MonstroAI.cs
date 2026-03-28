using System.Collections;
using UnityEngine;

/// <summary>
/// Monstro Boss AI — The Binding of Isaac tarzı top-down 2D boss.
///
/// ──────────────────────────────────────────────────────────────
/// ÖNERILEN HIERARCHY:
///   Monstro (Root)   ← Rigidbody2D (Dynamic, gravityScale=0), CircleCollider2D, MonstroAI
///     SpriteHolder   ← SpriteRenderer, Animator   ← Inspector'da MUTLAKA ata
///     FirePoint      ← Transform (Monstro'nun ağzının önünde)
///
/// ANIMATOR KURULUMU:
///   Bool  : isAttacking
///   Float : dirX, dirY   ← yön blend tree için (opsiyonel)
///   Entry → Idle (default)
///   Idle  → Attack : isAttacking = true   (Has Exit Time = false)
///   Attack→ Idle   : isAttacking = false  (Has Exit Time = false)
///
/// LAYER:
///   Monstro          → "Enemy"
///   BloodTear prefab → "EnemyProjectile"
///   Physics2D Matrix: Enemy ↔ EnemyProjectile çarpışma KAPALI
///
/// POZİSYON BUGININ NEDENİ VE FİXİ:
///   Rigidbody2D.position, Unity physics step başlamadan önce
///   transform.position ile senkronize olmayabilir.
///   Fix: Awake + Start içinde rb.position zorla set edilir +
///        Physics2D.SyncTransforms() çağrılır.
///        HopRoutine'de startPos artık rb.position değil transform.position'dan alınır.
/// ──────────────────────────────────────────────────────────────
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MonstroAI : MonoBehaviour
{
    // ─────────────────────────── State ───────────────────────────
    public enum BossState { Idle, Hop, Vomit, Dead }

    // ─────────────────────────── Stats ───────────────────────────
    [Header("Stats")]
    public int maxHP = 3;

    // ─────────────────────────── Target ──────────────────────────
    [Header("Target")]
    public string    playerTag = "Player";
    public Transform player;

    // ─────────────────────────── Idle ────────────────────────────
    [Header("Idle")]
    public float minIdleTime = 0.8f;
    public float maxIdleTime = 1.5f;

    // ─────────────────────────── Hop ─────────────────────────────
    [Header("Hop")]
    [Tooltip("Her zıplamanın süresi (sn).")]
    public float hopDuration  = 0.55f;
    [Tooltip("Sprite'ın havadaki maksimum yüksekliği (local Y).")]
    public float hopArcHeight = 1.6f;
    [Tooltip("Minimum yaklaşma oranı — bu kadar gidilir en az.")]
    [Range(0f, 1f)] public float minHopReach = 0.20f;
    [Tooltip("Maksimum yaklaşma oranı — bazen biraz daha fazla yaklaşır.")]
    [Range(0f, 1f)] public float maxHopReach = 0.55f;
    [Tooltip("Bu mesafeden daha yakınsa hop yapılmaz.")]
    public float minHopDistance = 1.8f;
    [Tooltip("İnişte squash animasyonunun süresi.")]
    public float landSquashDuration = 0.20f;

    // ─────────────────────────── Vomit ───────────────────────────
    [Header("Vomit — Şarj")]
    public float vomitChargeTime    = 0.65f;
    public float vomitAfterFireTime = 0.5f;

    [Header("Vomit — Tear")]
    public int   minTearCount = 6;
    public int   maxTearCount = 10;
    [Range(20f, 160f)]
    [Tooltip("Oyuncuya göre toplam saçılma açısı (derece). TBOI ~70°.")]
    public float spreadAngle       = 70f;
    public float tearSpeed         = 4.5f;
    public float tearSpeedVariance = 0.6f;
    public float tearArcHeight     = 1.5f;
    [Tooltip("Tearlar arası gecikme (sn). 0.04–0.06 ideal.")]
    public float tearFireDelay     = 0.045f;

    [Tooltip("BloodTear prefabı. Layer'i 'EnemyProjectile' olmalı.")]
    public GameObject tearPrefab;
    [Tooltip("Tear spawn noktası. Boş bırakılırsa Monstro merkezi kullanılır.")]
    public Transform  firePoint;

    // ─────────────────────────── Visuals ─────────────────────────
    [Header("Visuals & Feedback")]
    [Tooltip("MUTLAKA ATANMALI — arc/squash/flip bu child'a uygulanır.")]
    public Transform      spriteHolder;
    public SpriteRenderer spriteRenderer;
    public Animator       animator;
    public float hitFlashDuration = 0.10f;
    public Color hitFlashColor    = new Color(1f, 0.25f, 0.25f);
    public GameObject deathEffectPrefab;

    // ─────────────────────────── Animator ────────────────────────
    [Header("Animator Parametreleri")]
    public string animIsAttackingParam = "isAttacking";
    [Tooltip("Blend tree için yatay yön float parametresi. Boş = kullanma.")]
    public string animDirXParam = "dirX";
    [Tooltip("Blend tree için dikey yön float parametresi. Boş = kullanma.")]
    public string animDirYParam = "dirY";

    // ─────────────────────────── Debug ───────────────────────────
    [Header("Runtime (Read-only)")]
    public BossState currentState = BossState.Idle;
    public int  currentHP;
    public bool isDead;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ─────────────────────────── Private ─────────────────────────
    private Rigidbody2D rb;
    private Coroutine   flashRoutine;
    private int         hashIsAttacking;
    private int         hashDirX;
    private int         hashDirY;
    private Color       originalColor = Color.white;
    private int         explosionLayer;
    private bool        spriteIsRoot;
    private Vector2     currentFacingDir = Vector2.right;

    // ═════════════════════════════════════════════════════════════
    //  LIFECYCLE
    // ═════════════════════════════════════════════════════════════

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale           = 0f;
        rb.freezeRotation         = true;
        rb.interpolation          = RigidbodyInterpolation2D.None; // interpolasyon kaymasını önle
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // ── POZİSYON FIX (1/2): Physics step başlamadan rb.position'ı senkronize et
        rb.position = (Vector2)transform.position;
        Physics2D.SyncTransforms();

        // SpriteHolder
        if (spriteHolder == null)
            spriteHolder = transform.childCount > 0 ? transform.GetChild(0) : transform;

        spriteIsRoot = (spriteHolder == transform);
        if (spriteIsRoot)
            LogWarning("SpriteHolder root'la aynı — arc/squash çalışmaz!");

        if (spriteRenderer == null)
            spriteRenderer = spriteHolder?.GetComponent<SpriteRenderer>()
                          ?? GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        hashIsAttacking = Animator.StringToHash(animIsAttackingParam);
        if (!string.IsNullOrEmpty(animDirXParam)) hashDirX = Animator.StringToHash(animDirXParam);
        if (!string.IsNullOrEmpty(animDirYParam)) hashDirY = Animator.StringToHash(animDirYParam);

        currentHP      = Mathf.Max(1, maxHP);
        explosionLayer = LayerMask.NameToLayer("Explosion");

        TryFindPlayer();
        Log("Awake tamamlandı.");
    }

    private void Start()
    {
        // ── POZİSYON FIX (2/2): Start frame'inde de zorla senkronize et
        rb.position = (Vector2)transform.position;
        Physics2D.SyncTransforms();

        IgnoreOwnTearLayer();
        StartCoroutine(StateMachine());
    }

    private void IgnoreOwnTearLayer()
    {
        int tearLayer = LayerMask.NameToLayer("EnemyProjectile");
        int selfLayer = gameObject.layer;
        if (tearLayer >= 0 && selfLayer >= 0)
            Physics2D.IgnoreLayerCollision(selfLayer, tearLayer, true);
    }

    // ═════════════════════════════════════════════════════════════
    //  STATE MACHINE
    // ═════════════════════════════════════════════════════════════

    private IEnumerator StateMachine()
    {
        yield return new WaitForSeconds(1.2f);

        bool firstAction = true;

        while (!isDead)
        {
            yield return StartCoroutine(IdleRoutine());
            if (isDead) yield break;

            if (firstAction)
            {
                firstAction = false;
                yield return StartCoroutine(VomitRoutine()); // İlk aksiyon daima Vomit
            }
            else
            {
                // 40% Vomit, 60% Hop
                if (Random.value < 0.4f)
                    yield return StartCoroutine(VomitRoutine());
                else
                    yield return StartCoroutine(HopRoutine());
            }
        }
    }

    // ─────────────────────────── Idle ────────────────────────────

    private IEnumerator IdleRoutine()
    {
        currentState = BossState.Idle;
        SetAttackAnim(false);
        FacePlayer(); // Idle'da oyuncuya bak

        float wait = Random.Range(minIdleTime, maxIdleTime);
        Log($"Idle: {wait:F2}s");
        yield return new WaitForSeconds(wait);

        TryFindPlayer();
    }

    // ─────────────────────────── Hop ─────────────────────────────
    //
    // POZİSYON FIX: startPos = transform.position (rb.position DEĞİL)
    // HOP FIX: hopReach her hop'ta minHopReach-maxHopReach arasında random

    private IEnumerator HopRoutine()
    {
        currentState = BossState.Hop;
        SetAttackAnim(false);
        TryFindPlayer();
        if (player == null) yield break;

        // ── POZİSYON FIX: rb.position değil transform.position kullan
        Vector2 startPos        = (Vector2)transform.position;
        Vector2 frozenPlayerPos = (Vector2)player.position; // hop başında kilitle

        float dist = Vector2.Distance(startPos, frozenPlayerPos);
        if (dist < minHopDistance)
        {
            Log($"Hop iptal — çok yakın ({dist:F2} < {minHopDistance:F2})");
            yield break;
        }

        // ── HOP FIX: Her hop'ta farklı random yaklaşma mesafesi
        float thisReach = Random.Range(minHopReach, maxHopReach);
        Vector2 targetPos = Vector2.Lerp(startPos, frozenPlayerPos, thisReach);

        UpdateFacing(targetPos - startPos);
        Log($"Hop: {startPos} → {targetPos} (reach={thisReach:F2})");

        float elapsed = 0f;
        while (elapsed < hopDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hopDuration);

            rb.MovePosition(Vector2.Lerp(startPos, targetPos, EaseInOutQuad(t)));

            float arcY = hopArcHeight * Mathf.Sin(t * Mathf.PI);
            ApplySpriteArc(arcY);

            yield return null;
        }

        rb.MovePosition(targetPos);
        ResetSpriteTransform();
        yield return StartCoroutine(LandingSquashRoutine());
        Log("Hop bitti.");
    }

    private IEnumerator LandingSquashRoutine()
    {
        SetSpriteScale(1.4f, 0.65f);
        float elapsed = 0f;
        while (elapsed < landSquashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / landSquashDuration;
            SetSpriteScale(Mathf.Lerp(1.4f, 1f, t), Mathf.Lerp(0.65f, 1f, t));
            yield return null;
        }
        SetSpriteScale(1f, 1f);
    }

    // ─────────────────────────── Vomit ───────────────────────────

    private IEnumerator VomitRoutine()
    {
        currentState = BossState.Vomit;
        TryFindPlayer();

        // Şarj başında oyuncuya bak
        if (player != null)
            UpdateFacing((Vector2)player.position - (Vector2)transform.position);

        SetAttackAnim(true);

        yield return new WaitForSeconds(vomitChargeTime);

        // Ateş anında yönü tekrar güncelle — oyuncu hareket etmiş olabilir
        TryFindPlayer();
        if (player != null)
            UpdateFacing((Vector2)player.position - (Vector2)transform.position);

        yield return StartCoroutine(FireBloodTearsRoutine());

        yield return new WaitForSeconds(vomitAfterFireTime);
        SetAttackAnim(false);
        Log("Vomit bitti → Idle.");
    }

    // ─────────────────────────── FireBloodTears ──────────────────
    //
    // ATEŞ YÖN FIX:
    //   baseAngle anlık player konumundan hesaplanır — sabit değil.
    //   Top-down 360°: Monstro hangi yönde olursa olsun doğru yönde atar.
    //   Spawn offseti random → tearlar görsel olarak dağılır, üst üste binmez.

    private IEnumerator FireBloodTearsRoutine()
    {
        if (tearPrefab == null)
        {
            LogWarning("tearPrefab Inspector'da ATANMAMIŞ!");
            yield break;
        }

        TryFindPlayer();
        if (player == null) yield break;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;

        // ── ATEŞ YÖN FIX: Anlık oyuncu pozisyonuna göre yön hesapla (360°)
        Vector2 toPlayer = (Vector2)player.position - (Vector2)origin;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = currentFacingDir;
        toPlayer = toPlayer.normalized;

        float baseAngle = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;

        int actualTearCount = Random.Range(minTearCount, maxTearCount + 1);
        Collider2D selfCol  = GetComponent<Collider2D>();

        Log($"Ateş: {actualTearCount} tear @ {baseAngle:F1}° ±{spreadAngle*0.5f:F0}°");

        for (int i = 0; i < actualTearCount; i++)
        {
            float frac      = actualTearCount > 1 ? (float)i / (actualTearCount - 1) : 0.5f;
            float evenAngle = Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, frac);
            float jitter    = Random.Range(-spreadAngle * 0.08f, spreadAngle * 0.08f);
            float angle     = baseAngle + evenAngle + jitter;
            float speed     = Mathf.Max(1.5f, tearSpeed + Random.Range(-tearSpeedVariance, tearSpeedVariance));

            float   rad        = angle * Mathf.Deg2Rad;
            Vector2 dir        = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float   flightTime = 1.1f + Random.Range(-0.15f, 0.25f);
            Vector2 landingPos = (Vector2)origin + dir * speed * flightTime;

            // Random spawn offset → tearlar dağılmış görünür
            Vector3 spawnPos = origin + (Vector3)(dir * Random.Range(0.15f, 0.35f));
            GameObject go    = Instantiate(tearPrefab, spawnPos, Quaternion.identity);

            Collider2D tearCol = go.GetComponent<Collider2D>();
            if (tearCol != null && selfCol != null)
                Physics2D.IgnoreCollision(tearCol, selfCol, true);

            MonstroBloodTear tear = go.GetComponent<MonstroBloodTear>();
            if (tear != null)
                tear.Initialize(landingPos, speed, tearArcHeight + Random.Range(-0.3f, 0.3f));
            else
                LogWarning($"Prefab'da MonstroBloodTear YOK — tear [{i}] hareketsiz kalır!");

            yield return new WaitForSeconds(tearFireDelay);
        }

        Log($"{actualTearCount} tear fırlatıldı.");
    }

    // ═════════════════════════════════════════════════════════════
    //  DAMAGE
    // ═════════════════════════════════════════════════════════════

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.gameObject.layer == explosionLayer)
        {
            Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
            TakeDamage(1, knockDir);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag(playerTag)) return;
        collision.gameObject.GetComponent<IsaacMovement>()?.DeathSequence();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag(playerTag)) return;
        collision.gameObject.GetComponent<IsaacMovement>()?.DeathSequence();
    }

    public void TakeDamage(int damage, Vector2 knockDir = default)
    {
        if (isDead) return;

        currentHP -= Mathf.Max(1, damage);
        Log($"Hasar: -{damage} | HP: {currentHP}/{maxHP}");

        if (knockDir != Vector2.zero)
            rb.AddForce(knockDir * 3.5f, ForceMode2D.Impulse);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        if (currentHP <= 0) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead       = true;
        currentState = BossState.Dead;

        StopAllCoroutines();

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = Vector2.zero;
#else
            rb.velocity = Vector2.zero;
#endif
            rb.simulated = false;
        }

        StartCoroutine(DeathAnimRoutine());
    }

    // ─────────────────────────── Death Animation ─────────────────

    private IEnumerator DeathAnimRoutine()
    {
        if (spriteRenderer == null)
        {
            if (deathEffectPrefab != null)
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(gameObject, 0.05f);
            yield break;
        }

        // Faz 1: Hızlı beyaz/kırmızı flash (~0.36s)
        for (int i = 0; i < 8; i++)
        {
            spriteRenderer.color = (i % 2 == 0) ? Color.white : hitFlashColor;
            yield return new WaitForSeconds(0.045f);
        }

        // Faz 2: Büyüyüp turuncu ile solar (~0.35s)
        float   t         = 0f;
        bool    canScale  = !spriteIsRoot && spriteHolder != null;
        Vector3 baseScale = canScale ? spriteHolder.localScale : Vector3.one;
        Color   startCol  = originalColor;
        startCol.a           = 1f;
        spriteRenderer.color = startCol;

        while (t < 0.35f)
        {
            t += Time.deltaTime;
            float p = EaseOutQuart(t / 0.35f);

            if (canScale)
                spriteHolder.localScale = Vector3.LerpUnclamped(baseScale, baseScale * 2.8f, p);

            spriteRenderer.color = Color.Lerp(startCol, new Color(1f, 0.35f, 0.1f, 0f), p);
            yield return null;
        }

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Log("Monstro öldü.");
        Destroy(gameObject);
    }

    // ═════════════════════════════════════════════════════════════
    //  FACING — TOP-DOWN 360°
    //
    //  flipX     → yatay yönü yönetir (sağa gidince normal, sola gidince flip)
    //  dirX/dirY → Animator blend tree parametreleri
    //              Blend tree kurulumu (2D Simple Directional):
    //                dirX=-1 sola, +1 sağa
    //                dirY=-1 aşağı, +1 yukarı
    //              Her yön için farklı animasyon klibi atanabilir.
    // ═════════════════════════════════════════════════════════════

    private void FacePlayer()
    {
        if (player == null) return;
        UpdateFacing((Vector2)player.position - (Vector2)transform.position);
    }

    private void UpdateFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.001f) return;

        currentFacingDir = dir.normalized;

        // flipX: yatay bileşene göre
        if (spriteRenderer != null && Mathf.Abs(dir.x) > 0.05f)
            spriteRenderer.flipX = dir.x < 0f;

        // Animator blend tree parametreleri (sadece parametre varsa gönder)
        if (animator != null)
        {
            if (!string.IsNullOrEmpty(animDirXParam))
                animator.SetFloat(hashDirX, currentFacingDir.x);
            if (!string.IsNullOrEmpty(animDirYParam))
                animator.SetFloat(hashDirY, currentFacingDir.y);
        }
    }

    // ═════════════════════════════════════════════════════════════
    //  SPRITE HELPERS
    // ═════════════════════════════════════════════════════════════

    private void ApplySpriteArc(float arcY)
    {
        if (spriteHolder == null || spriteIsRoot) return;
        spriteHolder.localPosition = new Vector3(0f, arcY, 0f);
        float stretch = 1f + arcY * 0.12f;
        spriteHolder.localScale = new Vector3(1f / stretch, stretch, 1f);
    }

    private void ResetSpriteTransform()
    {
        if (spriteHolder == null || spriteIsRoot) return;
        spriteHolder.localPosition = Vector3.zero;
        spriteHolder.localScale    = Vector3.one;
    }

    private void SetSpriteScale(float sx, float sy)
    {
        if (spriteHolder == null || spriteIsRoot) return;
        spriteHolder.localScale = new Vector3(sx, sy, 1f);
    }

    // ═════════════════════════════════════════════════════════════
    //  MISC
    // ═════════════════════════════════════════════════════════════

    private void SetAttackAnim(bool value)
    {
        if (animator == null) return;
        animator.SetBool(hashIsAttacking, value);
    }

    private void TryFindPlayer()
    {
        if (player != null) return;
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    private static float EaseInOutQuad(float t) =>
        t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

    private static float EaseOutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);

    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[MonstroAI:{name}] {msg}");
    }

    private void LogWarning(string msg) =>
        Debug.LogWarning($"[MonstroAI:{name}] {msg}");

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
            Vector2 minTarget = Vector2.Lerp(transform.position, player.position, minHopReach);
            Vector2 maxTarget = Vector2.Lerp(transform.position, player.position, maxHopReach);
            Gizmos.DrawLine(transform.position, maxTarget);
            Gizmos.DrawWireSphere(minTarget, 0.18f);
            Gizmos.DrawWireSphere(maxTarget, 0.28f);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, currentFacingDir * 1.5f);
            }
        }
        // minHopDistance çemberi
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, minHopDistance);
    }
#endif
}