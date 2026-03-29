using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BombPush : MonoBehaviour
{
    [Header("Kayma Ayarları")]
    public float kickForce = 5f;       
    public float friction = 2f;        
    public LayerMask wallLayer;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private float lastKickTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearDamping = friction; 
        rb.angularDamping = 0;
        col.isTrigger = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && col.isTrigger)
        {
            col.isTrigger = false;
        }
    }

    // ✅ Kapı Trigger olduğu için buradan yakalanıyor
    private void OnTriggerEnter2D(Collider2D other)
    {
        StopIfBlocked(other.gameObject);
    }

    // ✅ Kapının içinde kalırsa da dur
    private void OnTriggerStay2D(Collider2D other)
    {
        StopIfBlocked(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        StopIfBlocked(collision.gameObject);
        HandleKick(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        StopIfBlocked(collision.gameObject);
        HandleKick(collision.gameObject);
    }

    private void StopIfBlocked(GameObject other)
    {
        bool isWall = ((1 << other.layer) & wallLayer) != 0;
        bool isDoor = other.CompareTag("Door");

        if (isWall || isDoor)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void HandleKick(GameObject kicker)
    {
        if (kicker.CompareTag("Player") && Time.time > lastKickTime + 0.1f)
        {
            Vector2 direction = transform.position - kicker.transform.position;

            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                direction = new Vector2(Mathf.Sign(direction.x), 0);
            else
                direction = new Vector2(0, Mathf.Sign(direction.y));

            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.6f);

            if (hit.collider != null)
            {
                bool isWall = ((1 << hit.collider.gameObject.layer) & wallLayer) != 0;
                bool isDoor = hit.collider.CompareTag("Door");
                if (isWall || isDoor) return;
            }

            rb.AddForce(direction * kickForce, ForceMode2D.Impulse);
            lastKickTime = Time.time;
        }
    }
}