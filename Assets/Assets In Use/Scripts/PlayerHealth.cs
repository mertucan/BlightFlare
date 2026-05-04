using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHearts = 6;
    public int currentHearts = 6;

    [Header("Damage Sound")]
    public AudioSource audioSource;
    public AudioClip[] damageClips;
    public int selectedDamageClipIndex = 0;

    [Header("Invincibility")]
    public float invincibilityDuration = 1.2f;

    private float invincibilityTimer = 0f;

    public float NormalizedHealth => (float)currentHearts / maxHearts;

    private void Update()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(int halfHearts = 1)
    {
        // Her TakeDamage çağrısını logla — ses nereden geliyor bulmak için
        Debug.Log($"[PlayerHealth] TakeDamage({halfHearts}) çağrıldı. " +
                  $"currentHearts:{currentHearts}, invTimer:{invincibilityTimer:F2}", gameObject);

        if (currentHearts <= 0) return;
        if (invincibilityTimer > 0f)
        {
            Debug.Log("[PlayerHealth] İnvincibility aktif, hasar engellendi.");
            return;
        }

        // ── Holy Mantle kontrolü ──────────────────────────────────────────
        var mantle = GetComponent<HolyMantleEffect>();
        if (mantle != null && mantle.TryBlockDamage())
        {
            invincibilityTimer = invincibilityDuration;
            GetComponent<IsaacMovement>()?.TriggerDamageFlash(invincibilityDuration);
            Debug.Log("[PlayerHealth] Holy Mantle hasarı engelledi!");
            return;
        }
        // ─────────────────────────────────────────────────────────────────

        currentHearts -= halfHearts;
        currentHearts  = Mathf.Max(0, currentHearts);

        HeartUI ui = FindFirstObjectByType<HeartUI>();
        if (ui != null) ui.UpdateHearts(currentHearts);

        if (currentHearts <= 0)
        {
            GameOverScreen.instance?.Show();
            // Fizigi hemen durdur
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic; // artik hic bir force etkilemesin
            }

            // Collider'lari kapat - dusман/mermi carpmaya devam etmesin
            foreach (var col in GetComponents<Collider2D>())
                col.enabled = false;

            var movement = GetComponent<IsaacMovement>();
            if (movement != null) movement.DeathSequence();
        }
        else
        {
            PlayDamageSound();
            invincibilityTimer = invincibilityDuration;
            GetComponent<IsaacMovement>()?.TriggerDamageFlash(invincibilityDuration);
        }
    }

    public void Heal(int halfHearts = 1)
    {
        currentHearts = Mathf.Min(currentHearts + halfHearts, maxHearts);
        HeartUI ui = FindFirstObjectByType<HeartUI>();
        if (ui != null) ui.UpdateHearts(currentHearts);
    }

    private void PlayDamageSound()
    {
        Debug.Log($"[PlayerHealth] PlayDamageSound — clip:{(damageClips != null && damageClips.Length > 0 ? damageClips[selectedDamageClipIndex]?.name : "null")}");
        if (audioSource == null || damageClips == null || damageClips.Length == 0) return;
        if (selectedDamageClipIndex < 0 || selectedDamageClipIndex >= damageClips.Length) return;
        audioSource.PlayOneShot(damageClips[selectedDamageClipIndex]);
    }
}