using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Tooltip("Her temasta kaç half-heart hasar verilsin?")]
    public int damageAmount = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
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