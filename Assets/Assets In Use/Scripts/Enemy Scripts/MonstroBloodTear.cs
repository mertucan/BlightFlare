using System.Collections;
using UnityEngine;

/// <summary>
/// Monstro'nun kan gözyaşı mermisi.
///
/// PREFAB HIERARCHY:
///   BloodTear (Root)  ← MonstroBloodTear, Rigidbody2D (Kinematic, gravityScale=0)
///     SpriteHolder    ← SpriteRenderer
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MonstroBloodTear : MonoBehaviour
{
    [Header("Tear Settings")]
    public float  arcHeight           = 1.4f;
    public float  landingDamageRadius = 0.45f;
    public string playerTag           = "Player";

    [Header("References")]
    public Transform spriteHolder;

    [Header("Visual Curve")]
    public AnimationCurve arcCurve;
    public AnimationCurve scaleCurve;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    [HideInInspector] public bool hasLanded = false;

    private Rigidbody2D rb;
    private Vector2     startPos;
    private Vector2     targetPos;
    private float       flightDuration;
    private float       elapsed;
    private bool        isInitialized = false;

    // ═════════════════════════════════════════════════════════════
    //  INIT
    // ═════════════════════════════════════════════════════════════

    private void Awake()
    {
        rb              = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.bodyType     = RigidbodyType2D.Kinematic;

        if (spriteHolder == null && transform.childCount > 0)
            spriteHolder = transform.GetChild(0);

        BuildDefaultCurves();
    }

    private void BuildDefaultCurves()
    {
        if (arcCurve == null || arcCurve.length == 0)
        {
            arcCurve = new AnimationCurve(
                new Keyframe(0f,   0f, 0f,  2f),
                new Keyframe(0.5f, 1f, 0f,  0f),
                new Keyframe(1f,   0f, -2f, 0f));
        }

        if (scaleCurve == null || scaleCurve.length == 0)
        {
            scaleCurve = new AnimationCurve(
                new Keyframe(0f,   0.6f),
                new Keyframe(0.5f, 1.2f),
                new Keyframe(1f,   0.5f));
        }
    }

    public void Initialize(Vector2 landing, float speed, float arc = -1f)
    {
        startPos  = transform.position;
        targetPos = landing;

        if (arc >= 0f) arcHeight = arc;

        float distance = Vector2.Distance(startPos, targetPos);
        flightDuration = Mathf.Max(0.1f, distance / Mathf.Max(0.5f, speed));
        elapsed        = 0f;
        hasLanded      = false;

        if (spriteHolder != null)
            spriteHolder.localScale = Vector3.one * 0.6f;

        isInitialized = true;
        Log($"Başlatıldı: {startPos} → {targetPos} | süre={flightDuration:F2}s");
    }

    // ═════════════════════════════════════════════════════════════
    //  UPDATE
    // ═════════════════════════════════════════════════════════════

    private void Update()
    {
        if (!isInitialized || hasLanded) return;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightDuration);

        transform.position = Vector2.Lerp(startPos, targetPos, t);

        if (spriteHolder != null)
        {
            float curveY = arcCurve.Evaluate(t);
            spriteHolder.localPosition = new Vector3(0f, arcHeight * curveY, 0f);

            float s = scaleCurve.Evaluate(t);
            spriteHolder.localScale = new Vector3(s, s, 1f);

            float spin = Mathf.Sin(t * Mathf.PI * 3f) * 8f;
            spriteHolder.localRotation = Quaternion.Euler(0f, 0f, spin);
        }

        if (t >= 1f) Land();
    }

    // ═════════════════════════════════════════════════════════════
    //  LANDING
    // ═════════════════════════════════════════════════════════════

    private void Land()
    {
        hasLanded     = true;
        isInitialized = false;

        if (spriteHolder != null)
        {
            spriteHolder.localPosition = Vector3.zero;
            spriteHolder.localRotation = Quaternion.identity;
            spriteHolder.localScale    = Vector3.one;
        }

        DealLandingDamage();
        Destroy(gameObject);
    }

    private void DealLandingDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, landingDamageRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag(playerTag)) continue;
            hit.GetComponent<IsaacMovement>()?.DeathSequence();
            Log("Oyuncuya iniş hasarı!");
            break;
        }
    }

    private void OnTriggerEnter2D(Collider2D other) { }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !isInitialized) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(targetPos, landingDamageRadius);
    }
#endif

    private void Log(string msg)
    {
        if (enableDebugLogs) Debug.Log($"[MonstroBloodTear] {msg}");
    }
}