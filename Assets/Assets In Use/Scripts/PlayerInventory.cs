using System.Collections;
using UnityEngine;

/// <summary>
/// Oyuncunun anahtar ve para miktarını tutar.
/// HeartUI üzerinden ekranı otomatik günceller.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public int keys { get; private set; } = 1;
    public int pennies { get; private set; } = 1;

    private HeartUI _heartUI;

    public static PlayerInventory instance;

    private void Awake()
    {
        instance = this;
        _heartUI = FindFirstObjectByType<HeartUI>(FindObjectsInactive.Include);
    }

    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();

        if (_heartUI == null)
            Debug.LogError("HATA: HeartUI bulunamadı!");
        else
        {
            _heartUI.UpdateKey(keys);
            _heartUI.UpdatePennies(pennies);
        }
    }

    public void AddKey(int amount = 1)
    {
        keys += amount;
        _heartUI?.UpdateKey(keys);
    }

    /// <summary>
    /// Pozitif: para ekler. Negatif: para düşer (shop harcamaları için).
    /// Sonuç 0'ın altına düşmez.
    /// </summary>
    public void AddPenny(int amount = 1)
    {
        pennies = Mathf.Max(0, pennies + amount);
        _heartUI?.UpdatePennies(pennies);
    }

    public bool UseKey()
    {
        if (keys <= 0) return false;
        keys--;
        _heartUI?.UpdateKey(keys);
        return true;
    }
}