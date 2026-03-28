using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BabyAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // STATS
    // ─────────────────────────────────────────────
    [Header("Stats")]
    public int maxHP = 6;
    public float moveSpeed = 0.85f;

    [Header("Target")]
    public string playerTag = "Player";

    // ─────────────────────────────────────────────
    // ATTACK
    // ─────────────────────────────────────────────
    [Header("Attack")]
    [Tooltip("Bu mesafe içine girince saldırı tetiklenir.")]
    public float attackRange = 7f;
    [Tooltip("Saldırılar arası bekleme (saniye).")]
    public float attackCooldown = 3f;
    [Tooltip("Mermi fırladıktan sonra pose süresi.")]
    public float attackAfterFireSeconds = 0.2f;
    public string attackStateName = "BabyAttack";
    public GameObject projectilePrefab;
    public Transform firePoint;

    // ─────────────────────────────────────────────
    // TELEPORT
    // ─────────────────────────────────────────────
    [Header("Teleport")]
    public float teleportCooldownMin = 3f;
    public float teleportCooldownMax = 6f;
    public float teleportFadeOutDuration = 0.2f;
    public float teleportHoldDuration    = 0.1f;
    public float teleportFadeInDuration  = 0.2f;
    public float teleportAfterSeconds    = 0.15f;
    public float teleportMinDistFromPlayer = 2.5f;
    public float teleportMaxDistFromPlayer = 6f;
    public float teleportValidationRadius  = 0.3f;
    public int   teleportMaxAttempts       = 60;
    [Tooltip("Duvar kalınlığı kadar içe daralt.")]
    public float roomInset = 1.0f;

    // ─────────────────────────────────────────────
    // MOVEMENT & WANDER
    // ─────────────────────────────────────────────
    [Header("Movement")]
    public float smoothTime = 0.55f;
    [Range(0f,1f)] public float attackMoveMultiplier   = 0f;
    [Range(0f,1f)] public float teleportMoveMultiplier = 0f;
    public float panicDistance       = 1.2f;
    public float panicSpeedMultiplier = 1.4f;

    [Header("Wander")]
    public float wanderArrivalRadius     = 0.6f;
    public float wanderChangeIntervalMin = 1.8f;
    public float wanderChangeIntervalMax = 3.5f;
    [Range(0f,1f)] public float chaseBlend = 0.3f;
    public float wanderRadius = 3f;

    // ─────────────────────────────────────────────
    // WALL / DOOR
    // ─────────────────────────────────────────────
    [Header("Wall & Door Tags")]
    public string wallTag = "Wall";
    public string doorTag = "Door";
    public float wallAvoidDistance = 1.2f;
    public float wallAvoidWeight   = 2.5f;

    // ─────────────────────────────────────────────
    // FEEDBACK
    // ─────────────────────────────────────────────
    [Header("Feedback")]
    public float knockbackForce  = 3.5f;
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor   = Color.red;
    public GameObject deathEffectPrefab;
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    public string animAttackBoolParam = "isAttacking";

    // ─────────────────────────────────────────────
    // SPAWN IN FOURS
    // ─────────────────────────────────────────────
    [Header("Spawn In Fours")]
    public bool isSpawnLeader = false;
    public GameObject babyPrefab;
    public int   spawnCount  = 3;
    public float spawnRadius = 1.2f;

    // ─────────────────────────────────────────────
    // RUNTIME (DEBUG — READ ONLY)
    // ─────────────────────────────────────────────
    [Header("Runtime (Debug — Read Only)")]
    [SerializeField] private int  currentHP;
    [SerializeField] private bool isAttacking;
    [SerializeField] private bool isTeleporting;
    [SerializeField] private bool isDead;
    [SerializeField] private Rect roomSafeRect;

    // ─── privates ────────────────────────────────
    private Rigidbody2D rb;
    private Transform   player;
    private Collider2D  ownCollider;
    private float attackCooldownTimer;
    private float teleportCooldownTimer;
    private bool  projectileFiredByAnimationEvent;

    private Vector2 smoothDampVelocity;
    private int     hashAttackBool;
    private int     hashAttackState;
    private Color   originalColor = Color.white;

    private Vector2 wanderTarget;
    private float   wanderChangeTimer;

    // ═══════════════════════════════════════════
    #region Unity Callbacks

    private void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator       == null) animator       = GetComponent<Animator>();

        currentHP             = Mathf.Max(1, maxHP);
        attackCooldownTimer   = Random.Range(0f, attackCooldown);
        teleportCooldownTimer = Random.Range(teleportCooldownMin, teleportCooldownMax);

        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        wanderTarget      = (Vector2)transform.position;
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);

        CacheAnimatorParams();
        TryFindPlayer();
        GatherRoomBounds();

        if (isSpawnLeader) SpawnGroup();
    }

    private void Update()
    {
        if (isDead) return;
        if (player == null) TryFindPlayer();
        if (player == null) return;

        if (attackCooldownTimer   > 0f) attackCooldownTimer   -= Time.deltaTime;
        if (teleportCooldownTimer > 0f) teleportCooldownTimer -= Time.deltaTime;

        wanderChangeTimer -= Time.deltaTime;
        if (wanderChangeTimer <= 0f ||
            Vector2.Distance(rb.position, wanderTarget) < wanderArrivalRadius)
            PickNewWanderTarget();

        float dist = Vector2.Distance(transform.position, player.position);

        if (!isAttacking && !isTeleporting && teleportCooldownTimer <= 0f)
            StartCoroutine(TeleportRoutine());
        else if (!isAttacking && !isTeleporting && attackCooldownTimer <= 0f && dist <= attackRange)
            StartCoroutine(AttackRoutine());

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        if (isDead || rb == null) return;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float   distance = toPlayer.magnitude;

        float moveMul = 1f;
        if (isAttacking)   moveMul = attackMoveMultiplier;
        if (isTeleporting) moveMul = teleportMoveMultiplier;

        Vector2 desired = ComputeDesiredVelocity(toPlayer, distance) * moveMul;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, desired,
            ref smoothDampVelocity, Mathf.Max(0.01f, smoothTime));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        // Kendi mermilerini yoksay (BabyProjectile VEYA PooterProjectile)
        if (other.GetComponent<BabyProjectile>()   != null) return;
        if (other.GetComponent<PooterProjectile>()  != null) return;

        // Sadece Explosion layer'ındaki objeler hasar verir
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeDamage(1, knockDir);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag(playerTag)) KillPlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag(playerTag)) KillPlayer(collision.gameObject);
    }

    private void OnDisable()
    {
        // Sahne değişimi veya nesne disable'da rengi sıfırla
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Room Bounds

    /// <summary>
    /// Baby'nin etrafındaki Wall/Door collider'larından, her yönde
    /// baby'yi çevreleyen EN YAKIN sınırı bulur.
    ///
    /// Sol  sınır = baby'nin solundaki collider'ların sağ kenarlarının en büyüğü
    /// Sağ  sınır = baby'nin sağındaki collider'ların sol kenarlarının en küçüğü
    /// Alt  sınır = baby'nin altındaki collider'ların üst kenarlarının en büyüğü
    /// Üst  sınır = baby'nin üstündeki collider'ların alt kenarlarının en küçüğü
    ///
    /// Bu şekilde kapı açıklıkları veya köşeler probleme yol açmaz.
    /// </summary>
    /// <summary>
    /// Baby'nin pozisyonuyla dikey/yatay olarak ÇAKIŞAN duvar/kapı
    /// collider'larından oda sınırlarını hesaplar.
    /// Komşu odaların duvarları çakışma şartını sağlamadığından elenir.
    ///
    /// Sol/Sağ sınır  → baby'nin y değerini dikey olarak kapsayan collider'lar
    /// Alt/Üst sınır  → baby'nin x değerini yatay olarak kapsayan collider'lar
    /// </summary>
    private void GatherRoomBounds()
    {
        Vector2 origin = transform.position;
        float   search = 40f;

        // Başlangıçta çok uzak değerler — bulunamazsa fallback
        float bestLeft  = origin.x - 8f;
        float bestRight = origin.x + 8f;
        float bestDown  = origin.y - 5f;
        float bestUp    = origin.y + 5f;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, search);
        foreach (var col in nearby)
        {
            if (col == ownCollider) continue;
            if (!col.CompareTag(wallTag) && !col.CompareTag(doorTag)) continue;

            Bounds b = col.bounds;

            // SOL: collider baby'nin solunda VE dikey olarak baby'yi kapsıyor
            if (b.max.x <= origin.x && b.min.y <= origin.y && b.max.y >= origin.y)
                bestLeft = Mathf.Max(bestLeft, b.max.x);

            // SAĞ: collider baby'nin sağında VE dikey olarak baby'yi kapsıyor
            if (b.min.x >= origin.x && b.min.y <= origin.y && b.max.y >= origin.y)
                bestRight = Mathf.Min(bestRight, b.min.x);

            // AŞAĞI: collider baby'nin altında VE yatay olarak baby'yi kapsıyor
            if (b.max.y <= origin.y && b.min.x <= origin.x && b.max.x >= origin.x)
                bestDown = Mathf.Max(bestDown, b.max.y);

            // YUKARI: collider baby'nin üstünde VE yatay olarak baby'yi kapsıyor
            if (b.min.y >= origin.y && b.min.x <= origin.x && b.max.x >= origin.x)
                bestUp = Mathf.Min(bestUp, b.min.y);
        }

        roomSafeRect = Rect.MinMaxRect(
            bestLeft  + roomInset,
            bestDown  + roomInset,
            bestRight - roomInset,
            bestUp    - roomInset);
    }

    private bool IsPositionValid(Vector2 pos)
    {
        if (!roomSafeRect.Contains(pos)) return false;

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, teleportValidationRadius);
        foreach (var hit in hits)
        {
            if (hit == ownCollider) continue;
            // Wall veya Door tag'li collider içindeyse geçersiz (trigger olsa da)
            if (hit.CompareTag(wallTag) || hit.CompareTag(doorTag)) return false;
        }
        return true;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Movement & Wander

    private Vector2 ComputeDesiredVelocity(Vector2 toPlayer, float distance)
    {
        if (distance < 0.001f) return Vector2.zero;
        Vector2 dirToPlayer = toPlayer / distance;

        if (distance < panicDistance)
            return -dirToPlayer * moveSpeed * panicSpeedMultiplier;

        Vector2 toWander  = wanderTarget - rb.position;
        Vector2 wanderDir = toWander.sqrMagnitude > 0.001f ? toWander.normalized : dirToPlayer;
        Vector2 blended   = Vector2.Lerp(wanderDir, dirToPlayer, chaseBlend).normalized;

        Vector2 avoidForce = ComputeWallAvoidance();
        Vector2 finalDir   = blended + avoidForce * wallAvoidWeight;
        if (finalDir.sqrMagnitude > 1f) finalDir.Normalize();

        return finalDir * moveSpeed;
    }

    private Vector2 ComputeWallAvoidance()
    {
        Vector2 avoidForce = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(rb.position, wallAvoidDistance);
        foreach (var w in nearby)
        {
            if (w.isTrigger || w == ownCollider) continue;
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
        // Fallback: oda merkezine doğru
        wanderTarget      = roomSafeRect.center;
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null || player == null) return;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) spriteRenderer.flipX = dx < 0f;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Attack Routine

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        SetAttackAnim(true);
        projectileFiredByAnimationEvent = false;

        float elapsed = 0f;
        while (!projectileFiredByAnimationEvent && elapsed < 0.5f)
        {
            TryFireFromAnimatorStateProgress();
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!projectileFiredByAnimationEvent) FireAtPlayer();

        yield return new WaitForSeconds(Mathf.Max(0f, attackAfterFireSeconds));

        SetAttackAnim(false);
        isAttacking = false;
        attackCooldownTimer = attackCooldown;
    }

    public void AnimationEvent_FireProjectile()
    {
        if (isDead || !isAttacking || projectileFiredByAnimationEvent) return;
        FireAtPlayer();
        projectileFiredByAnimationEvent = true;
    }

    private void TryFireFromAnimatorStateProgress()
    {
        if (projectileFiredByAnimationEvent || animator == null || hashAttackState == 0) return;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash != hashAttackState) return;
        FireAtPlayer();
        projectileFiredByAnimationEvent = true;
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
    #region Teleport Routine

    private IEnumerator TeleportRoutine()
    {
        if (isTeleporting) yield break;
        isTeleporting = true;

        float t;

        // ── FADE OUT: Orijinal → Beyaz ──────────────
        t = 0f;
        while (t < teleportFadeOutDuration && !isDead)
        {
            t += Time.deltaTime;
            SetSpriteColor(Color.Lerp(originalColor, Color.white, t / teleportFadeOutDuration));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetSpriteColor(Color.white);

        // ── FADE OUT: Beyaz → Şeffaf ─────────────────
        t = 0f;
        while (t < teleportFadeOutDuration && !isDead)
        {
            t += Time.deltaTime;
            SetSpriteColor(new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / teleportFadeOutDuration)));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetSpriteColor(new Color(1f, 1f, 1f, 0f));

        // ── GÖRÜNMEZKEN: Konumu değiştir ────────────
        yield return new WaitForSeconds(teleportHoldDuration);
        if (isDead) { FinishTeleport(); yield break; }

        Vector2 newPos    = FindTeleportPosition();
        rb.position        = newPos;
        transform.position = (Vector3)newPos;
        rb.linearVelocity  = Vector2.zero;
        smoothDampVelocity = Vector2.zero;

        // ── FADE IN: Şeffaf → Beyaz ──────────────────
        t = 0f;
        while (t < teleportFadeInDuration && !isDead)
        {
            t += Time.deltaTime;
            SetSpriteColor(new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, t / teleportFadeInDuration)));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetSpriteColor(Color.white);

        // ── FADE IN: Beyaz → Orijinal ────────────────
        t = 0f;
        while (t < teleportFadeInDuration && !isDead)
        {
            t += Time.deltaTime;
            SetSpriteColor(Color.Lerp(Color.white, originalColor, t / teleportFadeInDuration));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetSpriteColor(originalColor);

        yield return new WaitForSeconds(teleportAfterSeconds);

        FinishTeleport();
        teleportCooldownTimer = Random.Range(teleportCooldownMin, teleportCooldownMax);
    }

    // Teleport'un her çıkış yolunda çağrılır — renk ve flag garantili sıfırlanır
    private void FinishTeleport()
    {
        SetSpriteColor(originalColor);
        isTeleporting = false;
    }

    private Vector2 FindTeleportPosition()
    {
        Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.zero;

        for (int i = 0; i < teleportMaxAttempts; i++)
        {
            float   x         = Random.Range(roomSafeRect.xMin, roomSafeRect.xMax);
            float   y         = Random.Range(roomSafeRect.yMin, roomSafeRect.yMax);
            Vector2 candidate = new Vector2(x, y);

            float d = Vector2.Distance(candidate, playerPos);
            if (d < teleportMinDistFromPlayer || d > teleportMaxDistFromPlayer) continue;
            if (IsPositionValid(candidate)) return candidate;
        }

        // Fallback: sadece rect + fizik kontrolü (mesafe şartı yok)
        for (int i = 0; i < 20; i++)
        {
            float   x         = Random.Range(roomSafeRect.xMin, roomSafeRect.xMax);
            float   y         = Random.Range(roomSafeRect.yMin, roomSafeRect.yMax);
            Vector2 candidate = new Vector2(x, y);
            if (IsPositionValid(candidate)) return candidate;
        }

        return rb.position; // son çare: mevcut konumda kal
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Spawn Group

    private void SpawnGroup()
    {
        if (babyPrefab == null || spawnCount <= 0) return;
        for (int i = 0; i < spawnCount; i++)
        {
            float   angle   = (360f / spawnCount) * i * Mathf.Deg2Rad;
            Vector2 offset  = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            GameObject sp   = Instantiate(babyPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            BabyAI     ai   = sp.GetComponent<BabyAI>();
            if (ai != null) ai.isSpawnLeader = false;
        }
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Damage / Death

    public void TakeDamage(int damage, Vector2 knockDir)
    {
        if (isDead) return;
        currentHP -= Mathf.Max(1, damage);
        if (rb != null) rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        if (!isTeleporting) StartCoroutine(HitFlashRoutine());

        if (currentHP <= 0) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        SetSpriteColor(hitFlashColor);
        yield return new WaitForSeconds(Mathf.Max(0f, hitFlashDuration));
        // Teleport başlamışsa rengi değiştirme
        if (!isTeleporting) SetSpriteColor(originalColor);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        SetSpriteColor(originalColor);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (ownCollider != null) ownCollider.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated      = false;
        }

        Destroy(gameObject);
    }

    private void KillPlayer(GameObject playerObj)
    {
        IsaacMovement isaac = playerObj.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.DeathSequence();
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Helpers

    private void SetSpriteColor(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = c;
    }

    private void CacheAnimatorParams()
    {
        hashAttackBool  = string.IsNullOrEmpty(animAttackBoolParam)
            ? 0 : Animator.StringToHash(animAttackBoolParam);
        hashAttackState = string.IsNullOrWhiteSpace(attackStateName)
            ? 0 : Animator.StringToHash(attackStateName);
    }

    private void SetAttackAnim(bool value)
    {
        if (animator == null || hashAttackBool == 0) return;
        animator.SetBool(hashAttackBool, value);
    }

    private void TryFindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && roomSafeRect.width > 0)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
            Vector3 c = new Vector3(roomSafeRect.center.x, roomSafeRect.center.y, 0f);
            Vector3 s = new Vector3(roomSafeRect.width, roomSafeRect.height, 0f);
            Gizmos.DrawWireCube(c, s);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
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
}