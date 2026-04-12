using UnityEngine;

/// <summary>
/// Tüm item'ların implement etmesi gereken temel arayüz.
/// Item'ı alınca Pickup() çağrılır.
/// </summary>
public interface IItem
{
    void Pickup(GameObject player);
}