using UnityEngine;

/// <summary>
/// Seraphim item'ı — Uçma efekti.
///
/// Efektler:
///  - Pickup anında body sprite'larını yön bazlı değiştirir:
///      · Aşağı : 7 sprite → doğrudan body'nin yerini alır
///      · Yukarı: 1 body sprite + üstüne 7 sprite'lık kanat overlay
///      · Sağ   : mevcut sprite'lar body'nin yerini alır
///      · Sol   : sağ sprite'ların flipX ile aynalanmış hâli
///  - Tag'ı "Destructible" veya "Obstacle" olan collider'ları yoksayar (uçuş)
///  - Tüm animasyonlar 16 FPS'de, idle/hareket fark etmeksizin sürekli çalışır
///
/// KURULUM:
///  Inspector'da tüm sprite dizilerini, offset'leri ve (varsa) kanat overlay offset'ini doldurun.
///  "Obstacle" tag'ını henüz oluşturmadıysanız kod hata vermez; tag eklendiğinde
///  otomatik olarak devreye girer.
/// </summary>
public class Seraphim : MonoBehaviour, IItem
{
    // ── Aşağı yön ────────────────────────────────────────────────────────────
    [Header("Down — Body Sprites (7 adet, body'nin yerini alır)")]
    public Sprite[] bodySpritesDown;

    [Header("Down — Body Offset")]
    public Vector2 bodyOffsetDown = new Vector2(0.32f, 0f);

    // ── Yukarı yön ───────────────────────────────────────────────────────────
    [Header("Up — Body Sprite (body'nin yerini alır)")]
    public Sprite[] bodySpritesUp;

    [Header("Up — Wing Overlay Sprites (7 adet, body'nin üstüne eklenir)")]
    public Sprite[] wingSpritesUp;

    [Header("Up — Wing Overlay Offset (body üstünde konumu)")]
    public Vector2 wingOffsetUp = new Vector2(0.37f, -0.53f);

    // ── Sağ yön ──────────────────────────────────────────────────────────────
    [Header("Right — Body Sprites (body'nin yerini alır; sol için flipX uygulanır)")]
    public Sprite[] bodySpritesRight;

    [Header("Right — Body Offset")]
    public Vector2 bodyOffsetRight = new Vector2(0.16f, 0.09f);

    [Header("Left — Body Offset (sol yön için ayrı offset)")]
    public Vector2 bodyOffsetLeft = new Vector2(0.54f, 0.09f);

    // ── Yukarı body bob ──────────────────────────────────────────────────────
    // Body yukarı yönde başlangıçta aşağı kayar, sonra yukarı çıkar (0.05 birim)
    [Header("Up — Body Base Y Offset")]
    public float bodyBaseYUp = 0f;
    private const float BOB_AMPLITUDE = 0.05f;
    private const float BOB_SPEED     = 2f;   // radyan/saniye (ayarlanabilir)
    private float _bobPhase = 0f;

    // ── Animasyon ────────────────────────────────────────────────────────────
    private const float FPS = 4f;

    // ── Sorting ──────────────────────────────────────────────────────────────
    private const int WING_OVERLAY_ORDER = 7;

    // ── Çalışma zamanı ───────────────────────────────────────────────────────
    private GameObject     _wingGO;
    private SpriteRenderer _wingSR;
    private float          _wingTimer;
    private int            _wingFrame;

    // Body SpriteRenderer'ları — animasyonu kendimiz yönetiyoruz
    private SpriteRenderer _srDown, _srUp, _srRight, _srLeft;
    private float          _timerDown,  _timerUp,  _timerRight,  _timerLeft;
    private int            _frameDown,  _frameUp,  _frameRight,  _frameLeft;

    private IsaacMovement          _movement;
    private AnimatedSpriteRenderer _leftBodyASR;
    private bool                   _active;

    // ─────────────────────────────────────────────────────────────────────────
    //  IItem
    // ─────────────────────────────────────────────────────────────────────────
    public void Pickup(GameObject player)
    {
        var comp = player.GetComponent<Seraphim>();
        if (comp == null) comp = player.AddComponent<Seraphim>();

        comp.bodySpritesDown  = bodySpritesDown;
        comp.bodyOffsetDown   = bodyOffsetDown;
        comp.bodySpritesUp    = bodySpritesUp;
        comp.wingSpritesUp    = wingSpritesUp;
        comp.wingOffsetUp     = wingOffsetUp;
        comp.bodySpritesRight = bodySpritesRight;
        comp.bodyOffsetRight  = bodyOffsetRight;
        comp.bodyOffsetLeft   = bodyOffsetLeft;
        comp.bodyBaseYUp      = bodyBaseYUp;

        comp.Activate(player);
        Debug.Log("[Seraphim] Pickup tamamlandı.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Aktivasyon
    // ─────────────────────────────────────────────────────────────────────────
    private void Activate(GameObject player)
    {
        _movement = player.GetComponent<IsaacMovement>();
        _active   = true;

        ApplyBodySprites();
        ApplyFlight(player);
        EnsureWingOverlay();

        Debug.Log("[Seraphim] Aktive edildi.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Body sprite değiştirme + offset uygulama
    // ─────────────────────────────────────────────────────────────────────────
    private void ApplyBodySprites()
    {
        if (_movement == null) return;

        ReplaceBodySprites(_movement.spritesDown.body,  bodySpritesDown);
        ReplaceBodySprites(_movement.spritesUp.body,    bodySpritesUp);
        ReplaceBodySprites(_movement.spritesRight.body, bodySpritesRight);

        _leftBodyASR = _movement.spritesLeft.body;
        ReplaceBodySprites(_leftBodyASR, bodySpritesRight);
        SetFlipX(_leftBodyASR, true);

        // Offset uygula
        ApplyOffset(_movement.spritesDown.body,  bodyOffsetDown);
        ApplyOffset(_movement.spritesRight.body, bodyOffsetRight);
        ApplyOffset(_leftBodyASR,                bodyOffsetLeft);
        // Yukarı yön offset'i bob sistemi tarafından her frame güncellenir

        // SpriteRenderer referanslarını sakla
        _srDown  = GetSR(_movement.spritesDown.body);
        _srUp    = GetSR(_movement.spritesUp.body);
        _srRight = GetSR(_movement.spritesRight.body);
        _srLeft  = GetSR(_leftBodyASR);

        // Sayaçları sıfırla
        _timerDown = _timerUp = _timerRight = _timerLeft = 0f;
        _frameDown = _frameUp = _frameRight = _frameLeft = 0;

        ForceRefresh(_movement.spritesDown.body);
        ForceRefresh(_movement.spritesUp.body);
        ForceRefresh(_movement.spritesRight.body);
        ForceRefresh(_leftBodyASR);
    }

    private void ApplyOffset(AnimatedSpriteRenderer asr, Vector2 offset)
    {
        if (asr == null) return;
        var t = asr.transform;
        t.localPosition = new Vector3(offset.x, offset.y, t.localPosition.z);
    }

    private SpriteRenderer GetSR(AnimatedSpriteRenderer asr) =>
        asr != null ? asr.GetComponent<SpriteRenderer>() : null;

    private void ReplaceBodySprites(AnimatedSpriteRenderer asr, Sprite[] newSprites)
    {
        if (asr == null || newSprites == null || newSprites.Length == 0) return;
        asr.animationSprites = null;
        asr.animationSprites = newSprites;
    }

    private void SetFlipX(AnimatedSpriteRenderer asr, bool flip)
    {
        var sr = GetSR(asr);
        if (sr != null) sr.flipX = flip;
    }

    private void ForceRefresh(AnimatedSpriteRenderer asr)
    {
        if (asr == null) return;
        asr.enabled = false;
        asr.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Uçuş — collision yoksay
    // ─────────────────────────────────────────────────────────────────────────
    private void ApplyFlight(GameObject player)
    {
        var colliders = player.GetComponents<Collider2D>();
        IgnoreTaggedColliders(colliders, "Destructible");
        TryIgnoreTaggedColliders(colliders, "Obstacle");
    }

    private void IgnoreTaggedColliders(Collider2D[] playerCols, string tag)
    {
        var targets = GameObject.FindGameObjectsWithTag(tag);
        foreach (var t in targets)
            foreach (var tc in t.GetComponents<Collider2D>())
                foreach (var pc in playerCols)
                    Physics2D.IgnoreCollision(pc, tc, true);
    }

    private void TryIgnoreTaggedColliders(Collider2D[] playerCols, string tag)
    {
        try { IgnoreTaggedColliders(playerCols, tag); }
        catch { /* tag henüz tanımlı değil — sorun yok */ }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Kanat overlay oluşturma (yukarı yön)
    // ─────────────────────────────────────────────────────────────────────────
    private void EnsureWingOverlay()
    {
        if (_wingGO != null) return;
        if (wingSpritesUp == null || wingSpritesUp.Length == 0)
        {
            Debug.LogWarning("[Seraphim] wingSpritesUp atanmamış, overlay oluşturulmadı.");
            return;
        }

        _wingGO = new GameObject("SeraphimWingOverlay");
        _wingGO.transform.SetParent(transform, false);
        _wingGO.transform.localPosition = new Vector3(wingOffsetUp.x, wingOffsetUp.y, 0f);

        _wingSR = _wingGO.AddComponent<SpriteRenderer>();
        _wingSR.sortingLayerName = "Default";
        _wingSR.sortingOrder     = WING_OVERLAY_ORDER;
        _wingSR.sprite           = wingSpritesUp[0];

        _wingGO.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Update
    // ─────────────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (!_active) return;

        UpdateBodyAnimations();
        UpdateWingOverlay();
        UpdateUpBodyBob();
        UpdateFlightCollisions();
    }

    // ── Body animasyonları ────────────────────────────────────────────────────
    private void UpdateBodyAnimations()
    {
        float dt   = Time.deltaTime;
        float step = 1f / FPS;

        Tick(ref _timerDown,  ref _frameDown,  _srDown,  bodySpritesDown,  dt, step);
        Tick(ref _timerUp,    ref _frameUp,    _srUp,    bodySpritesUp,    dt, step);
        Tick(ref _timerRight, ref _frameRight, _srRight, bodySpritesRight, dt, step);
        Tick(ref _timerLeft,  ref _frameLeft,  _srLeft,  bodySpritesRight, dt, step);
    }

    private static void Tick(ref float timer, ref int frame, SpriteRenderer sr,
                              Sprite[] sprites, float dt, float step)
    {
        if (sr == null || sprites == null || sprites.Length == 0) return;
        timer += dt;
        if (timer >= step)
        {
            timer -= step;
            frame  = (frame + 1) % sprites.Length;
            sr.sprite = sprites[frame];
        }
    }

    // ── Yukarı yön body bob animasyonu ───────────────────────────────────────
    // Önce 0.05 birim aşağı kayar, ardından 0.05 birim yukarı çıkar (sürekli tekrar)
    private void UpdateUpBodyBob()
    {
        if (_movement == null || _movement.spritesUp.body == null) return;

        _bobPhase += Time.deltaTime * BOB_SPEED;
        if (_bobPhase >= Mathf.PI * 2f) _bobPhase -= Mathf.PI * 2f;

        // sin(-π/2) = -1 → başlangıçta aşağıda; sin(π/2) = +1 → yukarıda
        // Faz başlangıcı -π/2 ile aşağı yönde başlatılır
        float bobY = Mathf.Sin(_bobPhase - Mathf.PI / 2f) * BOB_AMPLITUDE;

        var t = _movement.spritesUp.body.transform;
        t.localPosition = new Vector3(t.localPosition.x, bodyBaseYUp + bobY, t.localPosition.z);
    }

    // ── Kanat overlay animasyonu ──────────────────────────────────────────────
    private void UpdateWingOverlay()
    {
        if (_wingGO == null || _wingSR == null || _movement == null) return;

        bool facingUp = IsFacingUp();
        _wingGO.SetActive(facingUp);

        if (!facingUp) return;

        _wingTimer += Time.deltaTime;
        if (_wingTimer >= 1f / FPS)
        {
            _wingTimer -= 1f / FPS;
            _wingFrame  = (_wingFrame + 1) % wingSpritesUp.Length;
            _wingSR.sprite = wingSpritesUp[_wingFrame];
        }
    }

    private bool IsFacingUp()
    {
        Vector2 dir = _movement.direction;
        if (dir == Vector2.zero)
            return _movement.spritesUp.body != null && _movement.spritesUp.body.enabled;
        return Mathf.Abs(dir.y) > Mathf.Abs(dir.x) && dir.y > 0;
    }

    // ── Periyodik collision güncellemesi ─────────────────────────────────────
    private float       _flightCheckTimer;
    private const float FLIGHT_CHECK_INTERVAL = 0.5f;

    private void UpdateFlightCollisions()
    {
        _flightCheckTimer += Time.deltaTime;
        if (_flightCheckTimer < FLIGHT_CHECK_INTERVAL) return;
        _flightCheckTimer = 0f;

        var cols = GetComponents<Collider2D>();
        IgnoreTaggedColliders(cols, "Destructible");
        TryIgnoreTaggedColliders(cols, "Obstacle");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Temizlik
    // ─────────────────────────────────────────────────────────────────────────
    private void OnDestroy()
    {
        if (_wingGO != null) Destroy(_wingGO);
    }
}