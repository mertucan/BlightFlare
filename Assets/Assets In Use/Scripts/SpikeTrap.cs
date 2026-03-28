using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D temas)
    {
        if (temas.CompareTag("Player"))
        {
            IsaacMovement isaac = temas.GetComponent<IsaacMovement>();
            
            if (isaac != null)
            {
                isaac.DeathSequence();
            }
        }
    }
}
