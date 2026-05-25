using UnityEngine;
using UnityEngine.UI;

public class HolyMantleUI : MonoBehaviour
{
    [Header("Holy Mantle Icon")]
    [Tooltip("Kalkan ikonunu gösteren Image bileşeni.")]
    public Image shieldIcon;

    private void Awake()
    {
        // Başlangıçta gizle
        if (shieldIcon != null)
            shieldIcon.gameObject.SetActive(false);
    }

    /// <summary>UI ikonunu göster/gizle. Gösterirken sprite'ı da günceller.</summary>
    public void SetVisible(bool visible, Sprite sprite = null)
    {
        if (shieldIcon == null)
        {
            Debug.LogError("[HolyMantleUI] shieldIcon bağlı değil!");
            return;
        }

        if (visible && sprite != null)
            shieldIcon.sprite = sprite;

        shieldIcon.gameObject.SetActive(visible);
        Debug.Log($"[HolyMantleUI] SetVisible({visible})");
    }
}