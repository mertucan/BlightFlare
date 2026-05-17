using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HollowSegment : MonoBehaviour
{
    [Header("Can & Hasar")]
    [SerializeField] private float damageCooldown     = 0.5f;
    [SerializeField] private float tearDamageCooldown = 0.3f;
    [SerializeField] private float hitFlashDuration   = 0.12f;
    [SerializeField] private Color hitFlashColor      = Color.red;

    [Header("Isaac Temas Hasarı")]
    [SerializeField] private string playerTag     = "Player";
    [SerializeField] private int    contactDamage = 1;

    [Header("Ölüm")]
    [SerializeField] private GameObject     deathEffectPrefab;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Ses Efektleri")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip   hitClip;
    [SerializeField] private AudioClip   deathClip;

    // ── Private ──
    private HollowHead    head;
    private HollowSegment prevSegment;
    private Rigidbody2D   rb;

    private float lastDamageTime  = -999f;
    private float lastTearDamTime = -999f;
    private bool  isDead          = false;
    private bool  initialized     = false;

    private Color     originalColor;
    private Coroutine flashRoutine;

    private List<Vector2> posHistory     = new();
    private int           historyCapacity = 40;
    private int           stepPerSeg      = 10;
    private float         moveSpeed       = 4f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        head = GetComponentInParent<HollowHead>();
        if (head == null) head = FindFirstObjectByType<HollowHead>();
    }

    public void InitChain(HollowSegment prev, HollowHead headRef, int step, int capacity, float speed, Vector2 startPos)
    {
        prevSegment     = prev;
        head            = headRef;
        stepPerSeg      = step;
        historyCapacity = capacity;
        moveSpeed       = speed;

        posHistory.Clear();
        for (int i = 0; i < historyCapacity; i++)
            posHistory.Add(startPos);

        rb.position        = startPos;
        transform.position = startPos;
        rb.linearVelocity  = Vector2.zero;
        rb.simulated       = true;

        initialized = true;
    }

    private void FixedUpdate()
    {
        if (isDead || !initialized) return;

        // Gerçek pozisyonu history'e kaydet
        posHistory.Add(rb.position);
        if (posHistory.Count > historyCapacity)
            posHistory.RemoveAt(0);

        // Hedef pozisyonu al
        Vector2 target;
        if (prevSegment == null || prevSegment.IsDead)
            target = head != null ? head.GetHeadHistoryPosition(stepPerSeg) : rb.position;
        else
            target = prevSegment.GetHistoryPosition(stepPerSeg);

        // Fizik yerine direkt pozisyon ata — sallanma yok
        rb.MovePosition(target);
        rb.linearVelocity = Vector2.zero;
    }

    public Vector2 GetHistoryPosition(int stepsBack)
    {
        if (posHistory.Count == 0) return rb.position;
        int idx = posHistory.Count - 1 - stepsBack;
        if (idx < 0) idx = 0;
        return posHistory[idx];
    }

    public bool IsDead => isDead;

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (isDead) return;
        if (col.gameObject.CompareTag(playerTag))
            TryDamagePlayer(col.gameObject);
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
        // Segment bomba yerse kafaya ilet — en son segment silinsin
        if (head != null) head.TakeTearDamage(1);
    }

    public void TakeTearDamage(int amount)
    {
        if (isDead) return;
        if (Time.time - lastTearDamTime < tearDamageCooldown) return;
        lastTearDamTime = Time.time;
        // Kafaya ilet — en son segment silinsin
        if (head != null) head.TakeTearDamage(amount);
    }

    private void TakeDamage()
    {
        PlayClip(hitClip);
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlash());
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

        PlayClip(deathClip);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated      = false;

        if (head != null) head.OnSegmentDied(this);

        if (spriteRenderer != null) spriteRenderer.enabled = false;
        Destroy(gameObject, 0.05f);
    }

    public void ForceKill()
    {
        if (isDead) return;
        isDead = true;
        PlayClip(deathClip);
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void SetTargetPosition(Vector2 pos) { } // eski API uyumu

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
}