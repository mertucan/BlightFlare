using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Tooltip("Her temaста kaç half-heart hasar verilsin?")]
    public int damageAmount = 1;

    private void OnTriggerEnter2D(Collider2D temas)
    {
        if (!temas.CompareTag("Player")) return;

        IsaacMovement isaac = temas.GetComponent<IsaacMovement>();
        if (isaac != null) isaac.ApplyDamage(damageAmount);
    }
}