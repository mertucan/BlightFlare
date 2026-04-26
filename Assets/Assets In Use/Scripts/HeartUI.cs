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
    [Tooltip("Son kalp slotunun sağ kenarından HolyMantle ikonuna piksel boşluk.")]
    public float holyMantleOffsetX = 10f;

    private void Start()
    {
        BuildHeartSlots();

        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        int currentHearts = ph != null ? ph.currentHearts : maxHalfHearts;
        UpdateHearts(currentHearts);

        UpdateKey(0);
        UpdatePennies(0);
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

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRT);

        int childCount = heartsPanel.childCount;
        if (childCount == 0) yield break;

        RectTransform lastSlotRT = heartsPanel.GetChild(childCount - 1)
                                            .GetComponent<RectTransform>();
        if (lastSlotRT == null) yield break;

        Vector3[] corners = new Vector3[4];
        lastSlotRT.GetWorldCorners(corners);
        float screenRightX = corners[2].x;

        // Canvas scale: kaç screen pixel = 1 canvas unit
        Canvas rootCanvas = mantleRT.GetComponentInParent<Canvas>().rootCanvas;
        float canvasScale = rootCanvas.transform.localScale.x;

        // screen pixel → canvas unit (UI objesi top-left anchor'da, anchoredPosition = screen pixel / scale)
        float targetX = (screenRightX / canvasScale) + holyMantleOffsetX;

        Vector2 pos = mantleRT.anchoredPosition;
        pos.x = targetX;
        mantleRT.anchoredPosition = pos;

        Debug.Log($"[HeartUI] screenRightX:{screenRightX} canvasScale:{canvasScale} targetX:{targetX}");
    }

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