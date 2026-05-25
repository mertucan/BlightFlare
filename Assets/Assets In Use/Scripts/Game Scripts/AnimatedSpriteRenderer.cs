using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSpriteRenderer : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    // ── Orijinal alanlar ──────────────────────────────────────
    public Sprite   idleSprite;
    public Sprite[] idleAnimationSprites;
    public Sprite[] animationSprites;

    public float animationTime     = 0.25f;
    public float idleAnimationTime = 0.5f;

    private int   walkFrame;
    private int   idleFrame;
    private float animationTimer;
    private bool  wasIdle = true;

    public bool loop = true;
    public bool idle = true;

    // ── Göz / Kırpma sistemi ──────────────────────────────────
    [Header("Eye Blink System")]
    public bool isHeadRenderer = false;
    public int   blinkClosedIndex  = 1;
    public float blinkIntervalMin  = 2f;
    public float blinkIntervalMax  = 5f;
    public float blinkDuration     = 0.12f;

    // ── Dahili durum ──────────────────────────────────────────
    private bool  isBlinking;
    private float blinkTimer;
    private float blinkOpenTimer;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ScheduleNextBlink();
    }

    private void OnEnable()  => spriteRenderer.enabled = true;
    private void OnDisable() => spriteRenderer.enabled = false;

    private void Update()
    {
        if (isHeadRenderer)
        {
            UpdateHeadBlink();
            return;
        }

        // ── idle → walk veya walk → idle geçişi ──────────────
        if (idle != wasIdle)
        {
            animationTimer = 0f;

            if (idle)
            {
                // Yürüyüş bitti → walk sprite'ını 0. frame'e sıfırla
                walkFrame = 0;
                if (animationSprites != null && animationSprites.Length > 0)
                    spriteRenderer.sprite = animationSprites[0];

                idleFrame = 0;
            }
            else
            {
                walkFrame = 0;
            }

            wasIdle = idle;
        }

        bool  hasIdleAnim = idleAnimationSprites != null && idleAnimationSprites.Length > 0;
        float frameTime   = idle
            ? (hasIdleAnim ? idleAnimationTime : animationTime)
            : animationTime;

        animationTimer += Time.deltaTime;
        if (animationTimer >= frameTime)
        {
            animationTimer -= frameTime;
            NextFrame();
        }
    }

    // ── Kafa: göz kırpma ─────────────────────────────────────
    private void UpdateHeadBlink()
    {
        if (idleAnimationSprites == null || idleAnimationSprites.Length == 0)
        {
            if (idleSprite != null) spriteRenderer.sprite = idleSprite;
            return;
        }

        int openIdx   = blinkClosedIndex == 0 ? 1 : 0;
        openIdx       = Mathf.Clamp(openIdx,          0, idleAnimationSprites.Length - 1);
        int closedIdx = Mathf.Clamp(blinkClosedIndex, 0, idleAnimationSprites.Length - 1);

        if (isBlinking)
        {
            spriteRenderer.sprite  = idleAnimationSprites[closedIdx];
            blinkOpenTimer        -= Time.deltaTime;
            if (blinkOpenTimer <= 0f) { isBlinking = false; ScheduleNextBlink(); }
        }
        else
        {
            spriteRenderer.sprite  = idleAnimationSprites[openIdx];
            blinkTimer            -= Time.deltaTime;
            if (blinkTimer <= 0f)  { isBlinking = true; blinkOpenTimer = blinkDuration; }
        }
    }

    private void ScheduleNextBlink() =>
        blinkTimer = Random.Range(blinkIntervalMin, blinkIntervalMax);

    // ── NextFrame ─────────────────────────────────────────────
    private void NextFrame()
    {
        if (idle)
        {
            if (idleAnimationSprites != null && idleAnimationSprites.Length > 0)
            {
                idleFrame = (idleFrame + 1) % idleAnimationSprites.Length;
                spriteRenderer.sprite = idleAnimationSprites[idleFrame];
            }
            else if (idleSprite != null)
            {
                spriteRenderer.sprite = idleSprite;
            }
            return;
        }

        walkFrame++;
        if (loop && walkFrame >= animationSprites.Length)
            walkFrame = 0;

        if (walkFrame >= 0 && walkFrame < animationSprites.Length)
            spriteRenderer.sprite = animationSprites[walkFrame];
    }

    // ── ResetAnimation ────────────────────────────────────────
    public void ResetAnimation()
    {
        walkFrame      = 0;
        idleFrame      = 0;
        animationTimer = 0f;
        wasIdle        = !idle;

        isBlinking     = false;
        ScheduleNextBlink();

        if (idle)
        {
            if (idleAnimationSprites != null && idleAnimationSprites.Length > 0)
                spriteRenderer.sprite = idleAnimationSprites[0];
            else if (idleSprite != null)
                spriteRenderer.sprite = idleSprite;
        }
        else
        {
            if (animationSprites != null && animationSprites.Length > 0)
                spriteRenderer.sprite = animationSprites[0];
        }
    }
}