using UnityEngine;

/// <summary>
/// Leo item'ı — Binding of Isaac'ten esinlenilmiştir.
/// Efektler:
///  - Pickup anında karakter "Leo" bileşeni kazanır; isActive = true olur.
///  - Isaac'ın dört yön head sprite'larını Leo versiyonuyla değiştirir
///    (Pyromaniac ile aynı ApplyHeadSprites mantığı).
///  - Karakter hareket ederken Destructible tag'li / bileşenli objelere
///    çarptığında OnCollisionEnter2D yakalanır ve Destructible.Explode()
///    çağrılır — taş/engel direkt kırılır, hasar almaz.
///
/// KURULUM:
///  1. Bu scripti bir Item prefab'ına ekle.
///  2. Inspector'dan dört yön head sprite dizilerini doldur.
///  3. Player GameObject'inin Rigidbody2D'si "Dynamic" olmalı;
///     Destructible objelerin Collider2D'si "IsTrigger = false" olmalı
///     — böylece OnCollisionEnter2D tetiklenir.
///     Eğer Destructible collider'ları Trigger ise OnTriggerEnter2D
///     bloğunu kullan (aşağıda açıklama satırı olarak bırakıldı).
/// </summary>
public class Leo : MonoBehaviour, IItem
{
    [Header("Head Sprites (Leo versiyonu)")]
    [Tooltip("Yukarı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesUp;

    [Tooltip("Aşağı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesDown;

    [Tooltip("Sol yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesLeft;

    [Tooltip("Sağ yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesRight;

    /// <summary>
    /// BombController ya da başka sistemler bu flag'i kontrol edebilir.
    /// </summary>
    [HideInInspector] public bool isActive = false;

    // ── Head sprite değiştirme ────────────────────────────────────────────

    public void ApplyHeadSprites(GameObject player)
    {
        var movement = player.GetComponent<IsaacMovement>();
        if (movement == null) return;

        ReplaceHeadSprites(movement.spritesUp.head,    headSpritesUp);
        ReplaceHeadSprites(movement.spritesDown.head,  headSpritesDown);
        ReplaceHeadSprites(movement.spritesLeft.head,  headSpritesLeft);
        ReplaceHeadSprites(movement.spritesRight.head, headSpritesRight);

        ForceRefreshHead(movement.spritesUp.head);
        ForceRefreshHead(movement.spritesDown.head);
        ForceRefreshHead(movement.spritesLeft.head);
        ForceRefreshHead(movement.spritesRight.head);
    }

    private void ReplaceHeadSprites(AnimatedSpriteRenderer asr, Sprite[] newSprites)
    {
        if (asr == null || newSprites == null || newSprites.Length == 0) return;

        asr.animationSprites = null;
        asr.animationSprites = newSprites;
    }

    private void ForceRefreshHead(AnimatedSpriteRenderer asr)
    {
        if (asr == null) return;
        asr.enabled = false;
        asr.enabled = true;
    }

    // ── IItem: Pickup ─────────────────────────────────────────────────────

    public void Pickup(GameObject player)
    {
        // Leo bileşenini player'a kopyala
        var leoOnPlayer = player.GetComponent<Leo>();
        if (leoOnPlayer == null)
            leoOnPlayer = player.AddComponent<Leo>();

        // Ayarları kopyala
        leoOnPlayer.headSpritesUp    = headSpritesUp;
        leoOnPlayer.headSpritesDown  = headSpritesDown;
        leoOnPlayer.headSpritesLeft  = headSpritesLeft;
        leoOnPlayer.headSpritesRight = headSpritesRight;

        // Aktifleştir
        leoOnPlayer.isActive = true;
        Debug.Log($"[Leo] Pickup çağrıldı. isActive={leoOnPlayer.isActive}, player={player.name}");

        // Head sprite'larını uygula
        leoOnPlayer.ApplyHeadSprites(player);
    }

    // ── Taş / Destructible çarpma mekaniği ───────────────────────────────

    /// <summary>
    /// Player'ın Rigidbody2D'si hareket ederken Collider (IsTrigger=false)
    /// olan Destructible'a çarptığında tetiklenir.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isActive) return;

        TryDestroyDestructible(collision.gameObject);
    }

    /// <summary>
    /// Eğer Destructible collider'ları Trigger ise bu metodu kullan,
    /// OnCollisionEnter2D yerine OnTriggerEnter2D bloğunu etkinleştir.
    /// </summary>
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (!isActive) return;
    //     TryDestroyDestructible(other.gameObject);
    // }

    /// <summary>
    /// Verilen GameObject üzerinde Destructible bileşeni varsa patlatır.
    /// Explosion layer'ı üretmek yerine doğrudan Explode() çağrılır —
    /// bu sayede hasar alma mekanizması devreye girmez.
    /// </summary>
    private void TryDestroyDestructible(GameObject target)
    {
        var destructible = target.GetComponent<Destructible>();
        if (destructible == null) return;

        Debug.Log($"[Leo] Destructible kırıldı → {target.name}");
        destructible.Explode();
    }
}