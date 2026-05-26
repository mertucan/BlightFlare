using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class NightWatchAI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // SPRITESHEET
    // ─────────────────────────────────────────────
    [Header("Idle Sprite'ları")]
    public Sprite[] idleDown;
    public Sprite[] idleUp;
    public Sprite[] idleLeft;
    public Sprite[] idleRight;

    [Header("Yürüme Sprite'ları")]
    public Sprite[] walkDown;
    public Sprite[] walkUp;
    public Sprite[] walkLeft;
    public Sprite[] walkRight;

    [Header("Delirme Sprite'ları (Sol yön — sağ için flipX kullanılır)")]
    public Sprite[] frenzyDown;
    public Sprite[] frenzyUp;
    public Sprite[] frenzyLeft;

    [Header("Animasyon FPS")]
    public float idleFPS   = 4f;
    public float walkFPS   = 8f;
    public float frenzyFPS = 12f;

    // ─────────────────────────────────────────────
    // STATS
    // ─────────────────────────────────────────────
    [Header("Stats")]
    public int maxHP = 5;
    public float normalSpeed  = 1.5f;
    public float frenzySpeed  = 4.5f;
    public string playerTag   = "Player";

    // ─────────────────────────────────────────────
    // HASAR KORUMASI
    // ─────────────────────────────────────────────
    [Header("Hasar Koruması")]
    public float damageCooldown = 0.5f;
    private float lastDamageTime = -999f;

    // ─────────────────────────────────────────────
    // ROOM INSET
    // ─────────────────────────────────────────────
    [Header("Room Inset Per Side")]
    public float roomInsetLeft   = 2.0f;
    public float roomInsetRight  = 2.5f;
    public float roomInsetBottom = 2.0f;
    public float roomInsetTop    = 2.0f;

    // ─────────────────────────────────────────────
    // TELEPORT
    // ─────────────────────────────────────────────
    [Header("Teleport")]
    public float teleportCooldownMin    = 5f;
    public float teleportCooldownMax    = 10f;
    public float teleportFadeOutDur     = 0.18f;
    public float teleportHoldDur        = 0.1f;
    public float teleportFadeInDur      = 0.18f;
    public float teleportMinDist        = 2.5f;
    public float teleportMaxDist        = 6f;
    public float teleportValidRadius    = 0.3f;
    public int   teleportMaxAttempts    = 60;

    // ─────────────────────────────────────────────
    // DARKNESS (ekran karartma)
    // ─────────────────────────────────────────────
    [Header("Darkness Effect")]
    public UnityEngine.UI.Image darknessOverlay;
    public float darknessMaxAlpha    = 0.82f;
    public float darknessFadeInDur   = 1.2f;
    public float darknessFadeOutDur  = 7.0f;
    public float darknessCooldownMin = 8f;
    public float darknessCooldownMax = 18f;

    [Header("Frenzy Süresi (saniye)")]
    public float frenzyDuration = 5f;   // Karanlık bitince bu kadar frenzy kalır, sonra normal moda döner

    // ─────────────────────────────────────────────
    // LANTERN LIGHT
    // ─────────────────────────────────────────────
    [Header("Lantern Light")]
    public Transform lanternLight;
    public float lanternOffset = 0.6f;

    // ─────────────────────────────────────────────
    // WANDER
    // ─────────────────────────────────────────────
    [Header("Wander")]
    public float wanderRadius            = 3f;
    public float wanderArrivalRadius     = 0.6f;
    public float wanderChangeIntervalMin = 2f;
    public float wanderChangeIntervalMax = 4f;

    // ─────────────────────────────────────────────
    // WALL AVOIDANCE
    // ─────────────────────────────────────────────
    [Header("Wall Avoidance")]
    public string wallTag        = "Wall";
    public string doorTag        = "Door";
    public float wallAvoidDist   = 1.2f;
    public float wallAvoidWeight = 2.5f;

    // ─────────────────────────────────────────────
    // DAMAGE / FEEDBACK
    // ─────────────────────────────────────────────
    [Header("Damage")]
    public float knockbackForce   = 4f;
    public float hitFlashDuration = 0.1f;
    public Color hitFlashColor    = Color.red;
    public GameObject deathEffectPrefab;

    // ─────────────────────────────────────────────
    // SOUNDS
    // ─────────────────────────────────────────────
    [Header("Sounds")]
    public AudioSource audioSource;

    [Header("  → Karartma Sesi")]
    public AudioClip[] darknessClips;
    public int selectedDarknessClip = 0;

    [Header("  → Delirme Sesi")]
    public AudioClip[] frenzyClips;
    public int selectedFrenzyClip = 0;

    [Header("  → Hasar Sesi")]
    public AudioClip[] damageClips;
    public int selectedDamageClip = 0;

    [Header("  → Ölüm Sesi")]
    public AudioClip[] deathClips;
    public int selectedDeathClip = 0;

    [Header("  -> Death Music")]
    public AudioClip deathMusic;
    [Range(0f, 1f)] public float deathMusicVolume = 1f;

    [Header("  → Işınlanma Sesi")]
    public AudioClip[] teleportClips;
    public int selectedTeleportClip = 0;

    // ─────────────────────────────────────────────
    // RUNTIME (Read Only)
    // ─────────────────────────────────────────────
    [Header("Runtime (Debug — Read Only)")]
    [SerializeField] private int  currentHP;
    [SerializeField] private bool isFrenzy;
    [SerializeField] private bool isTeleporting;
    [SerializeField] private bool isDead;
    [SerializeField] private bool isDarkness;
    [SerializeField] private Rect roomSafeRect;

    // ─── privates ────────────────────────────────
    private Rigidbody2D  rb;
    private Collider2D   ownCollider;
    private Transform    player;

    private float teleportTimer;
    private float darknessTimer;

    private Vector2 smoothDampVel;
    private Vector2 wanderTarget;
    private float   wanderChangeTimer;

    private bool isActivated = false;

    // Kapı referansları — BossRoomTrigger'dan set edilir
    private Collider2D[] managedDoors;
    private BossRoomTrigger bossRoomTrigger;

    // Sprite animation
    private enum FacingDir { Down, Up, Left, Right }
    private FacingDir facing = FacingDir.Down;
    public SpriteRenderer spriteRenderer;

    private Color originalColor = Color.white;

    // ══════════════════════════════════════════════
    #region Unity Callbacks

    private void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        ownCollider = GetComponent<Collider2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        currentHP = Mathf.Max(1, maxHP);
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        teleportTimer     = Random.Range(teleportCooldownMin, teleportCooldownMax);
        darknessTimer     = Random.Range(darknessCooldownMin, darknessCooldownMax);
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);

        SetAnim(idleDown, idleFPS);
    }

    private void Start()
    {
        GatherRoomBounds();
        wanderTarget = rb.position;

        if (darknessOverlay == null)
            darknessOverlay = FindFirstObjectByType<UnityEngine.UI.Image>(FindObjectsInactive.Include);

        if (darknessOverlay != null)
        {
            var c = darknessOverlay.color;
            c.a = 0f;
            darknessOverlay.color = c;
            darknessOverlay.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (isDead || !isActivated) return;
        if (player == null) TryFindPlayer();
        if (player == null) return;

        teleportTimer -= Time.deltaTime;
        darknessTimer -= Time.deltaTime;

        if (!isTeleporting && teleportTimer <= 0f)
            StartCoroutine(TeleportRoutine());

        if (!isDarkness && darknessTimer <= 0f)
            StartCoroutine(DarknessRoutine());

        wanderChangeTimer -= Time.deltaTime;
        if (wanderChangeTimer <= 0f ||
            Vector2.Distance(rb.position, wanderTarget) < wanderArrivalRadius)
            PickNewWanderTarget();

        UpdateFacing();
        TickAnimation();
        UpdateLantern();
    }

    private void FixedUpdate()
    {
        if (isDead || !isActivated || rb == null) return;
        if (player == null) { rb.linearVelocity = Vector2.zero; return; }
        if (isTeleporting)  { rb.linearVelocity = Vector2.zero; return; }

        Vector2 desired;
        float   speed = isFrenzy ? frenzySpeed : normalSpeed;

        if (isFrenzy)
        {
            Vector2 toPlayer = ((Vector2)player.position - rb.position).normalized;
            desired = toPlayer * speed;
        }
        else
        {
            desired = ComputeWanderVelocity() * speed;
        }

        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, desired,
            ref smoothDampVel, 0.25f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            // Hasar cooldown kontrolü
            if (Time.time - lastDamageTime < damageCooldown) return;
            lastDamageTime = Time.time;

            Vector2 knockDir = ((Vector2)transform.position - (Vector2)other.transform.position).normalized;
            TakeDamage(1, knockDir);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (isDead) return;
        if (col.gameObject.CompareTag(playerTag)) KillPlayer(col.gameObject);
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (isDead) return;
        if (col.gameObject.CompareTag(playerTag)) KillPlayer(col.gameObject);
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Sprite Animation

    private Sprite[] currentAnim;
    private float animTimer;
    private int   currentFrame;
    private float currentFPS;

    private void SetAnim(Sprite[] anim, float fps)
    {
        if (anim == null || anim.Length == 0) return;
        if (currentAnim == anim) return;
        currentAnim  = anim;
        currentFPS   = fps;
        currentFrame = 0;
        animTimer    = 0f;
        ApplyFrame();
    }

    private void TickAnimation()
    {
        if (currentAnim == null || currentAnim.Length == 0) return;
        animTimer += Time.deltaTime;
        if (animTimer >= 1f / currentFPS)
        {
            animTimer -= 1f / currentFPS;
            currentFrame = (currentFrame + 1) % currentAnim.Length;
            ApplyFrame();
        }
    }

    private void ApplyFrame()
    {
        if (spriteRenderer == null || currentAnim == null) return;
        int idx = Mathf.Clamp(currentFrame, 0, currentAnim.Length - 1);
        spriteRenderer.sprite = currentAnim[idx];
    }

    private Sprite[] GetAnim(bool frenzy, bool moving)
    {
        if (frenzy)
            return facing switch
            {
                FacingDir.Up    => frenzyUp,
                FacingDir.Down  => frenzyDown,
                // Hem Left hem Right için frenzyLeft kullanılır; sağa bakınca flipX=true
                FacingDir.Left  => frenzyLeft,
                FacingDir.Right => frenzyLeft,
                _               => frenzyDown
            };

        if (moving)
            return facing switch
            {
                FacingDir.Up    => walkUp,
                FacingDir.Down  => walkDown,
                FacingDir.Left  => walkLeft,
                FacingDir.Right => walkRight,
                _               => walkDown
            };

        return facing switch
        {
            FacingDir.Up    => idleUp,
            FacingDir.Down  => idleDown,
            FacingDir.Left  => idleLeft,
            FacingDir.Right => idleRight,
            _               => idleDown
        };
    }

    private void UpdateFacing()
    {
        if (player == null) return;

        Vector2 dir = isFrenzy
            ? ((Vector2)player.position - rb.position)
            : (wanderTarget - rb.position);

        if (dir.sqrMagnitude < 0.01f) return;

        FacingDir newFacing;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            newFacing = dir.x > 0 ? FacingDir.Right : FacingDir.Left;
        else
            newFacing = dir.y > 0 ? FacingDir.Up : FacingDir.Down;

        facing = newFacing;

        // Frenzy sağa bakarken flipX — diğer tüm durumlarda false
        spriteRenderer.flipX = (isFrenzy && facing == FacingDir.Right);

        bool moving = rb.linearVelocity.sqrMagnitude > 0.05f;
        float fps   = isFrenzy ? frenzyFPS : (moving ? walkFPS : idleFPS);
        SetAnim(GetAnim(isFrenzy, moving), fps);
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Darkness Routine

    private IEnumerator DarknessRoutine()
    {
        isDarkness = true;
        PlaySound(darknessClips, selectedDarknessClip);

        // Frenzy moduna geç
        if (!isFrenzy)
        {
            isFrenzy = true;
            SetAnim(GetAnim(true, false), frenzyFPS);
            PlaySound(frenzyClips, selectedFrenzyClip);
        }

        // Ekranı karart
        yield return StartCoroutine(FadeOverlay(0f, darknessMaxAlpha, darknessFadeInDur));

        // Karanlıkta kısa bekle
        yield return new WaitForSeconds(darknessFadeOutDur * 0.3f);

        // Yavaş aydınlan
        yield return StartCoroutine(FadeOverlay(darknessMaxAlpha, 0f, darknessFadeOutDur));

        // Karanlık bitti — frenzy'i kısa süre daha devam ettir, sonra normal moda dön
        yield return new WaitForSeconds(frenzyDuration);

        isFrenzy = false;
        spriteRenderer.flipX = false;
        SetAnim(GetAnim(false, rb.linearVelocity.sqrMagnitude > 0.05f), idleFPS);

        isDarkness = false;
        darknessTimer = Random.Range(darknessCooldownMin, darknessCooldownMax);
    }

    private IEnumerator FadeOverlay(float fromAlpha, float toAlpha, float duration)
    {
        if (darknessOverlay == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            var c = darknessOverlay.color;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            darknessOverlay.color = c;
            yield return null;
        }

        var fc = darknessOverlay.color;
        fc.a = toAlpha;
        darknessOverlay.color = fc;
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Lantern Light

    private void UpdateLantern()
    {
        if (lanternLight == null) return;

        Vector2 dir = facing switch
        {
            FacingDir.Up    => Vector2.up,
            FacingDir.Down  => Vector2.down,
            FacingDir.Left  => Vector2.left,
            FacingDir.Right => Vector2.right,
            _               => Vector2.down
        };

        lanternLight.position = (Vector2)transform.position + dir * lanternOffset;
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Teleport

    private IEnumerator TeleportRoutine()
    {
        if (isTeleporting) yield break;
        isTeleporting = true;

        float t;

        // Fade out
        t = 0f;
        while (t < teleportFadeOutDur && !isDead)
        {
            t += Time.deltaTime;
            SetColor(new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, t / teleportFadeOutDur)));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetColor(new Color(1f, 1f, 1f, 0f));

        yield return new WaitForSeconds(teleportHoldDur);
        if (isDead) { FinishTeleport(); yield break; }

        Vector2 newPos = FindTeleportPos();
        rb.position        = newPos;
        transform.position = (Vector3)newPos;
        rb.linearVelocity  = Vector2.zero;
        smoothDampVel      = Vector2.zero;
        PlaySound(teleportClips, selectedTeleportClip);

        // Fade in
        t = 0f;
        while (t < teleportFadeInDur && !isDead)
        {
            t += Time.deltaTime;
            SetColor(new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, t / teleportFadeInDur)));
            yield return null;
        }
        if (isDead) { FinishTeleport(); yield break; }
        SetColor(originalColor);

        FinishTeleport();
        teleportTimer = Random.Range(teleportCooldownMin, teleportCooldownMax);
    }

    private void FinishTeleport()
    {
        SetColor(originalColor);
        isTeleporting = false;
    }

    private Vector2 FindTeleportPos()
    {
        Vector2 playerPos = player != null ? (Vector2)player.position : Vector2.zero;

        for (int i = 0; i < teleportMaxAttempts; i++)
        {
            float x = Random.Range(roomSafeRect.xMin, roomSafeRect.xMax);
            float y = Random.Range(roomSafeRect.yMin, roomSafeRect.yMax);
            Vector2 c = new Vector2(x, y);
            float d = Vector2.Distance(c, playerPos);
            if (d < teleportMinDist || d > teleportMaxDist) continue;
            if (IsPositionValid(c)) return c;
        }
        for (int i = 0; i < 20; i++)
        {
            float x = Random.Range(roomSafeRect.xMin, roomSafeRect.xMax);
            float y = Random.Range(roomSafeRect.yMin, roomSafeRect.yMax);
            Vector2 c = new Vector2(x, y);
            if (IsPositionValid(c)) return c;
        }
        return rb.position;
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Wander / Movement

    private Vector2 ComputeWanderVelocity()
    {
        Vector2 toWander = wanderTarget - rb.position;
        Vector2 dir = toWander.sqrMagnitude > 0.001f ? toWander.normalized : Vector2.down;
        Vector2 avoid = ComputeWallAvoid();
        Vector2 final = dir + avoid * wallAvoidWeight;
        if (final.sqrMagnitude > 1f) final.Normalize();
        return final;
    }

    private Vector2 ComputeWallAvoid()
    {
        Vector2 avoid = Vector2.zero;
        Collider2D[] nearby = Physics2D.OverlapCircleAll(rb.position, wallAvoidDist);
        foreach (var w in nearby)
        {
            if (w.isTrigger || w == ownCollider) continue;
            if (!w.CompareTag(wallTag) && !w.CompareTag(doorTag)) continue;
            Vector2 closest = w.ClosestPoint(rb.position);
            Vector2 diff    = rb.position - closest;
            float   dist    = diff.magnitude;
            if (dist > 0.001f && dist < wallAvoidDist)
                avoid += (diff / dist) * ((wallAvoidDist - dist) / wallAvoidDist);
        }
        return avoid;
    }

    private void PickNewWanderTarget()
    {
        for (int i = 0; i < 20; i++)
        {
            float   angle     = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float   dist      = Random.Range(wanderRadius * 0.3f, wanderRadius);
            Vector2 candidate = rb.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
            if (IsPositionValid(candidate))
            {
                wanderTarget      = candidate;
                wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);
                return;
            }
        }
        wanderTarget      = roomSafeRect.center;
        wanderChangeTimer = Random.Range(wanderChangeIntervalMin, wanderChangeIntervalMax);
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Room Bounds

    private void GatherRoomBounds()
    {
        Vector2 origin  = transform.position;
        float   search  = 40f;
        float tolerance = 0.5f;

        float bestLeft  = origin.x - 8f;
        float bestRight = origin.x + 8f;
        float bestDown  = origin.y - 5f;
        float bestUp    = origin.y + 5f;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, search);
        foreach (var col in nearby)
        {
            if (col == ownCollider) continue;
            if (!col.CompareTag(wallTag)) continue;

            Bounds b = col.bounds;

            if (b.max.x <= origin.x && b.min.y <= origin.y + tolerance && b.max.y >= origin.y - tolerance)
                bestLeft = Mathf.Max(bestLeft, b.max.x);

            if (b.min.x >= origin.x && b.min.y <= origin.y + tolerance && b.max.y >= origin.y - tolerance)
                bestRight = Mathf.Min(bestRight, b.min.x);

            if (b.max.y <= origin.y && b.min.x <= origin.x + tolerance && b.max.x >= origin.x - tolerance)
                bestDown = Mathf.Max(bestDown, b.max.y);

            if (b.min.y >= origin.y && b.min.x <= origin.x + tolerance && b.max.x >= origin.x - tolerance)
                bestUp = Mathf.Min(bestUp, b.min.y);
        }

        roomSafeRect = Rect.MinMaxRect(
            bestLeft  + roomInsetLeft,
            bestDown  + roomInsetBottom,
            bestRight - roomInsetRight,
            bestUp    - roomInsetTop);
    }

    private bool IsPositionValid(Vector2 pos)
    {
        if (!roomSafeRect.Contains(pos)) return false;
        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, teleportValidRadius);
        foreach (var h in hits)
        {
            if (h == ownCollider) continue;
            if (h.isTrigger) continue;
            if (h.CompareTag(wallTag) || h.CompareTag(doorTag)) return false;
        }
        return true;
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Damage / Death

    public void TakeDamage(int damage, Vector2 knockDir)
    {
        if (isDead) return;
        currentHP -= Mathf.Max(1, damage);
        if (rb != null) rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

        PlaySound(damageClips, selectedDamageClip);
        StartCoroutine(HitFlashRoutine());

        if (currentHP <= 0) Die();
    }

    private IEnumerator HitFlashRoutine()
    {
        SetColor(hitFlashColor);
        yield return new WaitForSeconds(hitFlashDuration);
        if (!isTeleporting) SetColor(originalColor);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        // Kapıları aç
        OpenDoors();

        SetColor(originalColor);
        if (deathClips != null &&
            selectedDeathClip >= 0 &&
            selectedDeathClip < deathClips.Length)
        {
            BossAudioUtility.Play2D(deathClips[selectedDeathClip]);
        }
        BackgroundMusicManager.instance?.PlayBossDeathMusicThenResume(deathMusic, deathMusicVolume);

        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        if (ownCollider != null) ownCollider.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }

        // Sprite'ı gizle
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // Ekranı karart
        if (darknessOverlay != null)
            yield return StartCoroutine(FadeOverlay(darknessOverlay.color.a, 1f, 0.6f));

        // Karanlıkta bekle
        yield return new WaitForSeconds(0.8f);

        // Yavaşça aydınlan
        if (darknessOverlay != null)
            yield return StartCoroutine(FadeOverlay(1f, 0f, 1.2f));

        Destroy(gameObject);
    }

    private void KillPlayer(GameObject playerObj)
    {
        IsaacMovement isaac = playerObj.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(1);
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Door Management

    /// <summary>
    /// BossRoomTrigger tarafından çağrılır; kapıları NightWatch ölünce açabilsin.
    /// </summary>

    public void SetManagedDoors(Collider2D[] doors, BossRoomTrigger trigger = null)
    {
        managedDoors     = doors;
        bossRoomTrigger  = trigger;
    }

    private void OpenDoors()
    {
        if (managedDoors != null)
            foreach (var door in managedDoors)
                if (door != null) door.enabled = false;

        if (bossRoomTrigger != null)
            bossRoomTrigger.OnBossDefeated();
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Public API

    public void Activate()
    {
        isActivated = true;
        GatherRoomBounds();
    }

    public void Activate(Vector2 spawnPos)
    {
        rb.position        = spawnPos;
        transform.position = spawnPos;
        isActivated        = true;
        GatherRoomBounds();
    }

    public void SetActive(bool active)
    {
        isActivated = active;
        if (!active && rb != null) rb.linearVelocity = Vector2.zero;
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Helpers

    private void SetColor(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = c;
    }

    private void TryFindPlayer()
    {
        GameObject go = GameObject.FindGameObjectWithTag(playerTag);
        if (go != null) player = go.transform;
    }

    private void PlaySound(AudioClip[] clips, int index)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        if (index < 0 || index >= clips.Length || clips[index] == null) return;
        audioSource.PlayOneShot(clips[index]);
    }

    #endregion

    // ══════════════════════════════════════════════
    #region Gizmos
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && roomSafeRect.width > 0)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(
                new Vector3(roomSafeRect.center.x, roomSafeRect.center.y, 0f),
                new Vector3(roomSafeRect.width, roomSafeRect.height, 0f));
        }
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, wanderTarget);
            Gizmos.DrawWireSphere(wanderTarget, 0.2f);
        }
    }
#endif
    #endregion
}
