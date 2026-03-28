using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PooterAI : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 8;
    public float moveSpeed = 1.65f;

    [Header("Target")]
    public string playerTag = "Player";

    [Header("Attack")]
    public float attackRange = 6f;
    public float attackCooldown = 2.5f;
    public float attackWindupSeconds = 0.2f;
    public float attackAfterFireSeconds = 0.15f;
    [Tooltip("Saldırı animasyonunda event gelmezse yedek atış süresi.")]
    public float attackEventTimeoutSeconds = 0.4f;
    [Tooltip("PooterAttack animasyonunda merminin çıkacağı normalize zaman (0-1).")]
    [Range(0f, 1f)] public float attackFireNormalizedTime = 0.53f;
    public string attackStateName = "PooterAttack";
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("Movement (Isaac-style ring)")]
    [Tooltip("Oyuncuya çok yakınsa hızlıca geri çekilir.")]
    public float panicDistance = 1.35f;
    [Tooltip("Bu mesafenin altında geri iter, üstünde (panic dışında) ideal halkaya dönmeye çalışır.")]
    public float innerComfortDistance = 2.2f;
    [Tooltip("Bu mesafenin üstünde oyuncuya doğru yaklaşır.")]
    public float outerComfortDistance = 4.2f;
    public float smoothTime = 0.42f;
    [Range(0f, 1f)]
    public float attackMoveMultiplier = 0f;
    public float orbitTangentWeight = 0.88f;
    public float orbitRadialWeight = 0.12f;
    public float fleeSpeedMultiplier = 1.55f;

    [Header("Wall Avoidance")]
    public float wallAvoidDistance = 1.5f;
    public float wallAvoidWeight = 2f;
    public string wallTag = "Wall";

    [Header("Feedback")]
    public float knockbackForce = 4f;
    public float hitFlashDuration = 0.12f;
    public Color hitFlashColor = Color.red;
    public GameObject deathEffectPrefab;
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    [Tooltip("Animator'da yoksa boş bırakın (sadece isAttacking kullanılıyorsa).")]
    public string animIsMovingBoolParam = "";
    public string animAttackBoolParam = "isAttacking";
    [Tooltip("İsteğe bağlı; boş bırakılırsa tetik gönderilmez.")]
    public string animAttackTriggerParam = "";

    [Header("Runtime (Debug)")]
    public Rigidbody2D rb;
    public Transform player;
    public int currentHP;
    public float cooldownTimer;
    public bool isAttacking;
    public bool isDead;
    public bool projectileFiredByAnimationEvent;
    private bool isActivated = false;

    [Header("Debug Logs")]
    public bool enableDebugLogs = true;
    [Tooltip("Aynı hareket durumunu kaç saniyede bir tekrar loglayalım.")]
    public float repeatStateLogInterval = 1f;

    private Vector2 smoothDampVelocity;
    private Coroutine flashRoutine;
    private int hashAttackBool;
    private int hashMovingBool;
    private int hashAttackState;
    private bool hasMovingParam;
    private Color originalColor = Color.white;
    private string lastMoveState;
    private float nextStateLogTime;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        currentHP = Mathf.Max(1, maxHP);
        cooldownTimer = Random.Range(0f, attackCooldown * 0.5f);
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        CacheAnimatorParams();
        TryFindPlayer();
        Log("Awake tamamlandi.");
    }

    private void CacheAnimatorParams()
    {
        hashAttackBool = string.IsNullOrEmpty(animAttackBoolParam)
            ? 0
            : Animator.StringToHash(animAttackBoolParam);
        hasMovingParam = !string.IsNullOrWhiteSpace(animIsMovingBoolParam);
        hashMovingBool = hasMovingParam ? Animator.StringToHash(animIsMovingBoolParam) : 0;
        hashAttackState = string.IsNullOrWhiteSpace(attackStateName) ? 0 : Animator.StringToHash(attackStateName);
    }

    private void Update()
    {
        if (!isActivated) return;
        if (isDead) return;

        if (player == null) TryFindPlayer();
        if (player == null) return;

        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);
        if (!isAttacking && cooldownTimer <= 0f && distance <= attackRange)
        {
            Log($"Saldiri baslatildi. distance={distance:F2}, cooldown={cooldownTimer:F2}");
            StartCoroutine(AttackRoutine());
        }

        UpdateFacing();
    }

    private void UpdateFacing()
    {
        if (spriteRenderer == null || player == null) return;
        float dx = player.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f)
            spriteRenderer.flipX = dx < 0f;
    }

    private void FixedUpdate()
    {
        if (isDead || rb == null) return;
        if (!isActivated) { rb.linearVelocity = Vector2.zero; return; }
        if (player == null)
        {
            rb.linearVelocity = Vector2.zero;
            SetMovingAnim(false);
            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float distance = toPlayer.magnitude;

        float moveMul = isAttacking ? attackMoveMultiplier : 1f;
        Vector2 desiredVelocity = ComputeDesiredVelocity(toPlayer, distance) * moveMul;

        Vector2 vel = Vector2.SmoothDamp(rb.linearVelocity, desiredVelocity, ref smoothDampVelocity, Mathf.Max(0.01f, smoothTime));
        rb.linearVelocity = vel;

        SetMovingAnim(vel.sqrMagnitude > 0.01f);
        LogMovementState(distance, vel);
    }

    private Vector2 ComputeDesiredVelocity(Vector2 toPlayer, float distance)
    {
        if (distance < 0.001f) return Vector2.zero;

        Vector2 dirToPlayer = toPlayer / distance;

        if (distance < panicDistance)
            return -dirToPlayer * moveSpeed * fleeSpeedMultiplier;

        if (distance > outerComfortDistance)
            return dirToPlayer * moveSpeed;

        if (distance < innerComfortDistance)
            return -dirToPlayer * moveSpeed;

        float mid = (innerComfortDistance + outerComfortDistance) * 0.5f;
        Vector2 radial = distance > mid ? dirToPlayer : -dirToPlayer;
        Vector2 tangent = new Vector2(-dirToPlayer.y, dirToPlayer.x);
        Vector2 blended = tangent * orbitTangentWeight + radial * orbitRadialWeight;
        if (blended.sqrMagnitude < 0.0001f) blended = Vector2.zero;
        else blended = blended.normalized;

        Vector2 avoidForce = Vector2.zero;
    {
        Collider2D[] walls = Physics2D.OverlapCircleAll(rb.position, wallAvoidDistance);
        foreach (var w in walls)
        {
            if (w.isTrigger) continue;
            if (!w.CompareTag(wallTag)) continue; // sadece "Wall" tag'li collider'lar
            Vector2 closestPoint = w.ClosestPoint(rb.position);
            Vector2 diff = rb.position - closestPoint;
            float dist = diff.magnitude;
            if (dist > 0.001f && dist < wallAvoidDistance)
            {
                avoidForce += (diff / dist) * ((wallAvoidDistance - dist) / wallAvoidDistance);
            }
        }
    }

        Vector2 finalDir = blended + avoidForce * wallAvoidWeight;
        if (finalDir.sqrMagnitude > 1f) finalDir.Normalize();
        
        return finalDir * moveSpeed;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        projectileFiredByAnimationEvent = false;
        SetAttackAnim(true);

        float timeout = Mathf.Max(attackWindupSeconds, attackEventTimeoutSeconds);
        float elapsed = 0f;
        while (!projectileFiredByAnimationEvent && elapsed < timeout)
        {
            TryFireFromAnimatorStateProgress();
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!projectileFiredByAnimationEvent)
        {
            LogWarning("Animasyon event'i gelmedi, fallback atis kullanildi.");
            FireAtPlayer();
            projectileFiredByAnimationEvent = true;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, attackAfterFireSeconds));

        SetAttackAnim(false);
        isAttacking = false;
        cooldownTimer = Mathf.Max(0.05f, attackCooldown);
        Log("Saldiri bitti, cooldown resetlendi.");
    }

    // Animation Event: PooterAttack clip'i icinden cagirilir.
    public void AnimationEvent_FireProjectile()
    {
        if (isDead || !isAttacking || projectileFiredByAnimationEvent) return;
        FireAtPlayer();
        projectileFiredByAnimationEvent = true;
        Log("AnimationEvent ile projectile firlatildi.");
    }

    private void TryFireFromAnimatorStateProgress()
    {
        if (projectileFiredByAnimationEvent || animator == null || hashAttackState == 0) return;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.shortNameHash != hashAttackState) return;

        float normalized = info.normalizedTime % 1f;
        if (normalized < attackFireNormalizedTime) return;

        FireAtPlayer();
        projectileFiredByAnimationEvent = true;
        Log($"Animator state progress ile fire. normalized={normalized:F2}");
    }

    private void FireAtPlayer()
    {
        if (projectilePrefab == null || player == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = ((Vector2)player.position - (Vector2)spawnPos).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        PooterProjectile projectile = go.GetComponent<PooterProjectile>();
        if (projectile != null)
            projectile.Initialize(dir);
        else
        {
            Rigidbody2D projectileRb = go.GetComponent<Rigidbody2D>();
            if (projectileRb != null) projectileRb.linearVelocity = dir * 4.5f;
        }

        Log($"Projectile spawn. pos={spawnPos}, dir={dir}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.GetComponent<PooterProjectile>() != null) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeDamage(1, knockDir);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag(playerTag))
        {
            var isaac = collision.gameObject.GetComponent<IsaacMovement>();
            if (isaac != null) isaac.DeathSequence();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (collision.gameObject.CompareTag(playerTag))
        {
            var isaac = collision.gameObject.GetComponent<IsaacMovement>();
            if (isaac != null) isaac.DeathSequence();
        }
    }

    private void TakeDamage(int damage, Vector2 knockDir)
    {
        currentHP -= Mathf.Max(1, damage);
        if (rb != null) rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        if (currentHP <= 0)
            Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(Mathf.Max(0f, hitFlashDuration));
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        Destroy(gameObject);
    }

    private void TryFindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    private void SetMovingAnim(bool value)
    {
        if (animator == null || !hasMovingParam) return;
        animator.SetBool(hashMovingBool, value);
    }

    private void SetAttackAnim(bool value)
    {
        if (animator == null) return;

        if (hashAttackBool != 0)
            animator.SetBool(hashAttackBool, value);

        if (value && !string.IsNullOrWhiteSpace(animAttackTriggerParam))
            animator.SetTrigger(animAttackTriggerParam);
    }

    private void LogMovementState(float distance, Vector2 velocity)
    {
        if (!enableDebugLogs) return;

        string state;
        if (distance < panicDistance) state = "Flee";
        else if (distance > outerComfortDistance) state = "Chase";
        else if (distance < innerComfortDistance) state = "Backoff";
        else state = "Orbit";

        bool sameState = state == lastMoveState;
        bool timeReady = Time.time >= nextStateLogTime;
        if (!sameState || timeReady)
        {
            Debug.Log($"[PooterAI:{name}] state={state} dist={distance:F2} vel={velocity}");
            lastMoveState = state;
            nextStateLogTime = Time.time + Mathf.Max(0.1f, repeatStateLogInterval);
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[PooterAI:{name}] {message}");
    }

    private void LogWarning(string message)
    {
        if (!enableDebugLogs) return;
        Debug.LogWarning($"[PooterAI:{name}] {message}");
    }

    public void Activate()
    {
        isActivated = true;
    }

    public void Activate(Vector2 spawnPosition)
    {
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        isActivated = true;
    }
}
