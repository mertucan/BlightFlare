using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PooterProjectile : MonoBehaviour
{
    public float speed = 4.5f;
    public int damage = 1;
    public float lifetime = 6f;
    public string projectileLayerName = "Explosion";
    public LayerMask wallLayer;
    [Tooltip("Room/Stage collider layer mask. Degerse duvara carpmis gibi destroy eder.")]
    public LayerMask roomColliderLayer;
    public bool destroyOnAnyNonPlayerTriggerWhenWallMaskEmpty;
    public Animator bloodAnimator;
    public bool playImpactAnimationOnWallHit = true;
    public float impactDestroyDelay = 0.25f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 moveDirection = Vector2.right;
    private bool isImpacting;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        int projectileLayer = LayerMask.NameToLayer(projectileLayerName);
        if (projectileLayer >= 0) gameObject.layer = projectileLayer;

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 50;
        }
    }

    private void OnEnable()
    {
        Invoke(nameof(SelfDestruct), Mathf.Max(0.1f, lifetime));
    }

    public void Initialize(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        moveDirection = direction.normalized;
        rb.linearVelocity = moveDirection * speed;
    }

    private void FixedUpdate()
    {
        if (isImpacting) return;
        rb.linearVelocity = moveDirection * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isImpacting) return;

        if (other.CompareTag("Player"))
        {
            IsaacMovement isaac = other.GetComponent<IsaacMovement>();
            if (isaac != null) isaac.DeathSequence();

            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Enemy") || other.GetComponent<PooterAI>() != null) return;

        bool wallMaskDefined = wallLayer.value != 0;
        bool isWall = wallMaskDefined && ((wallLayer.value & (1 << other.gameObject.layer)) != 0);
        bool roomMaskDefined = roomColliderLayer.value != 0;
        bool isRoomCollider = roomMaskDefined && ((roomColliderLayer.value & (1 << other.gameObject.layer)) != 0);
        bool solidRoomHit = !other.isTrigger && !other.CompareTag("Enemy") && other.gameObject.layer != LayerMask.NameToLayer("Enemy");

        if (isWall || isRoomCollider || solidRoomHit || (!wallMaskDefined && destroyOnAnyNonPlayerTriggerWhenWallMaskEmpty))
            ImpactAndDestroy();
    }

    private void ImpactAndDestroy()
    {
        if (!playImpactAnimationOnWallHit || bloodAnimator == null)
        {
            Destroy(gameObject);
            return;
        }

        isImpacting = true;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (col != null) col.enabled = false;

        bloodAnimator.SetTrigger("Impact");
        Destroy(gameObject, Mathf.Max(0f, impactDestroyDelay));
    }

    private void SelfDestruct()
    {
        if (this != null) Destroy(gameObject);
    }
}
