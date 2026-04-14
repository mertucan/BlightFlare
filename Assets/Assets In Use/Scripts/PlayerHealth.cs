using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHearts = 6;       // Maksimum can (half-heart birimi)
    public int currentHearts = 6;   // Mevcut can

    [Header("Damage Sound")]
    public AudioSource audioSource;
    public AudioClip[] damageClips;
    public int selectedDamageClipIndex = 0;

    [Header("Invincibility")]
    [Tooltip("Hasar aldıktan sonra geçici dokunulmazlık süresi (saniye).")]
    public float invincibilityDuration = 1.2f;

    private float invincibilityTimer = 0f;

    public float NormalizedHealth => (float)currentHearts / maxHearts;

    private void Update()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;
    }

    /// <summary>Half-heart cinsinden hasar ver.</summary>
    public void TakeDamage(int halfHearts = 1)
    {
        if (currentHearts <= 0) return;

        // Dokunulmazlık süresi aktifse hasar alma
        if (invincibilityTimer > 0f) return;

        currentHearts -= halfHearts;
        currentHearts  = Mathf.Max(0, currentHearts);

        // UI'ı güncelle
        HeartUI ui = FindFirstObjectByType<HeartUI>();
        if (ui != null) ui.UpdateHearts(currentHearts);

        if (currentHearts <= 0)
        {
            // Ölünce hasar sesi değil, IsaacMovement'taki ölüm sesi çalar
            var movement = GetComponent<IsaacMovement>();
            if (movement != null) movement.DeathSequence();
        }
        else
        {
            // Can gitti ama ölmedi → hasar sesi çal
            PlayDamageSound();
            invincibilityTimer = invincibilityDuration;
            GetComponent<IsaacMovement>()?.TriggerDamageFlash(invincibilityDuration);
        }
    }

    /// <summary>Can ekle (max'ı aşmaz).</summary>
    public void Heal(int halfHearts = 1)
    {
        currentHearts = Mathf.Min(currentHearts + halfHearts, maxHearts);

        HeartUI ui = FindFirstObjectByType<HeartUI>();
        if (ui != null) ui.UpdateHearts(currentHearts);
    }

    private void PlayDamageSound()
    {
        if (audioSource == null || damageClips == null || damageClips.Length == 0) return;
        if (selectedDamageClipIndex < 0 || selectedDamageClipIndex >= damageClips.Length) return;
        audioSource.PlayOneShot(damageClips[selectedDamageClipIndex]);
    }
}