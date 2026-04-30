using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class SuckerAI : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 4;
    public float moveSpeed = 1.5f;
    public float detectionRange = 6f;
    public float stopDistance = 0.8f;

    [Header("Sprite Animation")]
    public Sprite[] walkFrames;
    public float fps = 8f;

    [Header("Blood FX")]
    public GameObject bloodPrefab;
    public Transform firePoint;
    public float bloodSpeed = 3f;
    public float bloodLifetime = 0.4f;

    [Header("Feedback")]
    public float knockbackForce = 3f;
    public float hitFlashDuration = 0.12f;
    public Color hitFlashColor = Color.red;
    public SpriteRenderer spriteRenderer;

    [Header("Steering")]
    [Tooltip("Duvarlardan kaçınma yarıçapı")]
    public float wallAvoidRadius = 1.2f;
    [Tooltip("Duvar kaçınma kuvveti")]
    public float wallAvoidWeight = 2.5f;
    public string wallTag = "Wall";
    public float smoothTime = 0.25f;

    [Header("Panic (değdikten sonra geri kaç)")]
    [Tooltip("Kaç saniye geri kaçsın")]
    public float panicDuration = 1.5f;

    // ── Runtime ──────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Collider2D col;
    private Transform player;

    private int currentHP;
    private bool isDead = false;
    private bool isActivated = false;

    private Color originalColor = Color.white;
    private Coroutine flashRoutine;
    private Vector2 smoothVelocity;
    private float panicTimer = 0f;

    // ─────────────────────────────────────────────────────────────
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
        rb.linearVelocity = Vector2.zero;

        currentHP = Mathf.Max(1, maxHP);
    }

    private void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (walkFrames != null && walkFrames.Length > 0)
            StartCoroutine(AnimationLoop());
    }

    // ─── Activation ──────────────────────────────────────────────
    public void SetActive(bool active)
    {
        isActivated = active;
        if (!active) rb.linearVelocity = Vector2.zero;
    }

    public void Activate() { }

    // ─── Sprite Animation Loop ───────────────────────────────────
    private IEnumerator AnimationLoop()
    {
        int frame = 0;
        float delay = 1f / Mathf.Max(1f, fps);

        while (true)
        {
            if (spriteRenderer != null && walkFrames.Length > 0)
                spriteRenderer.sprite = walkFrames[frame % walkFrames.Length];

            frame++;
            yield return new WaitForSeconds(delay);
        }
    }

    // ─── Movement ────────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (!isActivated || isDead || player == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (panicTimer > 0f) panicTimer -= Time.fixedDeltaTime;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;

        Vector2 desired = ComputeDesiredVelocity(toPlayer, dist);
        Vector2 vel = Vector2.SmoothDamp(
            rb.linearVelocity, desired, ref smoothVelocity,
            Mathf.Max(0.01f, smoothTime));

        rb.linearVelocity = vel;

        if (Mathf.Abs(vel.x) > 0.05f && spriteRenderer != null)
            spriteRenderer.flipX = vel.x < 0f;
    }

    private Vector2 ComputeDesiredVelocity(Vector2 toPlayer, float dist)
    {
        if (dist < 0.001f) return Vector2.zero;

        Vector2 dirToPlayer = toPlayer / dist;

        // Panic timer aktifse geri kaç
        if (panicTimer > 0f)
            return -dirToPlayer * moveSpeed * 1.5f;

        // Dur mesafesindeyse dur
        if (dist <= stopDistance)
            return Vector2.zero;

        // Normal takip + duvar kaçınma
        Vector2 moveDir    = dirToPlayer;
        Vector2 avoidForce = Vector2.zero;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(rb.position, wallAvoidRadius);
        foreach (var w in nearby)
        {
            if (w.isTrigger) continue;
            if (!w.CompareTag(wallTag)) continue;

            Vector2 closest = w.ClosestPoint(rb.position);
            Vector2 diff    = rb.position - closest;
            float   d       = diff.magnitude;
            if (d > 0.001f && d < wallAvoidRadius)
                avoidForce += (diff / d) * ((wallAvoidRadius - d) / wallAvoidRadius);
        }

        Vector2 final = moveDir + avoidForce * wallAvoidWeight;
        if (final.sqrMagnitude > 1f) final.Normalize();

        return final * moveSpeed;
    }

    // ─── Contact damage ──────────────────────────────────────────
    private void OnCollisionEnter2D(Collision2D collision)
    {
        var isaac = collision.gameObject.GetComponent<IsaacMovement>();
        if (isaac != null)
        {
            isaac.ApplyDamage(1);
            panicTimer = panicDuration; // değdikten sonra geri kaç
        }
    }

    private void OnCollisionStay2D(Collision2D collision) { }

    // ─── Hasar alma ──────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;
        if (other.GetComponent<SuckerAI>() != null) return;
        if (other.GetComponent<PooterProjectile>() != null) return; // Düşman kanı Sucker'a hasar vermesin

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeDamage(1, knockDir);
    }

    public void TakeDamage(int damage, Vector2 knockDir = default)
    {
        if (isDead) return;

        currentHP -= Mathf.Max(1, damage);

        if (rb != null && knockDir != Vector2.zero)
            rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        if (currentHP <= 0) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(Mathf.Max(0f, hitFlashDuration));
        spriteRenderer.color = originalColor;
    }

    // ─── Death ───────────────────────────────────────────────────
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;
        if (col != null) col.enabled = false;

        SpawnBlood();

        Destroy(gameObject, 0.15f);
    }

    // ─── Blood Spawn — artı (+) pattern ─────────────────────────
    private void SpawnBlood()
    {
        if (bloodPrefab == null) return;

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;

        Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

        foreach (var dir in dirs)
        {
            GameObject blood = Instantiate(bloodPrefab, origin, Quaternion.identity);

            PooterProjectile proj = blood.GetComponent<PooterProjectile>();
            if (proj != null)
            {
                proj.speed    = bloodSpeed;
                proj.lifetime = 6f;
                // damage Inspector'daki değerde kalır — oyuncuya hasar verir
                proj.Initialize(dir);
            }
            else
            {
                Rigidbody2D bloodRb = blood.GetComponent<Rigidbody2D>();
                if (bloodRb != null)
                {
                    bloodRb.gravityScale   = 0f;
                    bloodRb.linearVelocity = dir * bloodSpeed;
                }
                Destroy(blood, bloodLifetime);
            }
        }
    }
}