using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DOF_AI : MonoBehaviour
{
    [Header("Hareket")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Pooter Spawn")]
    [SerializeField] private GameObject pooterPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float spawnRadius = 1.5f;
    [SerializeField] private int maxPooters = 3;
    [SerializeField] private float spawnCooldown = 15f;

    [Header("Can & Hasar")]
    [SerializeField] private int bombsToKill = 5;
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Isaac Temas Hasarı")]
    [SerializeField] private string playerTag = "Player";
    [Header("Ölünce Aktif Olacaklar")]
    [SerializeField] private SpriteRenderer tunnelSpriteRenderer;
    [SerializeField] private Collider2D tunnelCollider2D;

    [Header("Death Sound")]
    [SerializeField] private AudioClip deathClip;
    [SerializeField] [Range(0f, 1f)] private float deathClipVolume = 1f;

    [Header("Death Music")]
    [SerializeField] private AudioClip deathMusic;
    [SerializeField] [Range(0f, 1f)] private float deathMusicVolume = 1f;

    [Header("Hasar Koruması")]
    [SerializeField] private float damageCooldown = 0.5f;
    private float lastDamageTime = -999f;

    private Animator animator;
    private Rigidbody2D rb;

    private Vector2 moveDirection;
    private List<GameObject> activePooters = new List<GameObject>();
    private float spawnTimer;
    private bool isActivated = false;
    private bool isAttacking = false;

    private int currentBombHits = 0;
    private bool isDead = false;
    private Color originalColor;
    private Coroutine flashRoutine;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        float[] diagonals = { 45f, 135f, 225f, 315f };
        float angle = diagonals[Random.Range(0, diagonals.Length)] * Mathf.Deg2Rad;
        moveDirection = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
        spawnTimer = spawnCooldown;
    }

    private void Update()
    {
        if (!isActivated ||isAttacking || isDead) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            activePooters.RemoveAll(p => p == null);
            if (activePooters.Count < maxPooters)
                StartCoroutine(SpawnRoutine());
            else
                spawnTimer = spawnCooldown;
        }
    }

    private void FixedUpdate()
    {
        if (!isActivated || isAttacking || isDead) return;
        
        // Her frame moveDirection'ı uygula — knockback varsa bile üzerine yazar
        rb.linearVelocity = moveDirection * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Wall") || collision.gameObject.CompareTag("Door"))
        {
            Vector2 normal = collision.contacts[0].normal;
            moveDirection = Vector2.Reflect(moveDirection, normal).normalized;
            return;
        }

        // Isaac'a değince öldür
        if (collision.gameObject.CompareTag(playerTag))
            TryKillPlayer(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag(playerTag))
            TryKillPlayer(collision.gameObject);
    }

    private void TryKillPlayer(GameObject player)
    {
        var isaac = player.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(1);
    }

    // Explosion layer'ındaki trigger'a girince bomba hasarı al
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        if (other.GetComponent<PooterProjectile>() != null) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Explosion")) return;

        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
        TakeDamage(knockDir);
    }

    private void TakeDamage(Vector2 knockDir)
    {
        currentBombHits++;

        // knockback kaldırıldı — hareket yönü korunuyor
        // rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(HitFlashRoutine());

        if (currentBombHits >= bombsToKill)
            Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        BossAudioUtility.Play2D(deathClip, deathClipVolume);
        BackgroundMusicManager.instance?.PlayBossDeathMusicThenResume(deathMusic, deathMusicVolume);

        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = true;
        if (tunnelCollider2D != null)     tunnelCollider2D.enabled = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (tunnelSpriteRenderer != null)
        {
            tunnelSpriteRenderer.enabled = true;
            Debug.Log("Tunnel sprite açıldı: " + tunnelSpriteRenderer.gameObject.name);
        }
        else
        {
            Debug.LogWarning("tunnelSpriteRenderer NULL!");
        }
        Destroy(gameObject);
    }

    private IEnumerator SpawnRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;
        animator.SetBool("isAttacking", true);

        float animLength = GetClipLength("DOF_Attack");
        yield return new WaitForSeconds(animLength);

        animator.SetBool("isAttacking", false);
        isAttacking = false;
        spawnTimer = spawnCooldown;
    }

    // DOF_Attack animasyonundaki Animation Event ile çağrılır
    public void SpawnFly()
    {
        if (pooterPrefab == null || firePoint == null) return;

        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
        Vector3 spawnPos = firePoint.position + (Vector3)offset;

        GameObject pooter = Instantiate(pooterPrefab, spawnPos, Quaternion.identity);
        activePooters.Add(pooter);

        var pooterAI = pooter.GetComponent<PooterAI>(); // kendi script adınla değiştir
        if (pooterAI != null) pooterAI.Activate();
    }

    // Pozisyonsuz Activate — DOF zaten doğru yerdeyse (prefab olarak room içine koyulmuşsa)
    public void Activate()
    {
        isActivated = true;
    }

    // Pozisyonlu Activate — oda merkezi veya spawn noktası verilerek çağrılır
    public void Activate(Vector2 spawnPosition)
    {
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        isActivated = true;
    }

    private float GetClipLength(string clipName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == clipName)
                return clip.length;
        return 1f;
    }

    public void SetActive(bool active)
    {
        isActivated = active;
        if (!active && rb != null)
            rb.linearVelocity = Vector2.zero;
    }
}
