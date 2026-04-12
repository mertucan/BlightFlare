using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bob's Curse bombası patladıktan sonra zeminde oluşan zehir bulutu.
/// - 5 saniye kalır.
/// - İçindeki sprite'ları yeşile boyar.
/// - Düşmanlara (BabyAI, PooterAI, DOF_AI) periyodik hasar verir.
/// - Player'a ZARAR VERMEZ.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class PoisonCloud : MonoBehaviour
{
    [Header("Cloud Settings")]
    public float duration      = 5f;
    public float damageInterval = 1f;
    public int   damagePerTick  = 1;
    public Color poisonTint     = new Color(0.4f, 1f, 0.4f, 1f);

    [Header("Enemy Layer / Tag")]
    [Tooltip("Bu layer'daki nesneler hasar alır. Boş bırakırsanız tag kontrolü kullanılır.")]
    public LayerMask enemyLayers;
    [Tooltip("Player tag'i — bu tag'e sahip nesneler hasar almaz.")]
    public string playerTag = "Player";

    private SpriteRenderer cloudRenderer;
    private float elapsed;

    // Düşman takip yapıları
    private readonly Dictionary<GameObject, List<SpriteRenderer>> tintedObjects = new();
    // BabyAI listesi
    private readonly Dictionary<BabyAI,   float> babyTimers   = new();
    // PooterAI listesi
    private readonly Dictionary<PooterAI, float> pooterTimers = new();
    // DOF_AI listesi (varsa)
    // DOF_AI TakeDamage public değil; ona sadece tint uygularız.

    private void Awake()
    {
        cloudRenderer = GetComponent<SpriteRenderer>();
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Start()
    {
        StartCoroutine(FadeAndDestroy());
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // Solma efekti (son 2 saniyede)
        float fadeStart = duration - 2f;
        if (elapsed >= fadeStart)
        {
            float alpha = Mathf.Lerp(1f, 0f, (elapsed - fadeStart) / 2f);
            var c = cloudRenderer.color;
            cloudRenderer.color = new Color(c.r, c.g, c.b, alpha);
        }

        // BabyAI hasar
        foreach (var key in new List<BabyAI>(babyTimers.Keys))
        {
            if (key == null) { babyTimers.Remove(key); continue; }
            if (Time.time - babyTimers[key] >= damageInterval)
            {
                key.TakeDamage(damagePerTick, Vector2.zero);
                babyTimers[key] = Time.time;
            }
        }

        // PooterAI hasar
        foreach (var key in new List<PooterAI>(pooterTimers.Keys))
        {
            if (key == null) { pooterTimers.Remove(key); continue; }
            if (Time.time - pooterTimers[key] >= damageInterval)
            {
                key.TakeDamage(damagePerTick, Vector2.zero);
                pooterTimers[key] = Time.time;
            }
        }
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(duration);
        CleanupAllTints();
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player'ı kesinlikle atla
        if (other.CompareTag(playerTag)) return;

        ApplyTint(other.gameObject);

        // BabyAI
        var baby = other.GetComponent<BabyAI>();
        if (baby != null && !babyTimers.ContainsKey(baby))
        {
            baby.TakeDamage(damagePerTick, Vector2.zero);
            babyTimers[baby] = Time.time;
            return;
        }

        // PooterAI
        var pooter = other.GetComponent<PooterAI>();
        if (pooter != null && !pooterTimers.ContainsKey(pooter))
        {
            pooter.TakeDamage(damagePerTick, Vector2.zero);
            pooterTimers[pooter] = Time.time;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) return;

        RemoveTint(other.gameObject);

        var baby   = other.GetComponent<BabyAI>();
        if (baby   != null) babyTimers.Remove(baby);

        var pooter = other.GetComponent<PooterAI>();
        if (pooter != null) pooterTimers.Remove(pooter);
    }

    private void ApplyTint(GameObject target)
    {
        var renderers = new List<SpriteRenderer>(
            target.GetComponentsInChildren<SpriteRenderer>());
        if (renderers.Count == 0) return;

        tintedObjects[target] = renderers;
        foreach (var sr in renderers)
            if (sr != null) sr.color = poisonTint;
    }

    private void RemoveTint(GameObject target)
    {
        if (!tintedObjects.TryGetValue(target, out var renderers)) return;
        foreach (var sr in renderers)
            if (sr != null) sr.color = Color.white;
        tintedObjects.Remove(target);
    }

    private void CleanupAllTints()
    {
        foreach (var kvp in tintedObjects)
        {
            if (kvp.Key == null) continue;
            foreach (var sr in kvp.Value)
                if (sr != null) sr.color = Color.white;
        }
        tintedObjects.Clear();
        babyTimers.Clear();
        pooterTimers.Clear();
    }
}