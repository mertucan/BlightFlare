using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class SecretRoomPhoneStation : MonoBehaviour
{
    private static Font cachedUiFont;

    [System.Serializable]
    private class PhoneItemEntry
    {
        public string itemName;
        public Sprite icon;
        [TextArea(2, 5)] public string description;
    }

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Key interactKey = Key.E;
    [SerializeField] private float interactionDistance = 2.4f;

    [Header("Catalog")]
    [SerializeField] private List<PhoneItemEntry> itemEntries = new();
    [SerializeField] private Vector2 panelSize = new Vector2(1120f, 720f);
    [SerializeField] private int iconsPerRow = 5;

    private bool playerNearby;
    private Canvas canvas;
    private GameObject panel;
    private Text titleText;
    private Text detailsText;
    private Transform iconGrid;
    private PhoneItemEntry selectedEntry;
    private Transform playerTransform;

    private void Awake()
    {
        ConfigureTrigger();
    }

    private void Update()
    {
        playerNearby = IsPlayerInInteractionRange();
        if (!playerNearby)
        {
            if (panel != null)
                panel.SetActive(false);
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[interactKey].wasPressedThisFrame)
            TogglePanel();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPlayer(other))
        {
            playerNearby = true;
            playerTransform = ResolvePlayerTransform(other);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsPlayer(other)) return;

        playerNearby = false;
        playerTransform = null;
        if (panel != null)
            panel.SetActive(false);
    }

    private void ConfigureTrigger()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;
    }

    private bool IsPlayerInInteractionRange()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null)
            return false;

        float sqrDistance = ((Vector2)playerTransform.position - (Vector2)transform.position).sqrMagnitude;
        return sqrDistance <= interactionDistance * interactionDistance;
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other.CompareTag(playerTag)) return true;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag)) return true;

        Transform current = other.transform.parent;
        while (current != null)
        {
            if (current.CompareTag(playerTag)) return true;
            current = current.parent;
        }

        return false;
    }

    private Transform ResolvePlayerTransform(Collider2D other)
    {
        if (other.CompareTag(playerTag)) return other.transform;
        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
            return other.attachedRigidbody.transform;

        Transform current = other.transform.parent;
        while (current != null)
        {
            if (current.CompareTag(playerTag)) return current;
            current = current.parent;
        }

        return null;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        DontDestroyOnLoad(eventSystem);
    }

    private void BuildPanel()
    {
        EnsureEventSystem();

        if (itemEntries == null || itemEntries.Count == 0)
            itemEntries = BuildFallbackEntries();

        GameObject canvasGO = new GameObject("PhoneStationCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9500;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        panel = new GameObject("PhoneStationOverlay", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);

        RectTransform overlayRect = panel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = panel.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.56f);
        overlayImage.raycastTarget = false;

        GameObject catalogPanel = new GameObject("PhoneStationPanel", typeof(RectTransform), typeof(Image));
        catalogPanel.transform.SetParent(panel.transform, false);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        RectTransform catalogRect = catalogPanel.GetComponent<RectTransform>();
        Vector2 effectivePanelSize = new Vector2(Mathf.Max(panelSize.x, 1120f), Mathf.Max(panelSize.y, 720f));
        catalogRect.anchorMin = new Vector2(0.5f, 0.5f);
        catalogRect.anchorMax = new Vector2(0.5f, 0.5f);
        catalogRect.pivot = new Vector2(0.5f, 0.5f);
        catalogRect.sizeDelta = effectivePanelSize;

        Image panelImage = catalogPanel.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.032f, 0.04f, 0.96f);

        panelRect = catalogRect;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        titleText = CreateText(catalogPanel.transform, "Title", 36, TextAnchor.MiddleLeft);
        titleText.fontStyle = FontStyle.Bold;
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(48f, -88f);
        titleRect.offsetMax = new Vector2(-48f, -24f);
        titleText.text = "Item Catalog";

        CreateIconGrid(catalogPanel.transform);
        CreateDetailsPanel(catalogPanel.transform);
        PopulateIcons();

        if (itemEntries.Count > 0)
            ShowEntry(itemEntries[0]);

        panel.SetActive(false);
    }

    private void CreateIconGrid(Transform parent)
    {
        GameObject gridGO = new GameObject("ItemIconGrid", typeof(RectTransform));
        gridGO.transform.SetParent(parent, false);
        iconGrid = gridGO.transform;

        RectTransform gridRect = gridGO.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0f, 0f);
        gridRect.anchorMax = new Vector2(0f, 1f);
        gridRect.pivot = new Vector2(0f, 0.5f);
        gridRect.offsetMin = new Vector2(48f, 48f);
        gridRect.offsetMax = new Vector2(650f, -112f);
    }

    private void CreateDetailsPanel(Transform parent)
    {
        GameObject detailBG = new GameObject("ItemDetails", typeof(RectTransform), typeof(Image));
        detailBG.transform.SetParent(parent, false);

        RectTransform detailRect = detailBG.GetComponent<RectTransform>();
        detailRect.anchorMin = new Vector2(1f, 0f);
        detailRect.anchorMax = new Vector2(1f, 1f);
        detailRect.pivot = new Vector2(1f, 0.5f);
        detailRect.offsetMin = new Vector2(-430f, 48f);
        detailRect.offsetMax = new Vector2(-48f, -112f);

        Image bg = detailBG.GetComponent<Image>();
        bg.color = new Color(0.055f, 0.052f, 0.06f, 0.98f);

        detailsText = CreateText(detailBG.transform, "Description", 30, TextAnchor.UpperLeft);
        detailsText.lineSpacing = 1.08f;
        RectTransform textRect = detailsText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(30f, 28f);
        textRect.offsetMax = new Vector2(-30f, -28f);
    }

    private void PopulateIcons()
    {
        const float iconSize = 86f;
        const float spacing = 18f;

        for (int i = 0; i < itemEntries.Count; i++)
        {
            PhoneItemEntry entry = itemEntries[i];
            int col = i % Mathf.Max(1, iconsPerRow);
            int row = i / Mathf.Max(1, iconsPerRow);

            GameObject buttonGO = new GameObject($"Item_{entry.itemName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
            buttonGO.transform.SetParent(iconGrid, false);

            RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.sizeDelta = new Vector2(iconSize, iconSize);
            buttonRect.anchoredPosition = new Vector2(col * (iconSize + spacing), -row * (iconSize + spacing));

            Image frame = buttonGO.GetComponent<Image>();
            frame.color = new Color(0.19f, 0.175f, 0.19f, 1f);

            Button button = buttonGO.GetComponent<Button>();
            PhoneItemEntry captured = entry;
            button.onClick.AddListener(() => ShowEntry(captured));

            EventTrigger trigger = buttonGO.GetComponent<EventTrigger>();
            EventTrigger.Entry hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            hover.callback.AddListener(_ => ShowEntry(captured));
            trigger.triggers.Add(hover);

            if (entry.icon != null)
                CreateIconImage(buttonGO.transform, entry.icon);
            else
                CreateFallbackLabel(buttonGO.transform, entry.itemName);
        }
    }

    private void CreateIconImage(Transform parent, Sprite sprite)
    {
        GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(parent, false);

        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.1f, 0.1f);
        iconRect.anchorMax = new Vector2(0.9f, 0.9f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        Image icon = iconGO.GetComponent<Image>();
        icon.sprite = sprite;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private void CreateFallbackLabel(Transform parent, string itemName)
    {
        Text label = CreateText(parent, "FallbackLabel", 18, TextAnchor.MiddleCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        label.text = string.IsNullOrWhiteSpace(itemName) ? "?" : itemName.Substring(0, 1);
    }

    private Text CreateText(Transform parent, string name, int fontSize, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.font = GetUiFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.98f, 0.98f, 0.94f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private Font GetUiFont()
    {
        if (cachedUiFont != null)
            return cachedUiFont;

        cachedUiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return cachedUiFont;
    }

    private void ShowEntry(PhoneItemEntry entry)
    {
        selectedEntry = entry;
        if (detailsText == null || selectedEntry == null) return;

        detailsText.text = $"{selectedEntry.itemName}\n\n{selectedEntry.description}";
    }

    private void TogglePanel()
    {
        if (panel == null)
            BuildPanel();

        panel.SetActive(!panel.activeSelf);

        if (panel.activeSelf && selectedEntry == null && itemEntries.Count > 0)
            ShowEntry(itemEntries[0]);
    }

    private List<PhoneItemEntry> BuildFallbackEntries()
    {
        return new List<PhoneItemEntry>
        {
            Entry("Blood Bag", "Canini riske atarak hiz kazandirir ve hayatta kalma oynanisina daha agresif bir tempo ekler."),
            Entry("Blood of the Martyr", "Hasar potansiyelini arttiran saldiri odakli bir itemdir."),
            Entry("Bobby Bomb", "Bombalarini daha akilli hale getirir; hedef arama davranisi kazanir."),
            Entry("Bob's Curse", "Bombalarini zehirli hale getirir ve patlama sonrasi zehir bulutu birakabilir."),
            Entry("Ceremonial Robe", "Karakteri daha karanlik bir forma tasiyan guclendirici bir pasif etkidir."),
            Entry("Holy Mantle", "Odaya girince ilk hasari engelleyen koruyucu kalkan verir."),
            Entry("Hot Bombs", "Bombalar patladiktan sonra yerde alev tehlikesi birakabilir."),
            Entry("Leo", "Kayalari ve engelleri kirmaya yarayan agir bir beden etkisi verir."),
            Entry("Pyromaniac", "Patlamalarla etkilesimi avantajli hale getirir; bomba odakli oynanisi guclendirir."),
            Entry("Seraphim", "Karaktere melek temali gorunum ve guclendirici etki kazandirir."),
            Entry("Taurus", "Dusmanli odalarda zamanla hizini arttirir; hiz tavanina ulasinca cok daha tehlikeli olur."),
            Entry("Blast Radius", "Bomba patlama menzilini arttirir."),
            Entry("Extra Bomb", "Bomba kapasiteni arttirir."),
            Entry("Speed Increase", "Karakter hizini arttirir.")
        };
    }

    private PhoneItemEntry Entry(string itemName, string description)
    {
        return new PhoneItemEntry
        {
            itemName = itemName,
            description = description
        };
    }
}
