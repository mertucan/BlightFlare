using UnityEngine;

public class BloodBag : MonoBehaviour, IItem
{
    // ─────────────────────────────────────────
    // HEAL SETTINGS
    // ─────────────────────────────────────────
    [Header("Blood Bag Settings")]
    [Tooltip("Kaç yarım kalp slotu ekleneceği (varsayılan 2 = 1 tam kalp).")]
    public int halfHeartsToAdd = 2;
    [Header("UI Icon")]
    public Sprite iconSprite;           // ← EKLE

    // ─────────────────────────────────────────
    // PICKUP SOUND
    // ─────────────────────────────────────────
    [Header("Pickup Sound")]
    public AudioClip[] pickupSoundClips;
    public int selectedPickupClipIndex = 0;

    // ─────────────────────────────────────────
    // IItem
    // ─────────────────────────────────────────
    public void Pickup(GameObject player)
    {
        PlayerHealth ph      = player.GetComponent<PlayerHealth>();
        HeartUI      heartUI = Object.FindFirstObjectByType<HeartUI>();

        if (ph == null)
        {
            Debug.LogWarning("[BloodBag] PlayerHealth bulunamadı!");
            return;
        }

        ph.maxHearts    += halfHeartsToAdd;
        ph.currentHearts = ph.maxHearts;

        if (heartUI != null)
        {
            heartUI.maxHalfHearts = ph.maxHearts;
            heartUI.RebuildSlots();
            heartUI.UpdateHearts(ph.currentHearts);
            // Yeniden konumlandırma bir frame bekleyerek yapılır
            heartUI.RepositionHolyMantleAfterFrame();
        }

        PlayPickupSound(player);
        ItemIconUI.Instance?.AddItemIcon(iconSprite, "BloodBag");
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────
    // TRIGGER
    // ─────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Pickup(other.gameObject);
    }

    private void PlayPickupSound(GameObject player)
    {
        if (pickupSoundClips == null || pickupSoundClips.Length == 0) return;
        if (selectedPickupClipIndex < 0 || selectedPickupClipIndex >= pickupSoundClips.Length) return;
        if (pickupSoundClips[selectedPickupClipIndex] == null) return;

        AudioSource src = player.GetComponent<AudioSource>()
                       ?? player.AddComponent<AudioSource>();

        src.PlayOneShot(pickupSoundClips[selectedPickupClipIndex]);
    }
}