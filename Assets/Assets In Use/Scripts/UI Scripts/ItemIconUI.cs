using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Alınan itemlerin ikonlarını ekranın sol altında 5'erli satırlar halinde gösterir.
/// </summary>
public class ItemIconUI : MonoBehaviour
{
    public static ItemIconUI Instance { get; private set; }

    [Header("Icon Settings")]
    public Vector2 iconSize = new Vector2(32f, 32f);
    public float spacing = 4f;
    public int iconsPerRow = 5;

    [Tooltip("İlk satırın başlangıç pozisyonu (panel içinde).")]
    public Vector2 startPosition = new Vector2(0f, 0f);

    public Sprite iconBackgroundSprite;
    public Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);

    private readonly List<GameObject> _iconObjects = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddItemIcon(Sprite iconSprite, string itemName = "")
    {
        if (iconSprite == null)
        {
            Debug.LogWarning("[ItemIconUI] iconSprite null, ikon eklenmedi.");
            return;
        }

        var container = new GameObject($"ItemIcon_{itemName}");
        container.transform.SetParent(transform, false);

        var containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0f);
        containerRect.anchorMax = new Vector2(0f, 0f);
        containerRect.pivot     = new Vector2(0f, 0f);
        containerRect.sizeDelta = iconSize;

        // Arka plan
        if (iconBackgroundSprite != null || backgroundColor.a > 0f)
        {
            var bg = new GameObject("BG");
            bg.transform.SetParent(container.transform, false);

            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var bgImg = bg.AddComponent<Image>();
            bgImg.sprite = iconBackgroundSprite;
            bgImg.color  = backgroundColor;
        }

        // İkon
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(container.transform, false);

        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        var iconImg = iconGO.AddComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.preserveAspect = true;

        _iconObjects.Add(container);
        LayoutIcons();

        Debug.Log($"[ItemIconUI] İkon eklendi: {itemName} (toplam: {_iconObjects.Count})");
    }

    private void LayoutIcons()
    {
        for (int i = 0; i < _iconObjects.Count; i++)
        {
            int col = i % iconsPerRow;
            int row = i / iconsPerRow;

            float x = startPosition.x + col * (iconSize.x + spacing);
            // Satırlar yukarı doğru büyüsün (row arttıkça Y artar)
            float y = startPosition.y + row * (iconSize.y + spacing);

            var rect = _iconObjects[i].GetComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0f, 0f);
            rect.anchorMax        = new Vector2(0f, 0f);
            rect.pivot            = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta        = iconSize;
        }
    }
}