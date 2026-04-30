using System.Collections;
using UnityEngine;

/// <summary>
/// Binding of Isaac - Host düşmanı
///
/// STATE MAKİNESİ:
///   Closed  → belli süre bekler → Opening → Open (ateş eder) → Closing → Closed
///   Closed + hasar → Dazed (patlamış, 5 sn) → Closed → döngü
///
/// ÇARPIŞMA MANTIĞI:
///   Closed / Opening / Closing / Dazed  → Ana collider katı (isTrigger=false)
///       Oyuncu çarpabilir, ITEBİLİR (TNT gibi velocity push), CAN GİTMEZ.
///   Open                                → Ana collider trigger (mermiler çarpabilir)
///       Blocker collider AÇIK kalır → oyuncu içinden geçemez + TEMAS HASARI VERİR.
///
/// SPRITE KURULUMU (Inspector):
///   closedSprite  = 1. sprite (kapalı mantar)
///   openSprite    = 2. sprite (açık, savunmasız)
///   dazedSprite   = 3. sprite (patlamış)
///
/// PROJECTILE:
///   bloodPrefab   = PooterProjectile veya benzeri prefab (Blood)
///   firePoint     = opsiyonel; yoksa transform.position kullanılır
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HostAI : MonoBehaviour
{
    // ─── Enum ────────────────────────────────────────────────────────────────
    private enum State { Closed, Opening, Open, Closing, Dazed }

    // ─── Inspector ───────────────────────────────────────────────────────────
    [Header("Sprites")]
    public SpriteRenderer spriteRenderer;
    public Sprite closedSprite;
    public Sprite openSprite;
    public Sprite dazedSprite;

    [Header("Health")]
    [Tooltip("Sadece Open durumunda hasar alınır.")]
    public int maxHP = 6;

    [Header("Timing")]
    public float closedWaitMin  = 5f;
    public float closedWaitMax  = 12f;
    public float openPreFireWait  = 2f;
    public float openFireDelay    = 1f;
    public float openPostFireWait = 2f;
    public float dazedDuration  = 5f;

    [Header("Attack")]
    public GameObject bloodPrefab;
    public Transform  firePoint;
    public int   projectileCount  = 3;
    public float spreadAngleDeg   = 20f;

    [Header("Player")]
    public string playerTag = "Player";

    // ── Push ayarları (TNT mantığıyla aynı) ─────────────────────────────────
    [Header("Push Settings — Host'a özel")]
    [Tooltip("Oyuncunun Host'u itebileceği maksimum hız")]
    public float maxPushSpeed = 2.5f;
    [Tooltip("Host'un linear damping'i (sürtünme)")]
    public float pushDrag = 5f;
    [Tooltip("Oyuncu Host'a çarptığında aldığı geri tepme kuvveti")]
    public float playerBounceForce = 5f;

    // ── Açıkken temas hasarı ─────────────────────────────────────────────────
    [Header("Contact Damage — Open durumu")]
    [Tooltip("Oyuncu açık Host'a değdiğinde alacağı hasar miktarı")]
    public int contactDamage = 1;
    [Tooltip("Aynı oyuncuya temas hasarı vermek için minimum bekleme (saniye)")]
    public float contactDamageCooldown = 1f;

    [Header("Feedback")]
    public float knockbackForce    = 3f;
    public float hitFlashDuration  = 0.1f;
    public Color hitFlashColor     = Color.red;
    public GameObject deathEffectPrefab;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip openClip;
    public AudioClip closeClip;
    public AudioClip fireClip;
    public AudioClip hitClip;
    public AudioClip dazedClip;
    public AudioClip deathClip;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // ─── Runtime ─────────────────────────────────────────────────────────────
    private State      state = State.Closed;
    private int        currentHP;
    private Rigidbody2D rb;
    private Collider2D  col;

    // Açıkken fizik engelini sağlayan ikinci collider
    private Collider2D  blockerCol;

    private Transform  player;
    private Coroutine  stateCoroutine;
    private Coroutine  flashCoroutine;
    private Color      originalColor;
    private bool       isDead;
    private bool       isActivated;

    // Temas hasarı cooldown
    private float lastContactDamageTime = -999f;

    // ═════════════════════════════════════════════════════════════════════════
    // UNITY CALLBACKS
    // ═════════════════════════════════════════════════════════════════════════

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // ── Push için Dynamic RB (TNT mantığı) ──────────────────────────────
        rb.bodyType               = RigidbodyType2D.Dynamic;
        rb.gravityScale           = 0f;
        rb.freezeRotation         = true;
        rb.linearDamping          = pushDrag;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        currentHP = Mathf.Max(1, maxHP);
        SetSprite(closedSprite);

        // Ana collider başlangıçta katı
        col.isTrigger = false;

        // Blocker collider oluştur (Open'da oyuncuyu durdurur + hasar verir)
        CreateBlockerCollider();
    }

    private void Start()
    {
        TryFindPlayer();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Hız sınırı (TNT push mantığı)
        if (rb.linearVelocity.sqrMagnitude > maxPushSpeed * maxPushSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxPushSpeed;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // BLOCKER COLLIDER — Açıkken içinden geçişi engeller + temas hasarı verir
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Ana collider ile aynı şekil/boyutta ikinci bir collider oluşturur.
    /// Bu collider HİÇBİR ZAMAN trigger olmaz → oyuncu içinden geçemez.
    /// Open durumunda aktif + OnCollisionEnter2D ile temas hasarı verir.
    /// </summary>
    private void CreateBlockerCollider()
    {
        // Ana collider tipini kopyala
        if (col is CircleCollider2D mainCircle)
        {
            CircleCollider2D bc = gameObject.AddComponent<CircleCollider2D>();
            bc.radius = mainCircle.radius;
            bc.offset = mainCircle.offset;
            blockerCol = bc;
        }
        else if (col is BoxCollider2D mainBox)
        {
            BoxCollider2D bc = gameObject.AddComponent<BoxCollider2D>();
            bc.size   = mainBox.size;
            bc.offset = mainBox.offset;
            blockerCol = bc;
        }
        else if (col is CapsuleCollider2D mainCapsule)
        {
            CapsuleCollider2D bc = gameObject.AddComponent<CapsuleCollider2D>();
            bc.size      = mainCapsule.size;
            bc.offset    = mainCapsule.offset;
            bc.direction = mainCapsule.direction;
            blockerCol   = bc;
        }

        if (blockerCol != null)
        {
            blockerCol.isTrigger = false; // Asla trigger değil
            blockerCol.enabled   = false; // Başlangıçta kapalı (Closed'da ana collider yeterli)
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // AKTİVASYON
    // ═════════════════════════════════════════════════════════════════════════

    public void SetActive(bool active)
    {
        if (isDead) return;
        if (active) { if (!isActivated) Activate(); }
        else
        {
            if (stateCoroutine != null) { StopCoroutine(stateCoroutine); stateCoroutine = null; }
            isActivated = false;
        }
    }

    public void Activate()
    {
        if (isActivated) return;
        isActivated    = true;
        stateCoroutine = StartCoroutine(StateMachine());
        Log("Activate() çağrıldı.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ANA STATE MAKİNESİ
    // ═════════════════════════════════════════════════════════════════════════

    private IEnumerator StateMachine()
    {
        while (!isDead)
        {
            yield return EnterClosed();
            if (isDead) yield break;

            yield return EnterOpening();
            if (isDead) yield break;

            yield return EnterOpen();
            if (isDead) yield break;

            yield return EnterClosing();
        }
    }

    private IEnumerator EnterClosed()
    {
        state = State.Closed;
        SetSprite(closedSprite);
        SetColliderMode(solid: true);
        Log("State: CLOSED");
        float wait = Random.Range(closedWaitMin, closedWaitMax);
        Log($"Kapalı bekleme süresi: {wait:F1} sn");
        yield return new WaitForSeconds(wait);
    }

    private IEnumerator EnterOpening()
    {
        state = State.Opening;
        SetColliderMode(solid: true);
        Log("State: OPENING");
        PlayClip(openClip);
        yield return null;
    }

    private IEnumerator EnterOpen()
    {
        state = State.Open;
        SetSprite(openSprite);
        // Ana collider trigger olur (mermiler çarpsın)
        // Blocker collider açılır (oyuncu geçemesin + temas hasarı)
        SetColliderMode(solid: false);
        Log("State: OPEN");

        yield return new WaitForSeconds(openPreFireWait);
        if (isDead) yield break;

        yield return new WaitForSeconds(openFireDelay);
        if (isDead) yield break;

        FireAtPlayer();

        yield return new WaitForSeconds(openPostFireWait);
    }

    private IEnumerator EnterClosing()
    {
        state = State.Closing;
        SetColliderMode(solid: true);
        Log("State: CLOSING");
        PlayClip(closeClip);
        yield return null;
    }

    private IEnumerator EnterDazed()
    {
        if (stateCoroutine != null) { StopCoroutine(stateCoroutine); stateCoroutine = null; }

        state = State.Dazed;
        SetSprite(dazedSprite);
        SetColliderMode(solid: true);
        PlayClip(dazedClip);
        Log($"State: DAZED — {dazedDuration} sn beklenecek.");

        yield return new WaitForSeconds(dazedDuration);

        if (!isDead)
        {
            Log("Dazed bitti, Closed'a dönülüyor.");
            stateCoroutine = StartCoroutine(StateMachine());
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COLLIDER MODU
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// solid=true  → Ana collider katı (push alır), Blocker kapalı.
    /// solid=false → Ana collider trigger (mermiler), Blocker açık (engel + hasar).
    /// </summary>
    private void SetColliderMode(bool solid)
    {
        if (col != null)
            col.isTrigger = !solid;

        if (blockerCol != null)
            blockerCol.enabled = !solid; // Sadece Open'da aktif
    }

    // ═════════════════════════════════════════════════════════════════════════
    // COLLISION — Oyuncudan push al (Closed/Dazed) veya temas hasarı ver (Open)
    // ═════════════════════════════════════════════════════════════════════════

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        if (!collision.gameObject.CompareTag(playerTag)) return;

        if (state == State.Open)
        {
            // Open: Blocker collider çarptı → temas hasarı ver + geri it
            GiveContactDamage(collision.gameObject, collision.contacts[0].normal);
        }
        else
        {
            // Closed / Opening / Closing / Dazed:
            // Oyuncu Host'u itebilir; oyuncuyu hafifçe geri it
            BouncePlayer(collision.gameObject, collision.contacts[0].normal);
            Log("Closed: Oyuncu Host'u itti.");
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;
        if (state != State.Open) return;
        if (!collision.gameObject.CompareTag(playerTag)) return;

        // Sürekli temas için cooldown kontrolü
        GiveContactDamage(collision.gameObject, collision.contacts[0].normal);
    }

    /// <summary>
    /// Oyuncuya temas hasarı ver (cooldown'a göre).
    /// </summary>
    private void GiveContactDamage(GameObject playerGO, Vector2 contactNormal)
    {
        if (Time.time - lastContactDamageTime < contactDamageCooldown) return;
        lastContactDamageTime = Time.time;

        // Oyuncuyu geri it
        BouncePlayer(playerGO, contactNormal);

        // Oyuncunun hasar alma metodunu çağır (projenin mevcut player sağlık scriptine göre uyarla)
        var playerHealth = playerGO.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(contactDamage);
            Log($"Open: Oyuncuya {contactDamage} temas hasarı verildi.");
        }
    }

    /// <summary>
    /// Oyuncuyu Host'tan uzaklaştır.
    /// </summary>
    private void BouncePlayer(GameObject playerGO, Vector2 contactNormal)
    {
        var playerRb = playerGO.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        // contactNormal: çarpışma yüzeyinin normali (Host'tan oyuncuya doğru)
        Vector2 bounceDir = contactNormal.normalized;
        if (bounceDir.sqrMagnitude < 0.001f)
            bounceDir = ((Vector2)playerGO.transform.position - (Vector2)transform.position).normalized;

        playerRb.linearVelocity = Vector2.zero;
        playerRb.AddForce(bounceDir * playerBounceForce, ForceMode2D.Impulse);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // TRIGGER — Mermi / Patlama hasarı (Ana collider trigger'dayken)
    // ═════════════════════════════════════════════════════════════════════════

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        // Sadece Explosion layer'ından gelen tetikleyiciler
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;
        if (other.GetComponent<PooterProjectile>() != null) return;
        if (other.CompareTag(playerTag)) return;

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeDamage(1, knockDir);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // HASAR ALMA
    // ═════════════════════════════════════════════════════════════════════════

    public void TakeDamage(int damage, Vector2 knockDir = default)
    {
        if (isDead) return;

        if (state != State.Open)
        {
            if (state != State.Dazed)
            {
                Log("Kapalıyken hasar — Dazed'e geçiliyor.");
                StartCoroutine(EnterDazed());
            }
            return;
        }

        currentHP -= Mathf.Max(1, damage);
        Log($"Hasar alındı: {damage}. Kalan HP: {currentHP}");

        PlayClip(hitClip);

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlash());

        if (knockDir != default && knockDir.sqrMagnitude > 0.001f && rb != null)
            StartCoroutine(KnockbackRoutine(knockDir));

        if (currentHP <= 0) Die();
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ATEŞ
    // ═════════════════════════════════════════════════════════════════════════

    private void FireAtPlayer()
    {
        if (bloodPrefab == null) { LogWarning("bloodPrefab atanmamış!"); return; }
        if (player == null) { TryFindPlayer(); if (player == null) return; }

        PlayClip(fireClip);

        Vector3 origin   = firePoint != null ? firePoint.position : transform.position;
        Vector2 baseDir  = ((Vector2)player.position - (Vector2)origin).normalized;
        if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector2.up;

        float totalSpread = spreadAngleDeg * (projectileCount - 1);
        float startAngle  = -totalSpread / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float      angle = startAngle + spreadAngleDeg * i;
            Vector2    dir   = RotateVector(baseDir, angle);
            GameObject go    = Instantiate(bloodPrefab, origin, Quaternion.identity);
            var        proj  = go.GetComponent<PooterProjectile>();
            if (proj != null) proj.Initialize(dir);
            else
            {
                var prb = go.GetComponent<Rigidbody2D>();
                if (prb != null) prb.linearVelocity = dir * 4.5f;
            }
        }

        Log($"Ateş edildi! {projectileCount} mermi, yön={baseDir}");
    }

    private Vector2 RotateVector(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // ÖLÜM
    // ═════════════════════════════════════════════════════════════════════════

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Log("Düşman öldü.");

        PlayClip(deathClip);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (stateCoroutine != null) StopCoroutine(stateCoroutine);

        col.enabled  = false;
        rb.simulated = false;
        if (blockerCol != null) blockerCol.enabled = false;

        var tracker    = GetComponentInParent<RoomEnemyTracker>();
        var roomTrigger = GetComponentInParent<EnemyRoomTrigger>();

        Destroy(gameObject, 0.05f);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // YARDIMCILAR
    // ═════════════════════════════════════════════════════════════════════════

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(Mathf.Max(0f, hitFlashDuration));
        spriteRenderer.color = originalColor;
    }

    private IEnumerator KnockbackRoutine(Vector2 dir)
    {
        // Dynamic zaten, sadece kuvvet uygula
        rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.12f);
        rb.linearVelocity = Vector2.zero;
    }

    private void SetSprite(Sprite s)
    {
        if (spriteRenderer != null && s != null)
            spriteRenderer.sprite = s;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    private void TryFindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[HostAI:{name}] {msg}");
    }

    private void LogWarning(string msg)
    {
        if (enableDebugLogs) Debug.LogWarning($"[HostAI:{name}] {msg}");
    }
}