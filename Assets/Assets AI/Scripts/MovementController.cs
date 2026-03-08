using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direction = Vector2.zero;
    public float speed = 5f;

    [Header("Input")]
    public KeyCode inputUp = KeyCode.W;
    public KeyCode inputDown = KeyCode.S;
    public KeyCode inputLeft = KeyCode.A;
    public KeyCode inputRight = KeyCode.D;

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
        activeSpriteRenderer = spriteRendererDown;
    }

    private void Update()
    {
        // Giriş vektörünü her karede sıfırla
        Vector2 inputVector = Vector2.zero;

        // else-if yerine ayrı if blokları kullanarak kombinasyonlara izin ver
        if (Input.GetKey(inputUp)) {
            inputVector.y += 1;
        }
        if (Input.GetKey(inputDown)) {
            inputVector.y -= 1;
        }
        if (Input.GetKey(inputLeft)) {
            inputVector.x -= 1;
        }
        if (Input.GetKey(inputRight)) {
            inputVector.x += 1;
        }

        // Eğer herhangi bir giriş varsa
        if (inputVector != Vector2.zero) {
            // Yönü normalize et (böylece çapraz giderken hız 1.4 katına çıkmaz)
            direction = inputVector.normalized;

            // Çapraz giderken hangi sprite'ın görüneceğine karar ver
            // Öncelik sırası: Yatay hareket varsa yan sprite, yoksa dikey sprite
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
            // Hareket yoksa dur
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
        
        // Hareket vektörü sıfırsa idle animasyonuna geç
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