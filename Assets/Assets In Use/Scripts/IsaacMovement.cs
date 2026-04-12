using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class IsaacMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direction = Vector2.zero;
    public float speed = 5f;

    [Header("Input")]
    public Key moveUp = Key.W;
    public Key moveDown = Key.S;
    public Key moveLeft = Key.A;
    public Key moveRight = Key.D;

    [System.Serializable]
    public struct DirectionSprites
    {
        public AnimatedSpriteRenderer body;
        public AnimatedSpriteRenderer head;
    }

    [Header("Sprites")]
    public DirectionSprites spritesUp;
    public DirectionSprites spritesDown;
    public DirectionSprites spritesLeft;
    public DirectionSprites spritesRight;

    [Header("Death Sprites")]
    public AnimatedSpriteRenderer spriteDeathBody;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip[] deathClips;
    public int selectedDeathClipIndex = 0;

    private DirectionSprites activeSprites;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        activeSprites = spritesDown;
    }

    private void OnEnable()
    {
        direction = Vector2.zero;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector2 inputVector = Vector2.zero;

        if (kb[moveUp].isPressed)    inputVector.y += 1;
        if (kb[moveDown].isPressed)  inputVector.y -= 1;
        if (kb[moveLeft].isPressed)  inputVector.x -= 1;
        if (kb[moveRight].isPressed) inputVector.x += 1;

        if (inputVector != Vector2.zero)
        {
            direction = inputVector.normalized;

            DirectionSprites targetSprites = activeSprites;

            if      (inputVector.x > 0) targetSprites = spritesRight;
            else if (inputVector.x < 0) targetSprites = spritesLeft;
            else if (inputVector.y > 0) targetSprites = spritesUp;
            else if (inputVector.y < 0) targetSprites = spritesDown;

            SetDirection(direction, targetSprites);
        }
        else
        {
            SetDirection(Vector2.zero, activeSprites);
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + speed * Time.fixedDeltaTime * direction);
    }

    private void SetDirection(Vector2 newDirection, DirectionSprites sprites)
    {
        direction = newDirection;

        SetDirectionVisible(spritesUp,    sprites.body == spritesUp.body);
        SetDirectionVisible(spritesDown,  sprites.body == spritesDown.body);
        SetDirectionVisible(spritesLeft,  sprites.body == spritesLeft.body);
        SetDirectionVisible(spritesRight, sprites.body == spritesRight.body);

        activeSprites = sprites;

        bool isIdle = direction == Vector2.zero;
        activeSprites.body.idle = isIdle;
        activeSprites.head.idle = isIdle;
    }

    private void SetDirectionVisible(DirectionSprites sprites, bool visible)
    {
        sprites.body.enabled = visible;
        sprites.head.enabled = visible;
    }

    // ─── Hasar Alma (Can sistemine yönlendirir) ───────────────────────────────
    /// <summary>
    /// Tüm dış sistemler (düşmanlar, bombalar, tuzaklar vb.) bu metodu çağırır.
    /// Hasar PlayerHealth üzerinden işlenir; can sıfırlanınca DeathSequence çağrılır.
    /// </summary>
    public void ApplyDamage(int halfHearts = 1)
    {
        PlayerHealth health = GetComponent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(halfHearts);
    }
 
    // ─── Ölüm Dizisi (Yalnızca PlayerHealth tarafından çağrılır) ─────────────
    private bool isDead = false;
 
    public void DeathSequence()
    {
        if (isDead) return;
        isDead = true;
 
        enabled = false;
 
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
 
        var bombCtrl = GetComponent<BombController>();
        if (bombCtrl != null) bombCtrl.enabled = false;
 
        SetDirectionVisible(spritesUp,    false);
        SetDirectionVisible(spritesDown,  false);
        SetDirectionVisible(spritesLeft,  false);
        SetDirectionVisible(spritesRight, false);
 
        if (spriteDeathBody != null) spriteDeathBody.enabled = true;
 
        PlayDeathSound();
    }
 
    private void PlayDeathSound()
    {
        if (audioSource == null || deathClips == null || deathClips.Length == 0) return;
        if (selectedDeathClipIndex < 0 || selectedDeathClipIndex >= deathClips.Length) return;
        audioSource.PlayOneShot(deathClips[selectedDeathClipIndex]);
    }
 
    // Explosion katmanına (kendi bombaları) çarpınca hasar al
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
            ApplyDamage(1);
    }
}