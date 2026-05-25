using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BombPush : MonoBehaviour
{
    [Header("Kayma Ayarları")]
    public float kickForce = 5f;
    public float friction  = 2f;
    public LayerMask wallLayer;

    private Rigidbody2D      rb;
    private CircleCollider2D col;
    private float            lastKickTime;

    // Bombanın üstünde spawn olan oyuncuyu takip ediyoruz.
    // Oyuncu fiziksel olarak bombanın dışına çıktığında Trigger kapatılır.
    private bool  waitingForPlayerExit = false;
    private float playerExitCheckRadius;

    // Oyuncu referansı — tag üzerinden bulunur.
    private Transform playerTransform;

    private void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();

        rb.gravityScale          = 0;
        rb.freezeRotation        = true;
        rb.bodyType              = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.linearDamping         = friction;
        rb.angularDamping        = 0;

        // Başlangıçta trigger: bomba içinde spawn olan oyuncuyla çakışma yaratmaz
        col.isTrigger = true;

        // Kontrol yarıçapı: collider yarıçapı + oyuncu yarıçapı tahmini
        // Oyuncunun collider'ı yaklaşık 0.4 birim — gerekirse Inspector'dan ayarlayın
        playerExitCheckRadius = col.radius + 0.45f;
    }

    private void Start()
    {
        // Oyuncu referansını bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;

        // Bomba, oyuncunun üstüne mi kondu? Eğer öyleyse exit bekle.
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist < playerExitCheckRadius)
            {
                waitingForPlayerExit = true;
            }
            else
            {
                // Oyuncu bombanın üstünde değil, direkt solid yap
                col.isTrigger = false;
            }
        }
        else
        {
            col.isTrigger = false;
        }
    }

    private void Update()
    {
        // Oyuncu bombadan yeterince uzaklaştığında collider'ı solid yap.
        // Bu FixedUpdate yerine Update'te: fizik frame'i beklemeye gerek yok,
        // mesafe kontrolü yeterince güvenilir.
        if (waitingForPlayerExit && playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist >= playerExitCheckRadius)
            {
                col.isTrigger        = false;
                waitingForPlayerExit = false;
            }
        }
    }

    // ── Trigger modundayken Wall/Door'a çarpınca dur ─────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        StopIfBlocked(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        StopIfBlocked(other.gameObject);
    }

    // ── Solid modundayken duvarla/kapıyla çarpışma ───────────────────────
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
        bool isDoor = other.CompareTag("Door") || other.CompareTag("Wall");

        if (isWall || isDoor)
        {
            rb.linearVelocity  = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    private void HandleKick(GameObject kicker)
    {
        if (!kicker.CompareTag("Player")) return;
        if (Time.time <= lastKickTime + 0.1f) return;

        Vector2 direction = transform.position - kicker.transform.position;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            direction = new Vector2(Mathf.Sign(direction.x), 0);
        else
            direction = new Vector2(0, Mathf.Sign(direction.y));

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.6f);
        if (hit.collider != null)
        {
            bool isWall = ((1 << hit.collider.gameObject.layer) & wallLayer) != 0;
            bool isDoor = hit.collider.CompareTag("Door") || hit.collider.CompareTag("Wall");
            if (isWall || isDoor) return;
        }

        rb.AddForce(direction * kickForce, ForceMode2D.Impulse);
        lastKickTime = Time.time;
    }
}