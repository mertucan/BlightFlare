using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    private const string ControllerName = "BossHealthBarUI";
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [Header("Layout")]
    [SerializeField] private Vector2 size = new Vector2(560f, 34f);
    [SerializeField] private Vector2 anchoredPosition = new Vector2(0f, -32f);
    [SerializeField] private float borderThickness = 4f;
    [SerializeField] private int cornerRadius = 10;

    [Header("Colors")]
    [SerializeField] private Color backgroundColor = new Color(0.05f, 0.04f, 0.04f, 0.86f);
    [SerializeField] private Color damageColor = new Color(0.28f, 0.02f, 0.02f, 0.95f);
    [SerializeField] private Color fillColor = new Color(0.78f, 0.04f, 0.04f, 0.96f);
    [SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 0.92f);

    [Header("Animation")]
    [SerializeField] private float scanInterval = 0.2f;
    [SerializeField] private float trailingSpeed = 3f;

    private readonly Dictionary<Component, float> maxHealthByBoss = new();

    private RectTransform root;
    private RectTransform fillRect;
    private RectTransform trailingRect;
    private Image fillImage;
    private Image trailingImage;
    private Sprite roundedSprite;
    private Component currentBoss;
    private float scanTimer;
    private float visibleFill;
    private float trailingFill;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<BossHealthBarUI>() != null) return;

        GameObject go = new GameObject(ControllerName);
        go.AddComponent<BossHealthBarUI>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        CreateUI();
        Hide();
    }

    private void Update()
    {
        scanTimer -= Time.unscaledDeltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = scanInterval;
            currentBoss = FindActiveBoss();
        }

        if (currentBoss == null || !TryGetHealth(currentBoss, out float current, out float max) || max <= 0f)
        {
            Hide();
            return;
        }

        float targetFill = Mathf.Clamp01(current / max);
        visibleFill = targetFill;
        trailingFill = Mathf.MoveTowards(trailingFill, targetFill, trailingSpeed * Time.unscaledDeltaTime);

        Show();
        SetBarFill(fillRect, visibleFill);
        SetBarFill(trailingRect, Mathf.Max(trailingFill, visibleFill));
    }

    private Component FindActiveBoss()
    {
        Component boss =
            FindActiveBossOfType<NightWatchAI>() ??
            FindActiveBossOfType<HeartAI>() ??
            FindActiveBossOfType<LokiAI>() ??
            FindActiveBossOfType<DOF_AI>() ??
            FindActiveBossOfType<HollowHead>();

        if (boss != currentBoss)
        {
            visibleFill = 1f;
            trailingFill = 1f;
        }

        return boss;
    }

    private Component FindActiveBossOfType<T>() where T : Component
    {
        T[] bosses = FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var boss in bosses)
        {
            if (boss == null) continue;
            if (!ReadBool(boss, "isActivated")) continue;
            if (ReadBool(boss, "isDead")) continue;
            if (!TryGetHealth(boss, out float current, out float max)) continue;
            if (current <= 0f || max <= 0f) continue;

            return boss;
        }

        return null;
    }

    private bool TryGetHealth(Component boss, out float current, out float max)
    {
        current = 0f;
        max = 0f;

        if (TryReadNumber(boss, "maxHP", out max) && TryReadNumber(boss, "currentHP", out current))
            return true;

        if (TryReadNumber(boss, "bombsToKill", out max) && TryReadNumber(boss, "currentBombHits", out float hits))
        {
            current = Mathf.Max(0f, max - hits);
            return true;
        }

        if (boss is HollowHead && TryReadListCount(boss, "segments", out int segmentCount))
        {
            if (!maxHealthByBoss.TryGetValue(boss, out max))
            {
                max = Mathf.Max(1, segmentCount + 1);
                maxHealthByBoss[boss] = max;
            }

            current = Mathf.Clamp(segmentCount + 1, 0f, max);
            return true;
        }

        return false;
    }

    private bool TryReadNumber(Component target, string fieldName, out float value)
    {
        value = 0f;
        FieldInfo field = target.GetType().GetField(fieldName, Flags);
        if (field == null) return false;

        object raw = field.GetValue(target);
        if (raw is int i)
        {
            value = i;
            return true;
        }

        if (raw is float f)
        {
            value = f;
            return true;
        }

        return false;
    }

    private bool TryReadListCount(Component target, string fieldName, out int count)
    {
        count = 0;
        FieldInfo field = target.GetType().GetField(fieldName, Flags);
        if (field == null) return false;

        object raw = field.GetValue(target);
        if (raw is System.Collections.ICollection collection)
        {
            count = collection.Count;
            return true;
        }

        return false;
    }

    private bool ReadBool(Component target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, Flags);
        return field != null && field.GetValue(target) is bool value && value;
    }

    private void CreateUI()
    {
        GameObject canvasGO = new GameObject("BossHealthCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9000;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        roundedSprite = CreateRoundedSprite(32, cornerRadius);

        root = CreateImage(canvasGO.transform, "BossHealthRoot", borderColor).rectTransform;
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = anchoredPosition;
        root.sizeDelta = size;

        RectTransform background = CreateChildImage(root, "Background", backgroundColor, new Vector2(borderThickness, borderThickness)).rectTransform;
        trailingImage = CreateChildImage(background, "DamageTrail", damageColor, Vector2.zero);
        fillImage = CreateChildImage(background, "Fill", fillColor, Vector2.zero);
        trailingRect = trailingImage.rectTransform;
        fillRect = fillImage.rectTransform;

        SetBarFill(trailingRect, 1f);
        SetBarFill(fillRect, 1f);
    }

    private Image CreateChildImage(RectTransform parent, string name, Color color, Vector2 inset)
    {
        Image image = CreateImage(parent, name, color);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = inset;
        rect.offsetMax = -inset;
        return image;
    }

    private Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void SetBarFill(RectTransform rect, float amount)
    {
        if (rect == null) return;

        amount = Mathf.Clamp01(amount);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(amount, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.gameObject.SetActive(amount > 0.001f);
    }

    private Sprite CreateRoundedSprite(int textureSize, int radius)
    {
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;
        float max = textureSize - 1;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float cx = Mathf.Clamp(x, radius, max - radius);
                float cy = Mathf.Clamp(y, radius, max - radius);
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                texture.SetPixel(x, y, dist <= radius ? white : clear);
            }
        }

        texture.Apply();

        Rect rect = new Rect(0f, 0f, textureSize, textureSize);
        Vector4 border = Vector4.one * radius;
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), textureSize, 0, SpriteMeshType.FullRect, border);
    }

    private void Show()
    {
        if (root != null && !root.gameObject.activeSelf)
            root.gameObject.SetActive(true);
    }

    private void Hide()
    {
        if (root != null && root.gameObject.activeSelf)
            root.gameObject.SetActive(false);
    }
}
