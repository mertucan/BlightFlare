using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeartUI : MonoBehaviour
{
    // ─────────────────────────────────────────
    // HEARTS
    // ─────────────────────────────────────────
    [Header("Hearts – Panel")]
    [Tooltip("Kalp ikonlarının yerleşeceği panel (Horizontal Layout Group önerilir).")]
    public Transform heartsPanel;

    [Tooltip("İçinde Image bileşeni olan prefab. Her slot için bir tane oluşturulur.")]
    public GameObject heartSlotPrefab;

    [Header("Hearts – Sprites")]
    public Sprite halfHeartSprite;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("Hearts – Settings")]
    [Tooltip("Maksimum half-heart birimi. PlayerHealth.maxHearts ile eşleştir.")]
    public int maxHalfHearts = 6;

    private Image[] _slotImages;

    // ─────────────────────────────────────────
    // KEY
    // ─────────────────────────────────────────
    [Header("Key")]
    [Tooltip("Anahtar sprite'ını gösteren Image bileşeni.")]
    public Image keyIcon;

    [Tooltip("Anahtar sayısını gösteren TMP (başlangıçta '0').")]
    public TMP_Text keyText;

    // ─────────────────────────────────────────
    // PENNIES
    // ─────────────────────────────────────────
    [Header("Pennies")]
    [Tooltip("Para sprite'ını gösteren Image bileşeni.")]
    public Image penniesIcon;

    [Tooltip("Para miktarını gösteren TMP (başlangıçta '0').")]
    public TMP_Text penniesText;

    // ═════════════════════════════════════════
    // UNITY
    // ═════════════════════════════════════════
    private void Start()
    {
        BuildHeartSlots();

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        int currentHearts = ph != null ? ph.currentHearts : maxHalfHearts;
        UpdateHearts(currentHearts);

        // Başlangıç değerleri
        UpdateKey(0);
        UpdatePennies(0);
    }

    // ═════════════════════════════════════════
    // PUBLIC API
    // ═════════════════════════════════════════

    /// <summary>
    /// currentHalfHearts değerine göre kalp ikonlarını günceller.
    /// 0 = tüm kalpler boş, maxHalfHearts = tüm kalpler tam.
    /// </summary>
    public void UpdateHearts(int currentHalfHearts)
    {
        if (_slotImages == null) return;

        for (int i = 0; i < _slotImages.Length; i++)
        {
            if (_slotImages[i] == null) continue;

            int slotFirstHalf  = i * 2;
            int slotSecondHalf = i * 2 + 1;

            bool leftFilled  = currentHalfHearts > slotFirstHalf;
            bool rightFilled = currentHalfHearts > slotSecondHalf;

            if (leftFilled && rightFilled)
                _slotImages[i].sprite = fullHeartSprite;
            else if (leftFilled)
                _slotImages[i].sprite = halfHeartSprite;
            else
                _slotImages[i].sprite = emptyHeartSprite;
        }
    }

    /// <summary>
    /// Ekrandaki anahtar sayısını günceller. Sprite sabit, sadece sayı değişir.
    /// </summary>
    public void UpdateKey(int count)
    {
        if (keyText != null)
            keyText.text = count.ToString();
    }

    /// <summary>
    /// Ekrandaki penny miktarını günceller. Sprite sabit, sadece sayı değişir.
    /// </summary>
    public void UpdatePennies(int amount)
    {
        if (penniesText != null)
            penniesText.text = amount.ToString();
    }

    // ═════════════════════════════════════════
    // PRIVATE
    // ═════════════════════════════════════════
    private void BuildHeartSlots()
    {
        if (heartsPanel == null) return;

        foreach (Transform child in heartsPanel)
            Destroy(child.gameObject);

        int slotCount = Mathf.CeilToInt(maxHalfHearts / 2f);
        _slotImages = new Image[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            GameObject go = Instantiate(heartSlotPrefab, heartsPanel);
            go.name = $"HeartSlot_{i}";
            _slotImages[i] = go.GetComponent<Image>();
        }
    }
}