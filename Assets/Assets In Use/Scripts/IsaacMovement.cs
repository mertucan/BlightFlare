using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class IsaacMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direction = Vector2.zero;
    public float speed = 5f;

    [Header("Input")]
    public KeyCode inputUp = KeyCode.W;
    public KeyCode inputDown = KeyCode.S;
    public KeyCode inputLeft = KeyCode.A;
    public KeyCode inputRight = KeyCode.D;

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

    private DirectionSprites activeSprites;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        activeSprites = spritesDown;
    }

    private void Update()
    {
        Vector2 inputVector = Vector2.zero;

        if (Input.GetKey(inputUp))    inputVector.y += 1;
        if (Input.GetKey(inputDown))  inputVector.y -= 1;
        if (Input.GetKey(inputLeft))  inputVector.x -= 1;
        if (Input.GetKey(inputRight)) inputVector.x += 1;

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

        // Tüm yön sprite'larını gizle
        SetDirectionVisible(spritesUp,    sprites.body == spritesUp.body);
        SetDirectionVisible(spritesDown,  sprites.body == spritesDown.body);
        SetDirectionVisible(spritesLeft,  sprites.body == spritesLeft.body);
        SetDirectionVisible(spritesRight, sprites.body == spritesRight.body);

        activeSprites = sprites;

        // Hareket yoksa idle animasyonuna geç
        bool isIdle = direction == Vector2.zero;
        activeSprites.body.idle = isIdle;
        activeSprites.head.idle = isIdle;
    }

    private void SetDirectionVisible(DirectionSprites sprites, bool visible)
    {
        sprites.body.enabled = visible;
        sprites.head.enabled = visible;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            DeathSequence();
        }
    }

    private void DeathSequence()
    {
        enabled = false;
        var bombCtrl = GetComponent<BombController>();
        if (bombCtrl != null) bombCtrl.enabled = false;

        SetDirectionVisible(spritesUp,    false);
        SetDirectionVisible(spritesDown,  false);
        SetDirectionVisible(spritesLeft,  false);
        SetDirectionVisible(spritesRight, false);

        // Ölüm sprite'larını göster
        spriteDeathBody.enabled = true;
    }
}