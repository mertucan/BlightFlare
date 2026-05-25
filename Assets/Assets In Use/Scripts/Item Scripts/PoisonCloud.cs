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
    public float duration       = 5f;
    public float damageInterval = 1f;
    public int   damagePerTick  = 1;
    public Color poisonTint     = new Color(0.4f, 1f, 0.4f, 1f);

    [Header("Tags")]
    [Tooltip("Player tag'i — bu tag'e sahip nesneler hasar almaz.")]
    public string playerTag = "Player";

    // ── İç durum ─────────────────────────────────────────────────────────
    private SpriteRenderer cloudRenderer;
    private float          elapsed;

    // Hasar takibi — FireHazard ile aynı yapı
    private readonly Dictionary<GameObject, float>                  damageTimers  = new();

    // Tint rengi uygulanan nesneler
    private readonly Dictionary<GameObject, List<SpriteRenderer>>   tintedObjects = new();

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        cloudRenderer = GetComponent<SpriteRenderer>();

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    private void Start()
    {
        StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // ── Solma (son 2 saniyede) ────────────────────────────────────────
        float fadeStart = duration - 2f;
        if (elapsed >= fadeStart)
        {
            float alpha = Mathf.Lerp(1f, 0f, (elapsed - fadeStart) / 2f);
            var c = cloudRenderer.color;
            cloudRenderer.color = new Color(c.r, c.g, c.b, alpha);
        }

        // ── Periyodik hasar (FireHazard ile birebir aynı döngü) ───────────
        foreach (var key in new List<GameObject>(damageTimers.Keys))
        {
            if (key == null) { damageTimers.Remove(key); continue; }
            if (Time.time - damageTimers[key] < damageInterval) continue;

            DealDamageTo(key);
            damageTimers[key] = Time.time;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);
        CleanupAllTints();
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player'ı kesinlikle atla
        if (other.CompareTag(playerTag)) return;

        GameObject go = other.gameObject;

        ApplyTint(go);

        // İlk temas hasarı + timer kayıt
        if (!damageTimers.ContainsKey(go))
        {
            DealDamageTo(go);
            damageTimers[go] = Time.time;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) return;

        RemoveTint(other.gameObject);
        damageTimers.Remove(other.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────
    /// <summary>
    /// FireHazard'daki DealDamageTo ile aynı mantık —
    /// tek fark: Player bloğu hiç hasar vermeden return eder.
    /// </summary>
    private void DealDamageTo(GameObject go)
    {
        // ── Player → hasar YOK ───────────────────────────────────────────
        if (go.CompareTag(playerTag)) return;

        // ── BabyAI ───────────────────────────────────────────────────────
        var baby = go.GetComponent<BabyAI>();
        if (baby != null)
        {
            baby.TakeDamage(damagePerTick, Vector2.zero);
            return;
        }

        // ── PooterAI ─────────────────────────────────────────────────────
        var pooter = go.GetComponent<PooterAI>();
        if (pooter != null)
        {
            pooter.TakeDamage(damagePerTick, Vector2.zero);
            return;
        }

        // ── DOF_AI — FireHazard ile aynı reflection yaklaşımı ────────────
        var dof = go.GetComponent<DOF_AI>();
        if (dof != null)
        {
            var method = typeof(DOF_AI).GetMethod(
                "TakeDamage",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method?.Invoke(dof, new object[] { Vector2.zero });
        }
    }

    // ── Tint ─────────────────────────────────────────────────────────────
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
        damageTimers.Clear();
    }
}