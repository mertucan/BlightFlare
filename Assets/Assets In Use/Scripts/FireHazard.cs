using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hot Bomb patladıktan sonra zeminde 5 saniye yanan ateş alanı.
/// Hem player'a hem düşmanlara (BabyAI, PooterAI, DOF_AI) hasar verir.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class FireHazard : MonoBehaviour
{
    [Header("Fire Settings")]
    public float duration       = 5f;
    public float damageInterval = 1f;
    public int   damagePerTick  = 1;
    public Color fireTint       = new Color(1f, 0.45f, 0.1f, 1f);

    [Header("Sprite Animation")]
    public Sprite[] fireSprites;
    public float    animFps = 8f;

    [Header("Tags")]
    public string playerTag = "Player";

    // ── İç durum ─────────────────────────────────────────────────────────
    private SpriteRenderer sr;
    private float          elapsed;

    // Animasyon
    private float animTimer;
    private int   animFrame;

    // Hasar takibi — her nesne için son hasar zamanını tut
    private readonly Dictionary<GameObject, float> damageTimers = new();

    // Tint rengi uygulanan nesneler
    private readonly Dictionary<GameObject, List<SpriteRenderer>> tintedObjects = new();

    // Player referansı (invincibility kontrolü için)
    private PlayerHealth playerHealth;

    // ─────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        var col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    private void Start()
    {
        sr.color = fireTint;
        StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        // ── Sprite animasyonu ─────────────────────────────────────────────
        if (fireSprites != null && fireSprites.Length > 1)
        {
            animTimer += Time.deltaTime;
            if (animTimer >= 1f / animFps)
            {
                animTimer  = 0f;
                animFrame  = (animFrame + 1) % fireSprites.Length;
                sr.sprite  = fireSprites[animFrame];
            }
        }

        // ── Solma (son 2 saniyede) ────────────────────────────────────────
        float fadeStart = duration - 2f;
        if (elapsed >= fadeStart)
        {
            float alpha = Mathf.Lerp(1f, 0f, (elapsed - fadeStart) / 2f);
            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
        }

        // ── Periyodik hasar ───────────────────────────────────────────────
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
        GameObject go = other.gameObject;

        // Tint uygula
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
        RemoveTint(other.gameObject);
        damageTimers.Remove(other.gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────
    private void DealDamageTo(GameObject go)
    {
        // ── Player ───────────────────────────────────────────────────────
        if (go.CompareTag(playerTag))
        {
            var health = go.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(damagePerTick);
            return;
        }

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

        // ── DOF_AI — reflection ile TakeDamage çağrısı (method public değil) ──
        // DOF_AI'nın TakeDamage'i private olduğu için Explosion layer'ını taklit ediyoruz.
        // Alternatif olarak DOF_AI'da public bir wrapper açabilirsin.
        var dof = go.GetComponent<DOF_AI>();
        if (dof != null)
        {
            // DOF_AI yalnızca Explosion layer'ındaki trigger'lara tepki veriyor.
            // En temiz yol: DOF_AI'ya public TakeBurnDamage() eklemek.
            // Şimdilik reflection kullanıyoruz — ileride public yapabilirsin.
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
        foreach (var r in renderers)
            if (r != null) r.color = new Color(1f, 0.5f, 0.1f, 1f); // turuncu tint
    }

    private void RemoveTint(GameObject target)
    {
        if (!tintedObjects.TryGetValue(target, out var renderers)) return;
        foreach (var r in renderers)
            if (r != null) r.color = Color.white;
        tintedObjects.Remove(target);
    }

    private void CleanupAllTints()
    {
        foreach (var kvp in tintedObjects)
        {
            if (kvp.Key == null) continue;
            foreach (var r in kvp.Value)
                if (r != null) r.color = Color.white;
        }
        tintedObjects.Clear();
        damageTimers.Clear();
    }
}