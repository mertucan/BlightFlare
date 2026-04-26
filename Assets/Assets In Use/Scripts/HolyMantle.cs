using UnityEngine;

public class HolyMantle : MonoBehaviour, IItem
{
    [Header("Character Overlay (Animated)")]
    public Sprite[] overlaySprites;
    public float overlayFps = 8f;
    public int overlayOrderInLayer = 10;
    public Vector2 overlayOffset = new Vector2(0.15f, -0.1f);

    [Header("UI Icon (Single Sprite)")]
    public Sprite uiSprite;

    [Header("Shield Break Sound")]
    public AudioClip[] shieldBreakClips;
    public int selectedClipIndex = 0;

    // ─────────────────────────────────────────
    public void Pickup(GameObject player)
    {
        var existing = player.GetComponent<HolyMantleEffect>();
        if (existing != null)
        {
            existing.ActivateShield();
        }
        else
        {
            var effect = player.AddComponent<HolyMantleEffect>();
            effect.overlaySprites      = overlaySprites;
            effect.overlayFps          = overlayFps;
            effect.overlayOrderInLayer = overlayOrderInLayer;
            effect.overlayOffset       = overlayOffset;
            effect.uiSprite            = uiSprite;
            effect.audioSource         = player.GetComponent<AudioSource>()
                                    ?? player.AddComponent<AudioSource>();
            effect.shieldBreakClips    = shieldBreakClips;
            effect.selectedClipIndex   = selectedClipIndex;
            effect.ActivateShield();
        }

        // Konumlandırmayı HeartUI üzerinden yap
        HeartUI heartUI = Object.FindFirstObjectByType<HeartUI>();
        heartUI?.RepositionHolyMantleAfterFrame();
    }

    // ─────────────────────────────────────────
    /// <summary>
    /// HeartUI panelindeki son slot'un gerçek X pozisyonunu okur,
    /// üstüne 100 px ekleyerek HolyMantleUI'ı konumlandırır.
    /// </summary>

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Pickup(other.gameObject);
    }
}