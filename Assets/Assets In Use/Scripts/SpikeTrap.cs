using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Tooltip("Her temasta kaç half-heart hasar verilsin?")]
    public int damageAmount = 1;

    private void OnCollisionEnter2D(Collision2D temas)
    {
        if (!temas.collider.CompareTag("Player")) return;

        IsaacMovement isaac = temas.collider.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(damageAmount);
    }
}