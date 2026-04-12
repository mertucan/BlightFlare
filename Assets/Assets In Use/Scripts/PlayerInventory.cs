using UnityEngine;

/// <summary>
/// Oyuncunun anahtar ve para miktarını tutar.
/// HeartUI üzerinden ekranı otomatik günceller.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public int keys    { get; private set; } = 0;
    public int pennies { get; private set; } = 0;

    private HeartUI _heartUI;

    private void Awake()
    {
        _heartUI = FindFirstObjectByType<HeartUI>();
    }

    public void AddKey(int amount = 1)
    {
        keys += amount;
        _heartUI?.UpdateKey(keys);
    }

    public void AddPenny(int amount = 1)
    {
        pennies += amount;
        _heartUI?.UpdatePennies(pennies);
    }
}