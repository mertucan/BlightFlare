using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Binding of Isaac — Loki AI
///
/// Saldırı Modları (rastgele seçilir):
///   A) Çapraz atış  — sağ-alt, sol-alt, sağ-üst, sol-üst  (45° arası) aynı anda 4 mermi
///   B) Artı atış    — sağ, sol, yukarı, aşağı               aynı anda 4 mermi
///
/// Diğer davranışlar:
///   - BabyAI gibi wander + SmoothDamp hareketi, duvar kaçınma
///   - Belirli aralıklarla BoomFly spawn eder (DOF'daki Pooter mantığı)
///   - BabyAI'daki tam teleport dizisi (fade-out → konum değiştir → fade-in)
///   - Işınlanma SADECE boşta — saldırı / spawn sırasında tetiklenmez
///   - DOF kadar can (bombsToKill)
///   - Oyuncuya temas hasarı
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class LokiAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // HAREKET & WANDER  (BabyAI'dan)
    // ─────────────────────────────────────────────
    [Header("Hareket")]
    public float moveSpeed  = 2f;
    public float smoothTime = 0.55f;
    [Range(0f, 1f)] public float attackMoveMultiplier = 0f;

    [Header("Wander")]
    public float wanderArrivalRadius     = 0.6f;
    public float wanderChangeIntervalMin = 1.8f;
    public float wanderChangeIntervalMax = 3.5f;
    [Range(0f, 1f)] public float chaseBlend = 0.25f;
    public float wanderRadius = 3f;

    [Header("Duvar Kaçınma")]
    public float wallAvoidDistance = 1.2f;
    public float wallAvoidWeight   = 2.5f;

    // ─────────────────────────────────────────────
    // CAN
    // ─────────────────────────────────────────────
    [Header("Can")]
    public int bombsToKill = 5;
    public float knockbackForce  = 4f;
    public float hitFlashDuration = 0.15f;
    public Color hitFlashColor   = Color.red;
    public GameObject deathEffectPrefab;

    [Header("Tunnel (Ölünce Açılır)")]
    [Tooltip("Ölünce aktif olacak SpriteRenderer (tunnel görseli).")]
    public SpriteRenderer tunnelSpriteRenderer;
    [Tooltip("Ölünce aktif olacak Collider (tunnel girişi).")]
    public Collider2D tunnelCollider2D;

    // ─────────────────────────────────────────────
    // SPRITE / ANİMASYON
    // Kare 0 → idle (değişim yok, sabit)
    // Kare 1 → çapraz atış  (X)  yapılır
    // Kare 2 → düz atış     (+)  yapılır
    // ─────────────────────────────────────────────
    [Header("Sprite Animasyonu")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("Kare 0 — idle, dolaşırken gösterilir, atış yok.")]
    public Sprite spriteFrame0;
    [Tooltip("Kare 1 — bu kareye geçince çapraz (X) atış yapılır.")]
    public Sprite spriteFrame1;
    [Tooltip("Kare 2 — bu kareye geçince düz (+) atış yapılır.")]
    public Sprite spriteFrame2;

    [Tooltip("Her kare bu kadar saniye görünür (büyüt = yavaşla).")]
    public float frameDuration = 0.35f;

    // ─────────────────────────────────────────────
    // SALDIRI
    // ─────────────────────────────────────────────
    [Header("Saldırı")]
    public GameObject projectilePrefab;
    public Transform  firePoint;
    public float attackCooldownMin = 2.5f;
    public float attackCooldownMax = 4.5f;

    // ─────────────────────────────────────────────
    // BOOMFLY SPAWN
    // ─────────────────────────────────────────────
    [Header("BoomFly Spawn")]
    public GameObject boomFlyPrefab;
    public float boomFlySpawnRadius   = 1.5f;
    public int   maxBoomFlies         = 3;
    public float boomFlySpawnCooldown = 15f;

    [Tooltip("Spawn animasyonu kare A — ilk gösterilecek sprite.")]
    public Sprite spawnSpriteA;
    [Tooltip("Spawn animasyonu kare B — spawn anında gösterilecek sprite.")]
    public Sprite spawnSpriteB;
    [Tooltip("Her spawn karesinin süresi (saniye).")]
    public float spawnFrameDuration = 0.3f;

    // ─────────────────────────────────────────────
    // IŞINLANMA  (BabyAI'dan birebir)
    // ─────────────────────────────────────────────
    [Header("Işınlanma")]
    public float teleportCooldownMin = 4f;
    public float teleportCooldownMax = 8f;
    public float teleportFadeOutDuration = 0.2f;
    public float teleportHoldDuration    = 0.1f;
    public float teleportFadeInDuration  = 0.2f;
    public float teleportAfterSeconds    = 0.15f;
    public float teleportMinDistFromPlayer = 2.5f;
    public float teleportMaxDistFromPlayer = 6f;
    public float teleportValidationRadius  = 0.3f;
    public int   teleportMaxAttempts       = 60;

    // ─────────────────────────────────────────────
    // ODA SINIRI (BabyAI'dan)
    // ─────────────────────────────────────────────
    [Header("Oda Inset")]
    public float roomInsetLeft   = 2.0f;
    public float roomInsetRight  = 2.5f;
    public float roomInsetBottom = 2.0f;
    public float roomInsetTop    = 2.0f;

    [Header("Duvar & Kapı Tag")]
    public string wallTag = "Wall";
    public string doorTag = "Door";

    // ─────────────────────────────────────────────
    // OYUNCU TEMAS HASARI
    // ─────────────────────────────────────────────
    [Header("Oyuncu")]
    public string playerTag = "Player";
    public float damageCooldown = 1f;

    // ─────────────────────────────────────────────
    // SES
    // ─────────────────────────────────────────────
    [Header("Ses")]
    public AudioSource audioSource;
    public AudioClip attackClip;
    public AudioClip idleClip;
    public AudioClip deathClip;
    public AudioClip deathMusic;
    [Range(0f, 1f)] public float deathMusicVolume = 1f;

    // ─────────────────────────────────────────────
    // RUNTIME DEBUG
    // ─────────────────────────────────────────────
    [Header("Runtime (Debug — Read Only)")]
    private int   currentBombHits;
    [SerializeField] private bool  isDead;
    [SerializeField] private bool  isAttacking;
    [SerializeField] private bool  isTeleporting;
    [SerializeField] private bool  isSpawning;
    [SerializeField] private Rect  roomSafeRect;

    // ─── privates ────────────────────────────────
    private Rigidbody2D rb;
    private Collider2D  col;
    private Transform   player;

    // wander (BabyAI)
    private Vector2 wanderTarget;
    private float   wanderChangeTimer;
    private Vector2 smoothDampVelocity;

    private bool  isActivated = false;

    private float attackTimer;
    private float teleportTimer;
    private float boomFlyTimer;
    private float lastDamageTime   = -999f;  // bomba için
    private float lastPlayerDamageTime = -999f;  // oyuncu teması için

    private Color     originalColor = Color.white;
    private Coroutine flashRoutine;
    private List<GameObject> activeBoomFlies = new List<GameObject>();

    // sprite animasyon
    private float animTimer;
    private int   animFrame;   // 0, 1, 2 arası döner
    private bool  inAttackAnim;
    private bool  inSpawnAnim;

    // ═══════════════════════════════════════════
    #region Unity Callbacks

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        Debug.Log($"Loki Awake: bombsToKill={bombsToKill}, currentBombHits={currentBombHits}");

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        // Başlangıç zamanlayıcıları — hemen saldırmasın
        attackTimer   = Random.Range(attackCooldownMin, attackCooldownMax);
        teleportTimer = Random.Range(teleportCooldownMin, teleportCooldownMax);
        boomFlyTimer  = boomFlySpawnCooldown;

        wanderTarget      = (Vector2)transform.position;
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);

        TryFindPlayer();
    }

    private void Start()
    {
        if (spriteRenderer != null && spriteFrame0 != null)
            spriteRenderer.sprite = spriteFrame0;

        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = false;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = false;
    }

    private void Update()
    {
        if (isDead || !isActivated) return;

        if (player == null) TryFindPlayer();
        if (player == null) return;

        // Wander hedef zamanlayıcısı — aksiyon sırasında da wander hesaplanabilir,
        // ama FixedUpdate'te hareket durdurulur.
        wanderChangeTimer -= Time.deltaTime;
        if (wanderChangeTimer <= 0f ||
            Vector2.Distance(rb.position, wanderTarget) < wanderArrivalRadius)
            PickNewWanderTarget();

        bool busy = isAttacking || isTeleporting || isSpawning;

        if (!busy)
        {
            attackTimer   -= Time.deltaTime;
            teleportTimer -= Time.deltaTime;
            boomFlyTimer  -= Time.deltaTime;

            // Öncelik: ışınlanma > saldırı > spawn
            // Saldırı VEYA spawn sırasında ışınlanma BAŞLATILMAZ
            if (teleportTimer <= 0f)
                StartCoroutine(TeleportRoutine());
            else if (attackTimer <= 0f)
                StartCoroutine(AttackRoutine());
            else if (boomFlyTimer <= 0f)
                StartCoroutine(SpawnBoomFlyRoutine());
        }

        UpdateSpriteAnimation();
    }

    private void FixedUpdate()
    {
        if (isDead || !isActivated || rb == null) return;

        if (player == null) { rb.linearVelocity = Vector2.zero; return; }

        // Saldırı / ışınlanma / spawn sırasında tamamen dur
        float moveMul = (isAttacking || isSpawning) ? attackMoveMultiplier : 1f;
        if (isTeleporting) moveMul = 0f;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        Vector2 desired  = ComputeDesiredVelocity(toPlayer) * moveMul;

        rb.linearVelocity = Vector2.SmoothDamp(
            rb.linearVelocity, desired,
            ref smoothDampVelocity, Mathf.Max(0.01f, smoothTime));
    }

    // ─── Wander hareketi (BabyAI'dan) ────────────
    private Vector2 ComputeDesiredVelocity(Vector2 toPlayer)
    {
        float distance = toPlayer.magnitude;
        if (distance < 0.001f) return Vector2.zero;

        Vector2 dirToPlayer = toPlayer / distance;
        Vector2 toWander    = wanderTarget - rb.position;
        Vector2 wanderDir   = toWander.sqrMagnitude > 0.001f ? toWander.normalized : dirToPlayer;
        Vector2 blended     = Vector2.Lerp(wanderDir, dirToPlayer, chaseBlend).normalized;

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
        // Fallback: oda merkezi
        wanderTarget      = roomSafeRect.center;
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);
    }

    // Temas hasarı — Collision yerine sadece Stay ile kontrol (wander'da fizik çarpışması yok)
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;
        if (other.GetComponent<BabyProjectile>()  != null) return;
        if (other.GetComponent<PooterProjectile>() != null) return;

        float now = Time.time;
        if (now - lastDamageTime < damageCooldown) return;
        lastDamageTime = now + 999f; // ← diğer trigger'ları anında blokla

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeBombDamage(knockDir);
        
        lastDamageTime = now; // ← gerçek değere set et
    }

    private void OnDisable()
    {
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Sprite Animasyonu

    private void UpdateSpriteAnimation()
    {
        if (spriteRenderer == null) return;

        // Spawn animasyonu aktifken bu metod sprite'a dokunmaz
        if (inSpawnAnim) return;

        // Saldırı animasyonu yoksa → idle sabit
        if (!inAttackAnim)
        {
            if (spriteFrame0 != null) spriteRenderer.sprite = spriteFrame0;
            return;
        }

        animTimer += Time.deltaTime;
        if (animTimer < frameDuration) return;
        animTimer -= frameDuration;

        animFrame++;

        // Kare 1: çapraz atış (X)
        if (animFrame == 1)
        {
            if (spriteFrame1 != null) spriteRenderer.sprite = spriteFrame1;
            FireDiagonal();
            PlayClip(attackClip);
        }
        // Kare 2: düz atış (+)
        else if (animFrame == 2)
        {
            if (spriteFrame2 != null) spriteRenderer.sprite = spriteFrame2;
            FireCardinal();
            PlayClip(attackClip);
        }
        // Kare 3+: animasyon bitti, idle'a dön
        else if (animFrame >= 3)
        {
            animFrame    = 0;
            inAttackAnim = false;
            if (spriteFrame0 != null) spriteRenderer.sprite = spriteFrame0;
        }
    }

    private void SetAttackAnimState(bool active)
    {
        inAttackAnim = active;
        animFrame    = 0;
        animTimer    = 0f;
        if (!active && spriteFrame0 != null && spriteRenderer != null)
            spriteRenderer.sprite = spriteFrame0;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Saldırı

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        SetAttackAnimState(true);

        // Animasyon bitene kadar bekle (3 kare × frameDuration)
        yield return new WaitUntil(() => !inAttackAnim);

        isAttacking = false;
        attackTimer = Random.Range(attackCooldownMin, attackCooldownMax);
    }

    // Çapraz (X): sağ-alt, sol-alt, sağ-üst, sol-üst
    private void FireDiagonal()
    {
        float s = Mathf.Sqrt(0.5f);
        FireInDirections(new Vector2[]
        {
            new Vector2( s, -s),
            new Vector2(-s, -s),
            new Vector2( s,  s),
            new Vector2(-s,  s),
        });
    }

    // Düz (+): sağ, sol, yukarı, aşağı
    private void FireCardinal()
    {
        FireInDirections(new Vector2[]
        {
            Vector2.right,
            Vector2.left,
            Vector2.up,
            Vector2.down,
        });
    }

    private void FireInDirections(Vector2[] dirs)
    {
        if (projectilePrefab == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        foreach (var dir in dirs)
        {
            GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            BabyProjectile bp = go.GetComponent<BabyProjectile>();
            if (bp != null) { bp.Initialize(dir); continue; }

            PooterProjectile pp = go.GetComponent<PooterProjectile>();
            if (pp != null) { pp.Initialize(dir); continue; }

            Rigidbody2D projRb = go.GetComponent<Rigidbody2D>();
            if (projRb != null) projRb.linearVelocity = dir * 4f;
        }
    }

    #endregion

    // ═══════════════════════════════════════════
    #region BoomFly Spawn (DOF mantığı)

    private IEnumerator SpawnBoomFlyRoutine()
    {
        if (boomFlyPrefab == null)
        {
            boomFlyTimer = boomFlySpawnCooldown;
            yield break;
        }

        activeBoomFlies.RemoveAll(b => b == null);
        if (activeBoomFlies.Count >= maxBoomFlies)
        {
            boomFlyTimer = boomFlySpawnCooldown;
            yield break;
        }

        isSpawning  = true;
        inSpawnAnim = true;
        PlayClip(attackClip);

        // Kare A
        if (spriteRenderer != null && spawnSpriteA != null)
            spriteRenderer.sprite = spawnSpriteA;
        yield return new WaitForSeconds(spawnFrameDuration);

        // Kare B — spawn bu anda gerçekleşir
        if (spriteRenderer != null && spawnSpriteB != null)
            spriteRenderer.sprite = spawnSpriteB;

        float   angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset   = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * boomFlySpawnRadius;
        Vector3 spawnPos = firePoint != null
            ? firePoint.position + (Vector3)offset
            : transform.position + (Vector3)offset;

        GameObject fly = Instantiate(boomFlyPrefab, spawnPos, Quaternion.identity);
        activeBoomFlies.Add(fly);

        BoomFlyAI bfAI = fly.GetComponent<BoomFlyAI>();
        if (bfAI != null) bfAI.Activate();

        yield return new WaitForSeconds(spawnFrameDuration);

        // Idle'a dön
        if (spriteRenderer != null && spriteFrame0 != null)
            spriteRenderer.sprite = spriteFrame0;

        inSpawnAnim  = false;
        isSpawning   = false;
        boomFlyTimer = boomFlySpawnCooldown;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Işınlanma (BabyAI'dan)

    private IEnumerator TeleportRoutine()
    {
        if (isTeleporting) yield break;
        isTeleporting = true;

        float t;

        // Fade Out: Orijinal → Beyaz
        t = 0f;
        while (t < teleportFadeOutDuration && !isDead)
        {
            t += Time.deltaTime;
            SetColor(Color.Lerp(originalColor, Color.white, t / teleportFadeOutDuration));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetColor(Color.white);

        // Fade Out: Beyaz → Şeffaf
        t = 0f;
        while (t < teleportFadeOutDuration && !isDead)
        {
            t += Time.deltaTime;
            SetColor(new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / teleportFadeOutDuration)));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetColor(new Color(1f, 1f, 1f, 0f));

        yield return new WaitForSeconds(teleportHoldDuration);
        if (isDead) { FinishTeleport(); yield break; }

        // Konum değiştir
        Vector2 newPos     = FindTeleportPosition();
        rb.position        = newPos;
        transform.position = (Vector3)newPos;
        rb.linearVelocity  = Vector2.zero;

        // ışınlanma sesi yok
        PickNewWanderTarget(); // ışınlanınca yeni wander hedefi seç

        // Fade In: Şeffaf → Beyaz
        t = 0f;
        while (t < teleportFadeInDuration && !isDead)
        {
            t += Time.deltaTime;
            SetColor(new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, t / teleportFadeInDuration)));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetColor(Color.white);

        // Fade In: Beyaz → Orijinal
        t = 0f;
        while (t < teleportFadeInDuration && !isDead)
        {
            t += Time.deltaTime;
            SetColor(Color.Lerp(Color.white, originalColor, t / teleportFadeInDuration));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetColor(originalColor);

        yield return new WaitForSeconds(teleportAfterSeconds);

        FinishTeleport();
        teleportTimer = Random.Range(teleportCooldownMin, teleportCooldownMax);
    }

    private void FinishTeleport()
    {
        SetColor(originalColor);
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

        // Fallback: mesafe şartı yok
        for (int i = 0; i < 20; i++)
        {
            float   x         = Random.Range(roomSafeRect.xMin, roomSafeRect.xMax);
            float   y         = Random.Range(roomSafeRect.yMin, roomSafeRect.yMax);
            Vector2 candidate = new Vector2(x, y);
            if (IsPositionValid(candidate)) return candidate;
        }

        return rb.position;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Oda Sınırı (BabyAI'dan)

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

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, teleportValidationRadius);
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
    #region Hasar / Ölüm

    private void TakeBombDamage(Vector2 knockDir)
    {
        if (isDead) return;
        currentBombHits++;
        Debug.Log($"Hit {currentBombHits}/{bombsToKill} — time={Time.time}");

        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        if (currentBombHits >= bombsToKill)
        {
            Debug.Log("Die() çağrılıyor!");
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        SetColor(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead && !isTeleporting) SetColor(originalColor);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        SetColor(originalColor);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        BossAudioUtility.Play2D(deathClip);
        BackgroundMusicManager.instance?.PlayBossDeathMusicThenResume(deathMusic, deathMusicVolume);

        // Tunnel aç
        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = true;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = true;

        if (col != null) col.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;

        Destroy(gameObject);
    }

    private void TryDamagePlayer(GameObject playerObj)
    {
        if (Time.time - lastPlayerDamageTime < damageCooldown) return;
        lastPlayerDamageTime = Time.time;

        IsaacMovement isaac = playerObj.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(1);
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

    /// <summary>
    /// idleClip'i AudioSource.clip olarak loop'a alır.
    /// Activate() sırasında çağrılır.
    /// </summary>
    private void StartIdleSound()
    {
        if (audioSource == null || idleClip == null) return;
        audioSource.clip   = idleClip;
        audioSource.loop   = true;
        audioSource.Play();
    }

    private void TryFindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Aktivasyon (diğer AI'larla uyumlu)

    public void Activate()
    {
        GatherRoomBounds();
        PickNewWanderTarget();
        isActivated = true;
        StartIdleSound();
    }

    public void Activate(Vector2 spawnPosition)
    {
        rb.position        = spawnPosition;
        transform.position = (Vector3)spawnPosition;
        GatherRoomBounds();
        PickNewWanderTarget();
        isActivated        = true;
        StartIdleSound();
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
            Vector3 c = new Vector3(roomSafeRect.center.x, roomSafeRect.center.y, 0f);
            Vector3 s = new Vector3(roomSafeRect.width, roomSafeRect.height, 0f);
            Gizmos.DrawWireCube(c, s);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, boomFlySpawnRadius);
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
