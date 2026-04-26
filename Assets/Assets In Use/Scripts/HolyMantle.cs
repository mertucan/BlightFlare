using UnityEngine;

public class HolyMantle : MonoBehaviour, IItem
{
    [Header("Character Overlay (Animated - 4 Sprite)")]
    [Tooltip("Karakterin üstünde animasyonlu dönen sprite dizisi.")]
    public Sprite[] overlaySprites;

    [Tooltip("Overlay animasyon FPS'i.")]
    public float overlayFps = 8f;

    [Tooltip("Overlay'in render sırası.")]
    public int overlayOrderInLayer = 10;

    [Tooltip("Overlay'in karaktere göre offset pozisyonu.")]
    public Vector2 overlayOffset = new Vector2(0.15f, -0.1f);

    [Header("UI Icon (Single Sprite)")]
    [Tooltip("UI'da gösterilecek tek kalkan sprite'ı.")]
    public Sprite uiSprite;

    [Header("Shield Break Sound")]
    public AudioClip[] shieldBreakClips;
    public int selectedClipIndex = 0;

    public void Pickup(GameObject player)
    {
        var existing = player.GetComponent<HolyMantleEffect>();
        if (existing != null)
        {
            existing.ActivateShield();
            return;
        }

        var effect = player.AddComponent<HolyMantleEffect>();
        effect.overlaySprites       = overlaySprites;
        effect.overlayFps           = overlayFps;
        effect.overlayOrderInLayer  = overlayOrderInLayer;
        effect.overlayOffset        = overlayOffset;
        effect.uiSprite             = uiSprite;
        // Önce mevcut AudioSource'u dene, yoksa ekle
        effect.audioSource = player.GetComponent<AudioSource>() 
                            ?? player.AddComponent<AudioSource>();
        effect.shieldBreakClips     = shieldBreakClips;
        effect.selectedClipIndex    = selectedClipIndex;

        Debug.Log($"[HolyMantle] Pickup — overlaySprites:{overlaySprites?.Length}, uiSprite:{uiSprite}");
        effect.ActivateShield();
    }
}