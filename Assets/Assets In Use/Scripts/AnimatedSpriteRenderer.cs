using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AnimatedSpriteRenderer : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    public Sprite idleSprite;
    public Sprite[] idleAnimationSprites;
    public Sprite[] animationSprites;

    public float animationTime = 0.25f;
    public float idleAnimationTime = 0.5f;

    private int walkFrame;
    private int idleFrame;
    private float animationTimer;
    private bool wasIdle = true;

    public bool loop = true;
    public bool idle = true;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        spriteRenderer.enabled = true;
    }

    private void OnDisable()
    {
        spriteRenderer.enabled = false;
    }

    private void Update()
    {
        if (idle != wasIdle)
        {
            animationTimer = 0f;
            if (idle) idleFrame = 0;
            else walkFrame = 0;
            wasIdle = idle;
        }

        bool hasIdleAnim = idleAnimationSprites != null && idleAnimationSprites.Length > 0;
        float frameTime = idle ? (hasIdleAnim ? idleAnimationTime : animationTime) : animationTime;

        animationTimer += Time.deltaTime;
        if (animationTimer < frameTime) return;
        animationTimer -= frameTime;

        NextFrame();
    }

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
}
