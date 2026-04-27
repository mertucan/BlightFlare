using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Tooltip("Her temasta kaç half-heart hasar verilsin?")]
    public int damageAmount = 1;

    private void OnCollisionEnter2D(Collision2D temas)
    {
        if (!temas.collider.CompareTag("Player")) return;

        // TrapImmunity kontrolü — item aktifse hasarı atla
        var trapImmunity = temas.collider.GetComponent<TrapImmunity>();
        if (trapImmunity != null && trapImmunity.isActive)
        {
            Debug.Log("[SpikeTrap] TrapImmunity aktif, hasar engellendi.");
            return;
        }

        IsaacMovement isaac = temas.collider.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(damageAmount);
    }
}