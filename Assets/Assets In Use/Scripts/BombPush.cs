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
        
        // FİZİK AYARLARI
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Sürtünme ayarı
        rb.linearDamping = friction; 
        rb.angularDamping = 0;

        // ÖNEMLİ: Bomba doğar doğmaz "Trigger" (Hayalet) olsun.
        // Böylece oyuncu içinde doğabilir.
        col.isTrigger = true;
    }

    // 1. AŞAMA: OYUNCU BOMBANIN İÇİNDEN ÇIKINCA KATI YAP
    private void OnTriggerExit2D(Collider2D other)
    {
        // Eğer içimden çıkan şey Player ise ve ben hala Trigger isem
        if (other.CompareTag("Player") && col.isTrigger)
        {
            col.isTrigger = false; // Trigger'ı kapat, artık bombaya çarpılabilir!
        }
    }

    // 2. AŞAMA: BOMBA ARTIK KATI, ŞİMDİ İTTİRİLEBİLİR
    // OnCollisionEnter yerine Stay kullanıyoruz ki yapışıkken de itebilelim
    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleKick(collision.gameObject);
    }

    // İlk çarpışma anını da kaçırmamak için Enter'ı da ekleyelim
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleKick(collision.gameObject);
    }

    private void HandleKick(GameObject kicker)
    {
        // Player çarpıyorsa ve bekleme süresi dolduysa
        if (kicker.CompareTag("Player") && Time.time > lastKickTime + 0.1f)
        {
            Vector2 direction = transform.position - kicker.transform.position;

            // 4 Ana yön hesabı (Isaac Tarzı)
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                direction = new Vector2(Mathf.Sign(direction.x), 0);
            else
                direction = new Vector2(0, Mathf.Sign(direction.y));

            // Gideceğimiz yönde duvar var mı kontrolü (Raycast)
            // Bombanın kendi collider'ına çarpmasın diye merkezden biraz ileriye (0.6f) bakıyoruz.
            if (!Physics2D.Raycast(transform.position, direction, 0.6f, wallLayer))
            {
                // ForceMode2D.Impulse anlık vuruş hissi verir
                rb.AddForce(direction * kickForce, ForceMode2D.Impulse);
                lastKickTime = Time.time;
            }
        }
    }
}