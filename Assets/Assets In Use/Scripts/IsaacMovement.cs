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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            DeathSequence();
        }
    }

    public void DeathSequence()
    {
        enabled = false;
        var bombCtrl = GetComponent<BombController>();
        if (bombCtrl != null) bombCtrl.enabled = false;

        SetDirectionVisible(spritesUp,    false);
        SetDirectionVisible(spritesDown,  false);
        SetDirectionVisible(spritesLeft,  false);
        SetDirectionVisible(spritesRight, false);

        spriteDeathBody.enabled = true;
    }
}
