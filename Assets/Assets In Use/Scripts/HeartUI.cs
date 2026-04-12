using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Canvas üzerindeki kalp slotlarını yönetir.
///
/// Kurulum:
///   1. Hiyerarşide bir Canvas oluştur (Screen Space – Overlay).
///   2. Canvas altına boş bir GameObject ekle, adını "HeartsPanel" yap.
///      Horizontal Layout Group bileşeni ekle; spacing = 4, child force expand = false.
///   3. Bu script'i Canvas'taki herhangi bir GameObject'e (veya Canvas'ın kendisine) ekle.
///   4. heartsPanel alanına HeartsPanel'i sürükle.
///   5. heartSlotPrefab alanına içinde Image bileşeni olan bir prefab sürükle
///      (boyutu 32x32 px yeterli). Prefab'ın Image src'i boş olabilir; script set eder.
///   6. halfHeartSprite, fullHeartSprite, emptyHeartSprite alanlarını doldur.
///   7. Play'e bas — kalpler otomatik oluşturulur.
/// </summary>
public class HeartUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kalp ikonlarının yerleşeceği panel (Horizontal Layout Group önerilir).")]
    public Transform heartsPanel;

    [Tooltip("İçinde Image bileşeni olan prefab. Her slot için bir tane oluşturulur.")]
    public GameObject heartSlotPrefab;

    [Header("Sprites")]
    public Sprite halfHeartSprite;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("Settings")]
    [Tooltip("maxHearts (half-heart birimi). PlayerHealth.maxHearts ile eşleştir.")]
    public int maxHalfHearts = 6;

    // Her slot bir tam kalp yuvası = 2 half-heart temsil eder.
    // Slot sayısı = ceil(maxHalfHearts / 2)
    private Image[] slotImages;

    private void Start()
    {
        BuildSlots();

        // PlayerHealth'ten mevcut canı çek
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        int current = ph != null ? ph.currentHearts : maxHalfHearts;
        UpdateHearts(current);
    }

    private void BuildSlots()
    {
        // Önceki slotları temizle (editor'da tekrar oynatıldığında)
        foreach (Transform child in heartsPanel)
            Destroy(child.gameObject);

        int slotCount = Mathf.CeilToInt(maxHalfHearts / 2f);
        slotImages = new Image[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            GameObject go = Instantiate(heartSlotPrefab, heartsPanel);
            go.name = $"HeartSlot_{i}";
            slotImages[i] = go.GetComponent<Image>();
        }
    }

    /// <summary>
    /// currentHalfHearts değerine göre kalp ikonlarını günceller.
    /// 0 = tüm kalpler boş, maxHalfHearts = tüm kalpler tam.
    /// </summary>
    public void UpdateHearts(int currentHalfHearts)
    {
        if (slotImages == null) return;

        for (int i = 0; i < slotImages.Length; i++)
        {
            if (slotImages[i] == null) continue;

            // Bu slot kaçıncı half-heart çiftini temsil ediyor?
            int slotFirstHalf  = i * 2;       // sol yarı (0-based)
            int slotSecondHalf = i * 2 + 1;   // sağ yarı

            bool leftFilled  = currentHalfHearts > slotFirstHalf;
            bool rightFilled = currentHalfHearts > slotSecondHalf;

            if (leftFilled && rightFilled)
                slotImages[i].sprite = fullHeartSprite;
            else if (leftFilled)
                slotImages[i].sprite = halfHeartSprite;
            else
                slotImages[i].sprite = emptyHeartSprite;
        }
    }
}