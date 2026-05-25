using UnityEngine;

public class CeremonialRobe : MonoBehaviour, IItem
{
    [Header("Heal Amount")]
    public int healHalfHearts = 4;
    [Header("UI Icon")]
    public Sprite iconSprite;

    [Header("Character Overlay — Aşağı (Order: 7)")]
    public Sprite[] overlaySpritesDown;
    public Vector2  overlayOffsetDown  = new Vector2(0f, 0.6f);

    [Header("Character Overlay — Yukarı (Order: 7)")]
    public Sprite[] overlaySpritesUp;
    public Vector2  overlayOffsetUp    = new Vector2(0f, 0.6f);

    [Header("Character Overlay — Sol (Order: 7)")]
    public Sprite[] overlaySpritesLeft;
    public Vector2  overlayOffsetLeft  = new Vector2(0f, 0.6f);

    [Header("Character Overlay — Sağ (Order: 7)")]
    public Sprite[] overlaySpritesRight;
    public Vector2  overlayOffsetRight = new Vector2(0f, 0.6f);

    [Header("Overlay Settings")]
    public float overlayFps = 8f;

    [Header("Glow Sprite (Order: 3)")]
    public Sprite  glowSprite;
    public Vector2 glowOffset = new Vector2(0f, 0f);

    public void Pickup(GameObject player)
    {
        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.maxHearts += healHalfHearts;
            health.Heal(healHalfHearts);
        }
        else
            Debug.LogWarning("[CeremonialRobe] PlayerHealth bulunamadı!");

        HeartUI heartUI = Object.FindFirstObjectByType<HeartUI>();
        if (heartUI != null)
        {
            heartUI.maxHalfHearts += healHalfHearts;
            heartUI.RebuildSlots();
        }

        // effect burada tanımlanıyor
        var effect = player.GetComponent<CeremonialRobeEffect>()
                  ?? player.AddComponent<CeremonialRobeEffect>();

        effect.overlaySpritesDown  = overlaySpritesDown;
        effect.overlaySpritesUp    = overlaySpritesUp;
        effect.overlaySpritesLeft  = overlaySpritesLeft;
        effect.overlaySpritesRight = overlaySpritesRight;
        effect.overlayOffsetDown   = overlayOffsetDown;
        effect.overlayOffsetUp     = overlayOffsetUp;
        effect.overlayOffsetLeft   = overlayOffsetLeft;
        effect.overlayOffsetRight  = overlayOffsetRight;
        effect.overlayFps          = overlayFps;
        effect.glowSprite          = glowSprite;
        effect.glowOffset          = glowOffset;

        effect.Activate();
        ItemIconUI.Instance?.AddItemIcon(iconSprite, "CeremonialRobe");
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Pickup(other.gameObject);
    }
}