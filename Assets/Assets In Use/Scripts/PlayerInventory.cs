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
        // Eğer HeartUI inaktifse bile bulabilmesi için parametre ekledik:
        _heartUI = FindFirstObjectByType<HeartUI>(FindObjectsInactive.Include);
    }

    private IEnumerator Start()
    {
        // Diğer tüm scriptlerin (özellikle HeartUI'ın) Start metotlarının bitmesini bekle
        yield return new WaitForEndOfFrame();

        // HeartUI bulundu mu diye kontrol et
        if (_heartUI == null)
        {
            Debug.LogError("HATA: HeartUI bulunamadı! Sahnede HeartUI scripti olan bir obje olduğundan emin ol.");
        }
        else
        {
            Debug.Log("Başarı: HeartUI bulundu, değerler güncelleniyor...");
            _heartUI.UpdateKey(keys);
            _heartUI.UpdatePennies(pennies);
        }
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

    public bool UseKey()
    {
        if (keys <= 0) return false;
        keys--;
        _heartUI?.UpdateKey(keys);
        return true;
    }
}