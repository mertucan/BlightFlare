using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HeartUI : MonoBehaviour
{
    [Header("Hearts – Panel")]
    public Transform heartsPanel;
    public GameObject heartSlotPrefab;

    [Header("Hearts – Sprites")]
    public Sprite halfHeartSprite;
    public Sprite fullHeartSprite;
    public Sprite emptyHeartSprite;

    [Header("Hearts – Settings")]
    public int maxHalfHearts = 6;

    private Image[] _slotImages;

    [Header("Key")]
    public Image keyIcon;
    public TMP_Text keyText;

    [Header("Pennies")]
    public Image penniesIcon;
    public TMP_Text penniesText;

    [Header("Holy Mantle UI")]
    public float holyMantleOffsetX = 10f;
    [Tooltip("Bir satırda kaç kalp slotu olacak.")]
    public int heartsPerRow = 5;
    [Tooltip("2. satırın 1. satıra göre Y offset'i (negatif = aşağı).")]
    public float secondRowOffsetY = -90f;
    private float _holyMantleBaseY;
    private bool  _holyMantleBaseYCaptured = false;
    [Tooltip("HolyMantle ikonunun 2. satırda ne kadar aşağı ineceği.")]
    public float holyMantleRowOffsetY = -45f;

    private IEnumerator Start()
    {
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            maxHalfHearts = ph.maxHearts;
        }

        BuildHeartSlots();
        yield return null;

        ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            maxHalfHearts = ph.maxHearts;
        }

        if (ph != null)
            UpdateHearts(ph.currentHearts);

        if (PlayerInventory.instance != null)
        {
            UpdateKey(PlayerInventory.instance.keys);
            UpdatePennies(PlayerInventory.instance.pennies);
        }
        else
        {
            UpdateKey(0);
            UpdatePennies(0);
        }
    }

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

    public void UpdateKey(int count)
    {
        if (keyText != null) keyText.text = count.ToString();
    }

    public void UpdatePennies(int amount)
    {
        if (penniesText != null) penniesText.text = amount.ToString();
    }

    public void RebuildSlots()
    {
        BuildHeartSlots();

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null) UpdateHearts(ph.currentHearts);
    }

    /// <summary>
    /// HolyMantleUI ikonunu bir frame sonra doğru konuma taşır.
    /// BloodBag ve HolyMantle bu metodu çağırır.
    /// </summary>
    public void RepositionHolyMantleAfterFrame()
    {
        StartCoroutine(RepositionNextFrame());
    }

    private IEnumerator RepositionNextFrame()
    {
        yield return null;
        yield return null;

        HolyMantleUI mantleUI = Object.FindFirstObjectByType<HolyMantleUI>();
        if (mantleUI == null) yield break;

        RectTransform mantleRT = mantleUI.GetComponent<RectTransform>();
        if (mantleRT == null) yield break;

        RectTransform panelRT = heartsPanel as RectTransform;
        if (panelRT == null) yield break;

        int childCount = heartsPanel.childCount;
        if (childCount == 0) yield break;

        RectTransform lastSlotRT = heartsPanel.GetChild(childCount - 1)
                                            .GetComponent<RectTransform>();
        if (lastSlotRT == null) yield break;

        Canvas rootCanvas = mantleRT.GetComponentInParent<Canvas>().rootCanvas;
        float canvasScale = rootCanvas.transform.localScale.x;

        // Son slotun sağ-üst köşesi (screen pixel)
        Vector3[] corners = new Vector3[4];
        lastSlotRT.GetWorldCorners(corners);
        float screenRightX = corners[2].x; // sağ-üst
        float screenTopY   = corners[1].y; // sol-üst (aynı Y seviyesi)

        float targetX = (screenRightX / canvasScale) + holyMantleOffsetX;

        // Y: son slotun hangi satırda olduğunu bul
        int lastIndex = childCount - 1;
        int row = lastIndex / heartsPerRow;
        // HolyMantleIcon'un başlangıç Y'si (sadece 1 satır varken doğru olan değer)
        // row 0 ise Y değişmez, row 1+ ise secondRowOffsetY kadar aşağı in
        // Başlangıç Y'sini ilk kez kaydet
        if (!_holyMantleBaseYCaptured)
        {
            _holyMantleBaseY = mantleRT.anchoredPosition.y;
            _holyMantleBaseYCaptured = true;
        }

        float targetY = _holyMantleBaseY + (row * holyMantleRowOffsetY);

        Vector2 pos = mantleRT.anchoredPosition;
        pos.x = targetX;
        pos.y = targetY;
        mantleRT.anchoredPosition = pos;

        Debug.Log($"[HeartUI] row:{row} targetX:{targetX} targetY:{targetY} baseY:{_holyMantleBaseY}");
    }

    private void BuildHeartSlots()
    {
        if (heartsPanel == null) return;

        foreach (Transform child in heartsPanel)
            Destroy(child.gameObject);

        // HorizontalLayoutGroup varsa kapat — manual layout yapıyoruz
        var hlg = heartsPanel.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) hlg.enabled = false;

        int slotCount = Mathf.CeilToInt(maxHalfHearts / 2f);
        _slotImages = new Image[slotCount];

        // Slot boyutunu prefab'dan oku
        RectTransform prefabRT = heartSlotPrefab.GetComponent<RectTransform>();
        float slotW   = prefabRT != null ? prefabRT.rect.width  : 50f;
        float slotH   = prefabRT != null ? prefabRT.rect.height : 50f;
        float spacingX = slotW + 5f; // yatay boşluk — Inspector'dan ayarlamak istersen field ekle

        for (int i = 0; i < slotCount; i++)
        {
            GameObject go = Instantiate(heartSlotPrefab, heartsPanel);
            go.name = $"HeartSlot_{i}";

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Anchor + pivot: top-left
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot     = new Vector2(0, 1);

                int col = i % heartsPerRow;
                int row = i / heartsPerRow;

                rt.anchoredPosition = new Vector2(
                    col * spacingX,
                    row * secondRowOffsetY
                );
                rt.sizeDelta = new Vector2(slotW, slotH);
            }

            _slotImages[i] = go.GetComponent<Image>();
        }
    }
}
