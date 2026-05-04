using UnityEngine;

/// <summary>
/// Bob's Curse item'ı.
/// Efektler:
///  - BombController'a +1 bomba ekler (aynı anda 2 bomba bırakılabilir)
///  - BombController'ın bombPrefab'ını Bob's Curse bombası ile değiştirir
///  - BombController'a poisonCloudPrefab atar
///  - Isaac'ın dört yön head sprite'larını Bob's Curse versiyonuyla değiştirir
///    (önceki sprite'lar kaldırılır, sadece yeni set kalır)
/// </summary>
public class BobsCurse : MonoBehaviour, IItem
{
    [Header("Bomb Override")]
    [Tooltip("Bob's Curse bombası prefab'ı (BobsCurseBomb bileşeni olmalı).")]
    public GameObject bobsCurseBombPrefab;

    [Tooltip("Patlama sonrası zemine bırakılacak zehir bulutu prefab'ı.")]
    public GameObject poisonCloudPrefab;

    [Tooltip("+1 bomba ekler — aynı anda 2 bomba kullanılabilir olur.")]
    public int bonusBombs = 1;

    [Header("Head Sprites (Bob's Curse versiyonu)")]
    [Tooltip("Yukarı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesUp;

    [Tooltip("Aşağı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesDown;

    [Tooltip("Sol yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesLeft;

    [Tooltip("Sağ yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesRight;

    // ── IItem ──────────────────────────────────────────────────────────────
    public void Pickup(GameObject player)
    {
        ApplyBombUpgrade(player);
        ApplyHeadSprites(player);
    }

    // ── Bomba yükseltmesi ─────────────────────────────────────────────────
    private void ApplyBombUpgrade(GameObject player)
    {
        var bombCtrl = player.GetComponent<BombController>();
        if (bombCtrl == null) return;

        // +1 bomba (AddBomb hem bombAmount hem bombsRemaining artırır)
        for (int i = 0; i < bonusBombs; i++)
            bombCtrl.AddBomb();

        if (bobsCurseBombPrefab != null)
            bombCtrl.bombPrefab = bobsCurseBombPrefab;

        if (poisonCloudPrefab != null)
        {
            bombCtrl.poisonCloudPrefab = poisonCloudPrefab;
            bombCtrl.hasPoisonCloud    = true;
        }
    }

    // ── Head sprite değiştirme ────────────────────────────────────────────
    /// <summary>
    /// Önceki head sprite dizisini tamamen siler, yeni diziyle değiştirir.
    /// Böylece eski kafanın sprite'ları görünmez olur.
    /// </summary>
    private void ApplyHeadSprites(GameObject player)
    {
        var movement = player.GetComponent<IsaacMovement>();
        if (movement == null) return;

        ReplaceHeadSprites(movement.spritesUp.head,    headSpritesUp);
        ReplaceHeadSprites(movement.spritesDown.head,  headSpritesDown);
        ReplaceHeadSprites(movement.spritesLeft.head,  headSpritesLeft);
        ReplaceHeadSprites(movement.spritesRight.head, headSpritesRight);

        // Aktif animasyonu yenile — mevcut yönün head AnimatedSpriteRenderer'ını
        // kısa süre kapat/aç; bu, çerçeveyi sıfırlayarak yeni sprite'ı gösterir.
        ForceRefreshHead(movement.spritesDown.head);
        ForceRefreshHead(movement.spritesUp.head);
        ForceRefreshHead(movement.spritesLeft.head);
        ForceRefreshHead(movement.spritesRight.head);
    }

    /// <summary>
    /// AnimatedSpriteRenderer'ın sprite dizisini tamamen yenisiyle değiştirir.
    /// Eski sprite referansları array'den kaldırılır.
    /// </summary>
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
}