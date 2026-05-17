using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HollowHead : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float moveSpeed      = 4f;
    [SerializeField] private float aloneSpeedMult = 0.8f;
    [SerializeField] private float followStrength = 3f;

    [Header("Segmentler")]
    [SerializeField] private List<HollowSegment> segments = new();
    [SerializeField] private int historyStepPerSeg = 10;

    [Header("Yön Sprite'ları (Baş)")]
    [SerializeField] private Sprite spriteUp;
    [SerializeField] private Sprite spriteDown;
    [SerializeField] private Sprite spriteRight;

    [Header("Can & Hasar")]
    [SerializeField] private float damageCooldown     = 0.5f;
    [SerializeField] private float tearDamageCooldown = 0.3f;
    [SerializeField] private float hitFlashDuration   = 0.12f;
    [SerializeField] private Color hitFlashColor      = Color.red;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Isaac Temas Hasarı")]
    [SerializeField] private string playerTag     = "Player";
    [SerializeField] private int    contactDamage = 1;

    [Header("Trap Spawn")]
    [SerializeField] private GameObject trapPrefab;
    [SerializeField] private float      trapInterval = 5f;
    [SerializeField] [Range(0f, 1f)] private float trapChance = 0.3f;

    [Header("Ölünce Aktif Olacaklar")]
    [SerializeField] private SpriteRenderer tunnelSpriteRenderer;
    [SerializeField] private Collider2D     tunnelCollider2D;

    [Header("Ses Efektleri")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   trapPlaceClip;
    [SerializeField] private AudioClip   hitClip;
    [SerializeField] private AudioClip   deathClip;
    [SerializeField] private AudioClip   segmentDieClip;
    [SerializeField] private AudioClip   activateClip;

    // ── Private ──
    private Rigidbody2D rb;
    private Vector2     moveDir;

    private float lastDamageTime  = -999f;
    private float lastTearDamTime = -999f;
    private bool  isDead          = false;
    private bool  isActivated     = false;
    private bool  isAlone         = false;

    private Color     originalColor;
    private Coroutine flashRoutine;

    private float     trapTimer;
    private Transform isaacTransform;

    private List<Vector2> posHistory = new();
    private int           historyCapacity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        float[] diags = { 45f, 135f, 225f, 315f };
        float   angle = diags[Random.Range(0, diags.Length)] * Mathf.Deg2Rad;
        moveDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;

        historyCapacity = (segments.Count + 4) * historyStepPerSeg;

        trapTimer = trapInterval;

        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null) isaacTransform = player.transform;

        UpdateSprite();
        StartCoroutine(InitSegments());
    }

    private IEnumerator InitSegments()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Vector2 startPos = rb.position;

        posHistory.Clear();
        for (int i = 0; i < historyCapacity; i++)
            posHistory.Add(startPos);

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i] == null) continue;
            segments[i].InitChain(
                i == 0 ? null : segments[i - 1],
                this,
                historyStepPerSeg,
                historyCapacity,
                moveSpeed,
                startPos
            );
        }
    }

    private void Update()
    {
        if (!isActivated || isDead) return;

        trapTimer -= Time.deltaTime;
        if (trapTimer <= 0f)
        {
            trapTimer = trapInterval;
            if (trapPrefab != null && Random.value < trapChance)
            {
                Instantiate(trapPrefab, transform.position, Quaternion.identity);
                PlayClip(trapPlaceClip);
            }
        }

        if (isAlone && isaacTransform != null)
        {
            Vector2 toIsaac = ((Vector2)isaacTransform.position - rb.position).normalized;
            moveDir = Vector2.Lerp(moveDir, toIsaac, followStrength * Time.deltaTime).normalized;
        }

        UpdateSprite();
    }

    private void FixedUpdate()
    {
        if (!isActivated || isDead) return;

        posHistory.Add(rb.position);
        if (posHistory.Count > historyCapacity)
            posHistory.RemoveAt(0);

        float speed = isAlone ? moveSpeed * aloneSpeedMult : moveSpeed;
        rb.linearVelocity = moveDir * speed;
    }

    public Vector2 GetHeadHistoryPosition(int stepsBack)
    {
        if (posHistory.Count == 0) return rb.position;
        int idx = posHistory.Count - 1 - stepsBack;
        if (idx < 0) idx = 0;
        return posHistory[idx];
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (isDead) return;

        if (col.gameObject.CompareTag("Wall") || col.gameObject.CompareTag("Door"))
        {
            Vector2 normal = col.contacts[0].normal;
            moveDir = Vector2.Reflect(moveDir, normal).normalized;
            return;
        }

        if (col.gameObject.CompareTag(playerTag))
        {
            TryDamagePlayer(col.gameObject);
            Vector2 away = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
            moveDir = away;
        }
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (isDead) return;
        if (col.gameObject.CompareTag(playerTag))
            TryDamagePlayer(col.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;
        TakeHit();
    }

    public void TakeTearDamage(int amount)
    {
        if (isDead) return;
        if (Time.time - lastTearDamTime < tearDamageCooldown) return;
        lastTearDamTime = Time.time;
        TakeHit();
    }

    // Hasar alınca en arkadaki segmenti sil, segment yoksa öl
    private void TakeHit()
    {
        PlayClip(hitClip);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlash());

        if (segments.Count > 0)
        {
            // En arkadaki segmenti bul ve sil
            HollowSegment last = segments[segments.Count - 1];
            segments.RemoveAt(segments.Count - 1);
            PlayClip(segmentDieClip);
            if (last != null) last.ForceKill();

            // Segment bitti mi? → isAlone modu
            if (segments.Count == 0)
            {
                isAlone = true;
                Debug.Log("[HollowHead] Tüm segmentler öldü → takip modu.");
            }
        }
        else
        {
            // Segment kalmadı, kafa da hasar aldı → öl
            Die();
        }
    }

    private IEnumerator HitFlash()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isDead) spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        foreach (var seg in segments)
            if (seg != null) seg.ForceKill();

        PlayClip(deathClip);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = true;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = true;

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;

        Destroy(gameObject);
    }

    // Segment kendi kendine ölürse (bomba vs) bu çağrılır — burada TakeHit mantığı yok,
    // sadece listeden çıkar
    public void OnSegmentDied(HollowSegment seg)
    {
        segments.Remove(seg);
        if (segments.Count == 0)
        {
            isAlone = true;
            Debug.Log("[HollowHead] Tüm segmentler öldü → takip modu.");
        }
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        bool facingLeft  = moveDir.x < 0  && Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y);
        bool facingRight = moveDir.x >= 0 && Mathf.Abs(moveDir.x) >= Mathf.Abs(moveDir.y);

        if (facingRight)
        {
            spriteRenderer.sprite = spriteRight;
            spriteRenderer.flipX  = false;
        }
        else if (facingLeft)
        {
            spriteRenderer.sprite = spriteRight;
            spriteRenderer.flipX  = true;
        }
        else if (moveDir.y >= 0)
        {
            spriteRenderer.sprite = spriteUp;
            spriteRenderer.flipX  = false;
        }
        else
        {
            spriteRenderer.sprite = spriteDown;
            spriteRenderer.flipX  = false;
        }
    }

    private void TryDamagePlayer(GameObject player)
    {
        var isaac = player.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(contactDamage);
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource == null || clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    public void Activate()
    {
        PlayClip(activateClip);
        isActivated = true;
    }

    public void Activate(Vector2 pos)
    {
        rb.position        = pos;
        transform.position = pos;
        PlayClip(activateClip);
        isActivated        = true;
    }

    public void SetActive(bool active)
    {
        isActivated = active;
        if (!active) rb.linearVelocity = Vector2.zero;
    }

    public void RegisterSegment(HollowSegment seg) => segments.Add(seg);
}