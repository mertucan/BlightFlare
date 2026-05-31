using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direction = Vector2.zero;
    public float speed = IsaacMovement.DefaultSpeed;

    [Header("Input")]
    public Key moveUp = Key.W;
    public Key moveDown = Key.S;
    public Key moveLeft = Key.A;
    public Key moveRight = Key.D;

    [Header("Sprites")]
    public AnimatedSpriteRenderer spriteRendererUp;
    public AnimatedSpriteRenderer spriteRendererDown;
    public AnimatedSpriteRenderer spriteRendererLeft;
    public AnimatedSpriteRenderer spriteRendererRight;
    public AnimatedSpriteRenderer spriteRendererDeath;
    private AnimatedSpriteRenderer activeSpriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        speed = Mathf.Min(speed, IsaacMovement.MaxNormalSpeed);
        activeSpriteRenderer = spriteRendererDown;
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

        if (inputVector != Vector2.zero) {
            direction = inputVector.normalized;

            AnimatedSpriteRenderer targetSprite = activeSpriteRenderer;

            if (inputVector.x > 0) {
                targetSprite = spriteRendererRight;
            } else if (inputVector.x < 0) {
                targetSprite = spriteRendererLeft;
            } else if (inputVector.y > 0) {
                targetSprite = spriteRendererUp;
            } else if (inputVector.y < 0) {
                targetSprite = spriteRendererDown;
            }

            SetDirection(direction, targetSprite);
        } 
        else {
            SetDirection(Vector2.zero, activeSpriteRenderer);
        }
    }

    private void FixedUpdate()
    {
        Vector2 position = rb.position;
        Vector2 translation = speed * Time.fixedDeltaTime * direction;

        rb.MovePosition(position + translation);
    }

    private void SetDirection(Vector2 newDirection, AnimatedSpriteRenderer spriteRenderer)
    {
        direction = newDirection;

        spriteRendererUp.enabled = spriteRenderer == spriteRendererUp;
        spriteRendererDown.enabled = spriteRenderer == spriteRendererDown;
        spriteRendererLeft.enabled = spriteRenderer == spriteRendererLeft;
        spriteRendererRight.enabled = spriteRenderer == spriteRendererRight;

        activeSpriteRenderer = spriteRenderer;
        activeSpriteRenderer.idle = direction == Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion")) {
            DeathSequence();
        }
    }

    private void DeathSequence()
    {
        enabled = false;
        GetComponent<BombController>().enabled = false;

        spriteRendererUp.enabled = false;
        spriteRendererDown.enabled = false;
        spriteRendererLeft.enabled = false;
        spriteRendererRight.enabled = false;
        spriteRendererDeath.enabled = true;
    }
}
