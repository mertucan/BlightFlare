using UnityEngine;

/// <summary>
/// Pyromaniac item'ı.
/// Efektler:
///  - Pickup anında +1 bomba ekler (BobsCurse ile aynı mantık)
///  - Isaac'ın dört yön head sprite'larını Pyromaniac versiyonuyla değiştirir
///  - Player'a Pyromaniac bileşeni kalır; bomba patlaması hasar yerine
///    yarım kalp iyileştirir
///
/// KURULUM:
///  BombController.cs içinde Destroy(bomb) satırından sonra şunu ekle:
///    var pyro = GetComponent&lt;Pyromaniac&gt;();
///    if (pyro != null && pyro.isActive)
///        pyro.HealFromExplosion(gameObject);
/// </summary>
public class Pyromaniac : MonoBehaviour, IItem
{
    [Header("Bomba Ayarları")]
    [Tooltip("+1 bomba ekler — pickup anında uygulanır.")]
    public int bonusBombs = 1;

    [Header("Head Sprites (Pyromaniac versiyonu)")]
    [Tooltip("Yukarı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesUp;

    [Tooltip("Aşağı yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesDown;

    [Tooltip("Sol yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesLeft;

    [Tooltip("Sağ yön head sprite dizisi (öncekinin YERİNE geçer).")]
    public Sprite[] headSpritesRight;

    // Bomba scripti bu flag'i kontrol eder
    [HideInInspector] public bool isActive = false;

    // ── Bomba yükseltmesi ─────────────────────────────────────────────────
    public void ApplyBombUpgrade(GameObject player)
    {
        var bombCtrl = player.GetComponent<BombController>();
        if (bombCtrl == null) return;

        for (int i = 0; i < bonusBombs; i++)
            bombCtrl.AddBomb();
    }

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

    public void Pickup(GameObject player)
    {
        // Pyromaniac bileşenini player'a kopyala, bu objeden sil
        var pyroOnPlayer = player.GetComponent<Pyromaniac>();
        if (pyroOnPlayer == null)
            pyroOnPlayer = player.AddComponent<Pyromaniac>();

        // Ayarları kopyala
        pyroOnPlayer.bonusBombs       = bonusBombs;
        pyroOnPlayer.headSpritesUp    = headSpritesUp;
        pyroOnPlayer.headSpritesDown  = headSpritesDown;
        pyroOnPlayer.headSpritesLeft  = headSpritesLeft;
        pyroOnPlayer.headSpritesRight = headSpritesRight;

        // Artık player üzerindeki bileşeni aktifleştir
        pyroOnPlayer.isActive = true;
        Debug.Log($"[Pyromaniac] Pickup çağrıldı. isActive={pyroOnPlayer.isActive}, player={player.name}");

        pyroOnPlayer.ApplyBombUpgrade(player);
        pyroOnPlayer.ApplyHeadSprites(player);
    }

    [HideInInspector] public bool hasHealedThisExplosion = false;

    public System.Collections.IEnumerator ResetHealFlag(float duration)
    {
        yield return new WaitForSeconds(duration);
        hasHealedThisExplosion = false;
    }

    public void HealFromExplosion(GameObject player)
    {
        var health = player.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (health.currentHearts >= health.maxHearts)
        {
            Debug.Log("[Pyromaniac] Max can, iyileştirme yapılmadı.");
            return;
        }

        Debug.Log($"[Pyromaniac] Heal öncesi currentHearts={health.currentHearts}");
        health.Heal(1);
        Debug.Log($"[Pyromaniac] Heal sonrası currentHearts={health.currentHearts}");
    }
}