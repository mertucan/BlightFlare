using UnityEngine;

public class CeremonialRobeEffect : MonoBehaviour
{
    [HideInInspector] public Sprite[] overlaySpritesDown;
    [HideInInspector] public Sprite[] overlaySpritesUp;
    [HideInInspector] public Sprite[] overlaySpritesLeft;
    [HideInInspector] public Sprite[] overlaySpritesRight;

    [HideInInspector] public float   overlayFps           = 8f;
    [HideInInspector] public Vector2 overlayOffsetDown    = new Vector2(0f, 0.6f);
    [HideInInspector] public Vector2 overlayOffsetUp      = new Vector2(0f, 0.6f);
    [HideInInspector] public Vector2 overlayOffsetLeft    = new Vector2(0f, 0.6f);
    [HideInInspector] public Vector2 overlayOffsetRight   = new Vector2(0f, 0.6f);

    [HideInInspector] public Sprite  glowSprite;
    [HideInInspector] public Vector2 glowOffset           = new Vector2(0f, 0f);

    private const int OVERLAY_ORDER = 7;
    private const int GLOW_ORDER    = 3;

    private GameObject     _overlayGO;
    private SpriteRenderer _overlaySR;
    private GameObject     _glowGO;
    private SpriteRenderer _glowSR;

    private float _animTimer;
    private int   _animFrame;

    private FacingDirection _lastDir = FacingDirection.None;
    private IsaacMovement   _movement;

    private enum FacingDirection { None, Down, Up, Left, Right }

    private void Awake()
    {
        _movement = GetComponent<IsaacMovement>();
    }

    private void Update()
    {
        if (_overlayGO == null) return;

        FacingDirection dir     = GetFacingDirection();
        Sprite[]        sprites = GetSpritesForDirection(dir);

        if (sprites == null || sprites.Length == 0)
        {
            _overlayGO.SetActive(false);
            return;
        }

        if (dir != _lastDir)
        {
            _lastDir   = dir;
            _animFrame = 0;
            _animTimer = 0f;
            _overlayGO.transform.localPosition = GetOffsetForDirection(dir);
            _overlaySR.flipX = false; // Artık ayrı sprite olduğu için flip yok
        }

        _animTimer += Time.deltaTime;
        if (_animTimer >= 1f / overlayFps)
        {
            _animTimer = 0f;
            _animFrame = (_animFrame + 1) % sprites.Length;
        }

        _overlaySR.sprite = sprites[_animFrame];
        _overlayGO.SetActive(true);
    }

    public void Activate()
    {
        EnsureOverlay();
        EnsureGlow();
        _lastDir   = FacingDirection.None;
        _animFrame = 0;
        _animTimer = 0f;
        Debug.Log("[CeremonialRobeEffect] Aktive edildi.");
    }

    private void EnsureOverlay()
    {
        if (_overlayGO != null) return;

        _overlayGO = new GameObject("CeremonialRobeOverlay");
        _overlayGO.transform.SetParent(transform, false);

        _overlaySR = _overlayGO.AddComponent<SpriteRenderer>();
        _overlaySR.sortingLayerName = "Default";
        _overlaySR.sortingOrder     = OVERLAY_ORDER;
    }

    private void EnsureGlow()
    {
        if (_glowGO != null) return;
        if (glowSprite == null)
        {
            Debug.LogWarning("[CeremonialRobeEffect] glowSprite atanmamış.");
            return;
        }

        _glowGO = new GameObject("CeremonialRobeGlow");
        _glowGO.transform.SetParent(transform, false);
        _glowGO.transform.localPosition = new Vector3(glowOffset.x, glowOffset.y, 0f);

        _glowSR = _glowGO.AddComponent<SpriteRenderer>();
        _glowSR.sortingLayerName = "Default";
        _glowSR.sortingOrder     = GLOW_ORDER;
        _glowSR.sprite           = glowSprite;

        _glowGO.SetActive(true);
    }

    private FacingDirection GetFacingDirection()
    {
        if (_movement == null) return FacingDirection.Down;

        Vector2 dir = _movement.direction;

        if (dir == Vector2.zero)
            return _lastDir == FacingDirection.None ? FacingDirection.Down : _lastDir;

        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
            return dir.y > 0 ? FacingDirection.Up : FacingDirection.Down;
        else
            return dir.x > 0 ? FacingDirection.Right : FacingDirection.Left;
    }

    private Sprite[] GetSpritesForDirection(FacingDirection dir) => dir switch
    {
        FacingDirection.Up    => overlaySpritesUp,
        FacingDirection.Left  => overlaySpritesLeft,
        FacingDirection.Right => overlaySpritesRight,
        _                     => overlaySpritesDown,
    };

    private Vector3 GetOffsetForDirection(FacingDirection dir) => dir switch
    {
        FacingDirection.Up    => new Vector3(overlayOffsetUp.x,    overlayOffsetUp.y,    0f),
        FacingDirection.Left  => new Vector3(overlayOffsetLeft.x,  overlayOffsetLeft.y,  0f),
        FacingDirection.Right => new Vector3(overlayOffsetRight.x, overlayOffsetRight.y, 0f),
        _                     => new Vector3(overlayOffsetDown.x,  overlayOffsetDown.y,  0f),
    };
}