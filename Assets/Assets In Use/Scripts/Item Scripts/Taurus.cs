using System.Collections;
using UnityEngine;

/// <summary>
/// Taurus item — Binding of Isaac'ten esinlenilmiştir.
/// Efektler:
///  - Pickup anında karakter "Taurus" bileşeni kazanır; isActive = true olur.
///  - Düşmanlı odada duruldukça hız her saniye 0.05 artar.
///  - Maksimum hız artışı 2.5'tir (SpeedIncrease pickup'ları dahil ölçeklenir).
///  - Oda geçişlerinde hız base speed'e sıfırlanır.
///  - Hız arttıkça sprite'lar kırmızıya döner (IsaacMovement.TriggerDamageFlash mantığı).
///  - Leo.cs ile aynı head sprite değiştirme sistemi desteklenir.
///
/// KURULUM:
///  1. Bu scripti bir Item prefab'ına ekle.
///  2. Inspector'dan dört yön head sprite dizilerini doldur (isteğe bağlı).
///  3. RoomEnemyTracker'ın OnPlayerEntered/OnPlayerExited çağrılarından
///     Taurus.OnRoomEntered() / Taurus.OnRoomExited() metodlarını çağır.
///     YA DA bu scriptin kendi OnTriggerEnter2D/Exit2D'si oda trigger'ından
///     tetiklenecek şekilde ayarla (bkz. EnemyRoomTrigger).
/// </summary>
public class Taurus : MonoBehaviour, IItem
{
    [Header("UI Icon")]
    public Sprite iconSprite;
    [Header("Head Sprites (Taurus versiyonu)")]
    [Tooltip("Yukarı yön head sprite dizisi.")]
    public Sprite[] headSpritesUp;

    [Tooltip("Aşağı yön head sprite dizisi.")]
    public Sprite[] headSpritesDown;

    [Tooltip("Sol yön head sprite dizisi.")]
    public Sprite[] headSpritesLeft;

    [Tooltip("Sağ yön head sprite dizisi.")]
    public Sprite[] headSpritesRight;

    [Header("Taurus Ayarları")]
    [Tooltip("Her saniye kazanılan hız miktarı.")]
    public float speedPerSecond = 0.05f;

    [Tooltip("Base'e eklenebilecek maksimum hız artışı (SpeedIncrease pickup'larından bağımsız tavan).")]
    public float maxSpeedBonus = 3f;

    /// <summary>
    /// Diğer sistemler bu flag'i kontrol edebilir.
    /// </summary>
    [HideInInspector] public bool isActive = false;

    // ── İç durum ─────────────────────────────────────────────────────────────
    private IsaacMovement movement;
    private float baseSpeed;          // pickup anındaki orijinal hız
    private float currentBonus;       // [0, maxSpeedBonus]
    private bool inEnemyRoom = false;
    private Coroutine speedRoutine;
    private Coroutine tintRoutine;

    // ── Head sprite değiştirme (Leo.cs ile aynı mantık) ──────────────────────

    public void ApplyHeadSprites(GameObject player)
    {
        var mov = player.GetComponent<IsaacMovement>();
        if (mov == null) return;

        ReplaceHeadSprites(mov.spritesUp.head,    headSpritesUp);
        ReplaceHeadSprites(mov.spritesDown.head,  headSpritesDown);
        ReplaceHeadSprites(mov.spritesLeft.head,  headSpritesLeft);
        ReplaceHeadSprites(mov.spritesRight.head, headSpritesRight);

        OffsetHeadPosition(mov.spritesUp.head,    0.05f);
        OffsetHeadPosition(mov.spritesDown.head,  0.05f);
        OffsetHeadPosition(mov.spritesLeft.head,  0.05f);
        OffsetHeadPosition(mov.spritesRight.head, 0.05f);

        ForceRefreshHead(mov.spritesUp.head);
        ForceRefreshHead(mov.spritesDown.head);
        ForceRefreshHead(mov.spritesLeft.head);
        ForceRefreshHead(mov.spritesRight.head);
    }

    private void OffsetHeadPosition(AnimatedSpriteRenderer asr, float yOffset)
    {
        if (asr == null) return;
        var t = asr.transform;
        t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y + yOffset, t.localPosition.z);
    }

    private void ReplaceHeadSprites(AnimatedSpriteRenderer asr, Sprite[] newSprites)
    {
        if (asr == null || newSprites == null || newSprites.Length == 0) return;
        asr.animationSprites = newSprites;
        asr.ResetAnimation(); // ← bunu ekle
    }

    private void ForceRefreshHead(AnimatedSpriteRenderer asr)
    {
        if (asr == null) return;
        asr.enabled = false;
        asr.enabled = true;
    }

    // ── IItem: Pickup ─────────────────────────────────────────────────────────

    public void Pickup(GameObject player)
    {
        var taurusOnPlayer = player.GetComponent<Taurus>();
        if (taurusOnPlayer == null)
            taurusOnPlayer = player.AddComponent<Taurus>();

        // Ayarları kopyala
        taurusOnPlayer.headSpritesUp    = headSpritesUp;
        taurusOnPlayer.headSpritesDown  = headSpritesDown;
        taurusOnPlayer.headSpritesLeft  = headSpritesLeft;
        taurusOnPlayer.headSpritesRight = headSpritesRight;
        taurusOnPlayer.speedPerSecond   = speedPerSecond;
        taurusOnPlayer.maxSpeedBonus    = maxSpeedBonus;

        taurusOnPlayer.isActive = true;

        // Bileşen referanslarını kur
        taurusOnPlayer.movement  = player.GetComponent<IsaacMovement>();
        taurusOnPlayer.baseSpeed = taurusOnPlayer.movement != null
            ? Mathf.Min(taurusOnPlayer.movement.speed, IsaacMovement.MaxNormalSpeed)
            : IsaacMovement.DefaultSpeed;

        if (taurusOnPlayer.movement != null)
            taurusOnPlayer.movement.speed = taurusOnPlayer.baseSpeed;

        Debug.Log($"[Taurus] Pickup → player={player.name}, baseSpeed={taurusOnPlayer.baseSpeed}");

        // Head sprite uygula
        taurusOnPlayer.ApplyHeadSprites(player);
        ItemIconUI.Instance?.AddItemIcon(iconSprite, "Taurus");
    }

    // ── Oda olayları (RoomEnemyTracker veya oda trigger'ı tarafından çağrılır) ─

    /// <summary>
    /// Düşmanlı odaya girildiğinde çağır.
    /// </summary>
    public void OnEnemyRoomEntered()
    {
        if (!isActive) return;
        inEnemyRoom = true;
        StartSpeedRamp();
        Debug.Log("[Taurus] Düşmanlı odaya girildi, hız artışı başladı.");
    }

    /// <summary>
    /// Odadan çıkıldığında çağır (düşmanlı olsun olmasın).
    /// </summary>
    public void OnRoomExited()
    {
        if (!isActive) return;
        inEnemyRoom = false;
        StopSpeedRamp();
        ResetSpeed();
        Debug.Log("[Taurus] Odadan çıkıldı, hız sıfırlandı.");
    }

    // ── Hız rampa ────────────────────────────────────────────────────────────

    private void StartSpeedRamp()
    {
        if (speedRoutine != null) StopCoroutine(speedRoutine);
        speedRoutine = StartCoroutine(SpeedRampRoutine());
    }

    private void StopSpeedRamp()
    {
        if (speedRoutine != null)
        {
            StopCoroutine(speedRoutine);
            speedRoutine = null;
        }
    }

    private IEnumerator SpeedRampRoutine()
    {
        while (inEnemyRoom && currentBonus < GetCurrentMaxSpeedBonus())
        {
            yield return new WaitForSeconds(1f);

            float maxBonus = GetCurrentMaxSpeedBonus();
            float add = Mathf.Min(speedPerSecond, maxBonus - currentBonus);
            currentBonus += add;

            if (movement != null)
                movement.speed = Mathf.Min(baseSpeed + currentBonus, IsaacMovement.MaxTaurusSpeed);

            ApplySpeedTint();

            Debug.Log($"[Taurus] Hız artışı → bonus={currentBonus:F2}, speed={movement?.speed:F2}");
        }

        speedRoutine = null;
    }

    private void ResetSpeed()
    {
        currentBonus = 0f;

        if (movement != null)
            movement.speed = baseSpeed;

        ClearTint();
    }

    // ── Kızarma efekti ────────────────────────────────────────────────────────

    /// <summary>
    /// Hız artışı oranına (0→1) göre tüm sprite'ları kırmızıya çevirir.
    /// </summary>
    private void ApplySpeedTint()
    {
        if (movement == null) return;

        float maxBonus = GetCurrentMaxSpeedBonus();
        float t = maxBonus > 0f ? currentBonus / maxBonus : 0f; // 0..1

        // Beyazdan kırmızıya: R=1 sabit, G ve B azalır
        Color tintColor = Color.Lerp(Color.white, new Color(1f, 0.15f, 0.15f, 1f), t);

        SetAllSpritesColor(tintColor);
    }

    private void ClearTint()
    {
        SetAllSpritesColor(Color.white);
    }

    private void SetAllSpritesColor(Color color)
    {
        if (movement == null) return;

        SetASRColor(movement.spritesUp.body,    color);
        SetASRColor(movement.spritesUp.head,    color);
        SetASRColor(movement.spritesDown.body,  color);
        SetASRColor(movement.spritesDown.head,  color);
        SetASRColor(movement.spritesLeft.body,  color);
        SetASRColor(movement.spritesLeft.head,  color);
        SetASRColor(movement.spritesRight.body, color);
        SetASRColor(movement.spritesRight.head, color);
    }

    private void SetASRColor(AnimatedSpriteRenderer asr, Color color)
    {
        if (asr == null) return;
        var sr = asr.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = color;
    }

    private float GetCurrentMaxSpeedBonus()
    {
        return Mathf.Max(0f, Mathf.Min(maxSpeedBonus, IsaacMovement.MaxTaurusSpeed - baseSpeed));
    }

    // ── SpeedIncrease pickup desteği ──────────────────────────────────────────

    /// <summary>
    /// ItemPickup → SpeedIncrease alındığında bu metodu çağır.
    /// Base speed güncellenir; mevcut bonus korunur, sadece tavan kayar.
    /// </summary>
    public void OnSpeedPickup(float newBaseSpeed)
    {
        baseSpeed = Mathf.Min(newBaseSpeed, IsaacMovement.MaxNormalSpeed);
        // Mevcut bonus geçerliliğini koru; maxSpeedBonus sabit kalır
        if (movement != null)
            movement.speed = Mathf.Min(baseSpeed + currentBonus, IsaacMovement.MaxTaurusSpeed);

        Debug.Log($"[Taurus] SpeedPickup → yeni baseSpeed={baseSpeed}, speed={movement?.speed}");
    }
}
