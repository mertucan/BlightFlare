using UnityEngine;

/// <summary>
/// TrapImmunity item'ı.
/// Efektler:
///  - Pickup anında player'a TrapImmunity bileşeni eklenir
///  - Isaac'ın dört yön head sprite'larını TrapImmunity versiyonuyla değiştirir
///  - Player'a TrapImmunity bileşeni kalır; tuzaklara (SpikeTrap vb.)
///    değince hasar almaz
///
/// KURULUM:
///  SpikeTrap.cs içinde isaac.ApplyDamage() satırından önce şunu ekle:
///    var trapImmunity = temas.collider.GetComponent<TrapImmunity>();
///    if (trapImmunity != null && trapImmunity.isActive) return;
/// </summary>
public class TrapImmunity : MonoBehaviour, IItem
{
    [Header("Head Sprites (TrapImmunity versiyonu)")]
    [Tooltip("Yukarı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesUp;

    [Tooltip("Aşağı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesDown;

    [Tooltip("Sol yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesLeft;

    [Tooltip("Sağ yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesRight;

    // SpikeTrap ve diğer tuzak scriptleri bu flag'i kontrol eder
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

        asr.animationSprites = newSprites;
        asr.ResetAnimation(); // ← bunu ekle
    }

    private void ForceRefreshHead(AnimatedSpriteRenderer asr)
    {
        if (asr == null) return;
        asr.enabled = false;
        asr.enabled = true;
    }

    public void Pickup(GameObject player)
    {
        // TrapImmunity bileşenini player'a kopyala
        var immunityOnPlayer = player.GetComponent<TrapImmunity>();
        if (immunityOnPlayer == null)
            immunityOnPlayer = player.AddComponent<TrapImmunity>();

        // Ayarları kopyala
        immunityOnPlayer.headSpritesUp    = headSpritesUp;
        immunityOnPlayer.headSpritesDown  = headSpritesDown;
        immunityOnPlayer.headSpritesLeft  = headSpritesLeft;
        immunityOnPlayer.headSpritesRight = headSpritesRight;

        // Player üzerindeki bileşeni aktifleştir
        immunityOnPlayer.isActive = true;
        Debug.Log($"[TrapImmunity] Pickup çağrıldı. isActive={immunityOnPlayer.isActive}, player={player.name}");

        immunityOnPlayer.ApplyHeadSprites(player);
    }
}