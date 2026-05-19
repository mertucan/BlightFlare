using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Tooltip("Her temasta kaç half-heart hasar verilsin?")]
    public int damageAmount = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Bomba bu tuzaktan geçemesin
        if (other.CompareTag("Bomb") && CompareTag("BombBlockerTrap"))
        {
            // Fiziksel durdurmak için Trigger yerine solid collider gerekir
            // Ama en azından bomba interaction'ını buradan yönetebiliriz
            var bomb = other.GetComponent<BombController>();
            return;
        }

        if (!other.CompareTag("Player")) return;

        var trapImmunity = other.GetComponent<TrapImmunity>();
        if (trapImmunity != null && trapImmunity.isActive)
        {
            Debug.Log("[SpikeTrap] TrapImmunity aktif, hasar engellendi.");
            return;
        }

        IsaacMovement isaac = other.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(damageAmount);
    }
}