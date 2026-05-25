using System.Collections;
using UnityEngine;

public class HolyMantleEffect : MonoBehaviour
{
    // ── HolyMantle tarafından doldurulur ──────────────────────────────────
    [HideInInspector] public Sprite[]     overlaySprites;
    [HideInInspector] public float        overlayFps = 8f;
    [HideInInspector] public int          overlayOrderInLayer = 10;
    [HideInInspector] public Vector2      overlayOffset = new Vector2(0.15f, -0.1f);
    [HideInInspector] public Sprite       uiSprite;
    [HideInInspector] public AudioSource  audioSource;
    [HideInInspector] public AudioClip[]  shieldBreakClips;
    [HideInInspector] public int          selectedClipIndex = 0;

    public bool ShieldActive { get; private set; } = false;

    private bool           _initialized = false;
    private GameObject     _overlayGO;
    private SpriteRenderer _overlaySR;
    private HolyMantleUI   _uiInstance;

    // ── Animasyon ──────────────────────────────────────────────────────────
    private float _animTimer;
    private int   _animFrame;

    private void OnDestroy()
    {
        if (_initialized)
            RoomTransitionManager.OnRoomChanged -= OnRoomChanged;
    }

    private void Update()
    {
        if (!ShieldActive) return;
        if (_overlayGO == null || !_overlayGO.activeSelf) return;
        if (overlaySprites == null || overlaySprites.Length == 0) return;

        _animTimer += Time.deltaTime;
        if (_animTimer >= 1f / overlayFps)
        {
            _animTimer = 0f;
            _animFrame = (_animFrame + 1) % overlaySprites.Length;
            _overlaySR.sprite = overlaySprites[_animFrame];
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────
    public void ActivateShield()
    {
        Debug.Log("[HolyMantleEffect] ActivateShield.");

        if (!_initialized)
        {
            RoomTransitionManager.OnRoomChanged += OnRoomChanged;
            _initialized = true;
        }

        ShieldActive = true;
        _animFrame   = 0;
        _animTimer   = 0f;
        ShowOverlay();
        StartCoroutine(ActivateUIDelayed());
    }

    public bool TryBlockDamage()
    {
        if (!_initialized) return false;
        if (!ShieldActive)  return false;
        BreakShield();
        return true;
    }

    // ── Kalkan kırılma ─────────────────────────────────────────────────────
    private void BreakShield()
    {
        Debug.Log("[HolyMantleEffect] BreakShield!");
        ShieldActive = false;
        HideOverlay();
        EnsureUI();
        _uiInstance?.SetVisible(false);
        PlayBreakSound();
    }

    // ── Overlay ────────────────────────────────────────────────────────────
    private void ShowOverlay()
    {
        if (overlaySprites == null || overlaySprites.Length == 0)
        {
            Debug.LogWarning("[HolyMantleEffect] overlaySprites boş — HolyMantle prefabında 'Overlay Sprites' dizisini doldurun.");
            return;
        }

        if (_overlayGO == null)
        {
            _overlayGO = new GameObject("HolyMantleOverlay");
            _overlayGO.transform.SetParent(transform, false);
            _overlaySR = _overlayGO.AddComponent<SpriteRenderer>();
            _overlaySR.sortingLayerName = "Default";
            _overlaySR.sortingOrder     = overlayOrderInLayer;
        }

        _overlayGO.transform.localPosition = new Vector3(overlayOffset.x, overlayOffset.y, 0f);
        _overlaySR.sprite = overlaySprites[_animFrame];
        _overlayGO.SetActive(true);
        Debug.Log("[HolyMantleEffect] Overlay aktif.");
    }

    private void HideOverlay()
    {
        if (_overlayGO != null)
            _overlayGO.SetActive(false);
    }

    // ── Ses ────────────────────────────────────────────────────────────────
    private void PlayBreakSound()
    {
        if (shieldBreakClips == null || shieldBreakClips.Length == 0) return;
        if (selectedClipIndex < 0 || selectedClipIndex >= shieldBreakClips.Length) return;
        if (shieldBreakClips[selectedClipIndex] == null) return;

        // audioSource yoksa geçici bir kaynak oluştur
        if (audioSource == null)
            audioSource = gameObject.GetComponent<AudioSource>() 
                        ?? gameObject.AddComponent<AudioSource>();

        audioSource.PlayOneShot(shieldBreakClips[selectedClipIndex]);
        Debug.Log($"[HolyMantleEffect] Break sesi çalındı: {shieldBreakClips[selectedClipIndex].name}");
    }

    // ── Oda geçişi ─────────────────────────────────────────────────────────
    private void OnRoomChanged()
    {
        Debug.Log("[HolyMantleEffect] Oda değişti → kalkan yenileniyor.");
        ActivateShield();
    }

    // ── UI ─────────────────────────────────────────────────────────────────
    private void EnsureUI()
    {
        if (_uiInstance != null) return;
        _uiInstance = Object.FindFirstObjectByType<HolyMantleUI>(FindObjectsInactive.Include);
    }

    private IEnumerator ActivateUIDelayed()
    {
        yield return null;
        EnsureUI();
        if (_uiInstance != null)
        {
            _uiInstance.SetVisible(true, uiSprite);
            Debug.Log("[HolyMantleEffect] UI gösterildi.");
        }
        else
        {
            Debug.LogError("[HolyMantleEffect] HolyMantleUI bulunamadı! " +
                           "HolyMantleIcon objesine HolyMantleUI scriptini ekleyin.");
        }
    }
}