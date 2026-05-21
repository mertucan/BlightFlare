using System.Collections;
using UnityEngine;

/// <summary>
/// Mask of Infamy — Maske Kısmı
///
/// Davranış:
///   - Oyuncu maskenin tam yukarı / aşağı / sol / sağ hizasına girince
///     o yönde hızlıca şarj eder ve şarj sesi çalar.
///   - Hizada değilse wander yapar (BabyAI/LokiAI mantığı).
///   - Duvara çarpınca durur, kısa bekleme sonrası wander'a döner.
///   - Bomba patlamasından HİÇ hasar almaz.
///   - Oyuncuya temas halinde hasar verir.
///   - Wall ve Door içinden geçmez (Rigidbody2D + Collider2D fizik katmanı ile).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MaskAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // HAREKET
    // ─────────────────────────────────────────────
    [Header("Hareket")]
    public float wanderSpeed  = 1.2f;
    public float chargeSpeed  = 5f;
    public float smoothTime   = 0.45f;

    [Header("Wander")]
    public float wanderArrivalRadius     = 0.6f;
    public float wanderChangeIntervalMin = 1.5f;
    public float wanderChangeIntervalMax = 3.2f;
    public float wanderRadius            = 2.5f;

    // ─────────────────────────────────────────────
    // HİZALAMA (Charge Tetikleme)
    // ─────────────────────────────────────────────
    [Header("Hizalama & Charge")]
    [Tooltip("Bu piksel toleransı içinde 'hizalı' sayılır.")]
    public float alignTolerance   = 0.4f;
    [Tooltip("Charge sırasında ne kadar ilerlenmeden önce duvar tespiti yapılır — çok küçük bırak.")]
    public float chargeStopDistance = 0.3f;
    [Tooltip("Charge bittikten sonra wander'a dönmeden önceki bekleme (s).")]
    public float chargeRecoverTime  = 0.5f;

    // ─────────────────────────────────────────────
    // DUVAR KAÇINMA (wander sırasında)
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
    public string playerTag        = "Player";
    public float  contactCooldown  = 0.8f;

    // ─────────────────────────────────────────────
    // SES
    // ─────────────────────────────────────────────
    [Header("Ses")]
    public AudioSource audioSource;
    [Tooltip("Charge başlarken çalınır.")]
    public AudioClip   chargeClip;

    // ─────────────────────────────────────────────
    // GÖRSEL
    // ─────────────────────────────────────────────
    [Header("Görsel")]
    public SpriteRenderer spriteRenderer;
    public Sprite spriteRight;
    public Sprite spriteLeft;
    public Sprite spriteUp;
    public Sprite spriteDown;

    // ─────────────────────────────────────────────
    // RUNTIME DEBUG
    // ─────────────────────────────────────────────
    [Header("Runtime (Debug — Read Only)")]
    [SerializeField] private bool isCharging;
    [SerializeField] private bool isRecovering;
    [SerializeField] private Rect roomSafeRect;

    // ─── privates ────────────────────────────────
    private Rigidbody2D rb;
    private Collider2D  col;
    private Transform   player;

    private Vector2 wanderTarget;
    private float   wanderChangeTimer;
    private Vector2 smoothDampVelocity;

    private Vector2 chargeDirection;
    private float   lastContactTime = -999f;
    private bool    isActivated     = false;

    // ═══════════════════════════════════════════
    #region Unity Callbacks

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale   = 0f;
        rb.freezeRotation = true;

        wanderTarget      = (Vector2)transform.position;
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);

        TryFindPlayer();
    }

    private void Start()
    {
        GatherRoomBounds();
    }

    private void Update()
    {
        if (!isActivated || isCharging || isRecovering) return;

        if (player == null) TryFindPlayer();
        if (player == null) return;

        // Wander hedef zamanlayıcısı
        wanderChangeTimer -= Time.deltaTime;
        if (wanderChangeTimer <= 0f ||
            Vector2.Distance(rb.position, wanderTarget) < wanderArrivalRadius)
            PickNewWanderTarget();

        // Hizalama kontrolü — her kare kontrol et
        if (CheckAlignment(out Vector2 alignDir))
            StartCoroutine(ChargeRoutine(alignDir));
    }

    private void FixedUpdate()
    {
        if (!isActivated || isCharging || isRecovering) return;
        if (rb == null) return;

        Vector2 toWander  = wanderTarget - rb.position;
        Vector2 wanderDir = toWander.sqrMagnitude > 0.001f ? toWander.normalized : Vector2.right;

        Vector2 avoidForce = ComputeWallAvoidance();
        Vector2 finalDir   = wanderDir + avoidForce * wallAvoidWeight;
        if (finalDir.sqrMagnitude > 1f) finalDir.Normalize();

        Vector2 desired = finalDir * wanderSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(
            rb.linearVelocity, desired,
            ref smoothDampVelocity, Mathf.Max(0.01f, smoothTime));
        UpdateDirectionalSprite(finalDir);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
            TryDamagePlayer(collision.gameObject);

        // Charge sırasında duvara ya da kapıya çarpınca dur
        if (isCharging &&
            (collision.gameObject.CompareTag(wallTag) ||
             collision.gameObject.CompareTag(doorTag)))
        {
            StopAllCoroutines();
            StartCoroutine(RecoverRoutine());
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(playerTag))
            TryDamagePlayer(collision.gameObject);
    }

    // Bomba patlaması → HİÇ tepki verme
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kasıtlı olarak boş — maske hasar almaz
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Hizalama & Charge

    /// <summary>
    /// Oyuncu maskenin tam yukarı/aşağı/sol/sağ hizasındaysa true döner.
    /// alignDir = charge yönü
    /// </summary>
    private bool CheckAlignment(out Vector2 alignDir)
    {
        alignDir = Vector2.zero;
        if (player == null) return false;

        Vector2 diff = (Vector2)player.position - rb.position;
        float   ax   = Mathf.Abs(diff.x);
        float   ay   = Mathf.Abs(diff.y);

        // Yatay hizalama: |dy| < tolerance → sol veya sağ
        if (ay < alignTolerance)
        {
            alignDir = diff.x > 0 ? Vector2.right : Vector2.left;
            return true;
        }

        // Dikey hizalama: |dx| < tolerance → yukarı veya aşağı
        if (ax < alignTolerance)
        {
            alignDir = diff.y > 0 ? Vector2.up : Vector2.down;
            return true;
        }

        return false;
    }

    private IEnumerator ChargeRoutine(Vector2 dir)
    {
        isCharging      = true;
        chargeDirection = dir;

        PlayClip(chargeClip);

        // Duvara çarpana kadar ya da oyuncuya çarpana kadar ilerle
        // Rigidbody + Collider fizik sistemi duvarı zaten bloklar,
        // OnCollisionEnter2D durduracak; ama güvenlik için timeout ekle
        float timeout = 3f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            rb.linearVelocity = dir * chargeSpeed;
            UpdateDirectionalSprite(dir);
            elapsed          += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();

            // Önde duvar / kapı var mı? (ray ile erken dur)
            RaycastHit2D hit = Physics2D.Raycast(
                rb.position, dir, chargeStopDistance,
                LayerMask.GetMask("Default", "Wall"));          // layer adını projenize göre ayarlayın

            if (hit.collider != null &&
                (hit.collider.CompareTag(wallTag) || hit.collider.CompareTag(doorTag)))
                break;
        }

        StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        isCharging    = false;
        isRecovering  = true;
        rb.linearVelocity = Vector2.zero;
        smoothDampVelocity = Vector2.zero;

        yield return new WaitForSeconds(chargeRecoverTime);

        isRecovering = false;
        PickNewWanderTarget();
    }

    #endregion

    // ═══════════════════════════════════════════
    #region Wander & Duvar Kaçınma

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

    private Vector2 lastFacingDir = Vector2.right;

    private void UpdateDirectionalSprite(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.01f) return;

        Vector2 dominant;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            dominant = dir.x > 0 ? Vector2.right : Vector2.left;
        else
            dominant = dir.y > 0 ? Vector2.up : Vector2.down;

        if (dominant == lastFacingDir) return;
        lastFacingDir = dominant;

        if (spriteRenderer == null) return;

        if      (dominant == Vector2.right) { if (spriteRight) spriteRenderer.sprite = spriteRight; }
        else if (dominant == Vector2.left)  { if (spriteLeft)  spriteRenderer.sprite = spriteLeft;  }
        else if (dominant == Vector2.up)    { if (spriteUp)    spriteRenderer.sprite = spriteUp;    }
        else if (dominant == Vector2.down)  { if (spriteDown)  spriteRenderer.sprite = spriteDown;  }
    }

    private void TryDamagePlayer(GameObject playerObj)
    {
        if (Time.time - lastContactTime < contactCooldown) return;
        lastContactTime = Time.time;
        IsaacMovement isaac = playerObj.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(1);
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

        // Hizalama bandları
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireCube(transform.position, new Vector3(alignTolerance * 2f, 20f, 0f)); // dikey bant
        Gizmos.DrawWireCube(transform.position, new Vector3(20f, alignTolerance * 2f, 0f)); // yatay bant

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