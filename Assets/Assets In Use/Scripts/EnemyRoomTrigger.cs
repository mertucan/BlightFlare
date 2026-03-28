using UnityEngine;

public class EnemyRoomTrigger : MonoBehaviour
{
    [Header("Oyuncu Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Kapı Collider'ları (opsiyonel)")]
    [SerializeField] private Collider2D[] doorColliders;

    private bool triggered = false;

    private PooterAI[]  pooters;
    private BabyAI[]    babies;
    private DOF_AI[]    dofEnemies;

    private void Awake()
    {
        // Tüm düşmanları child'lardan topla
        pooters    = GetComponentsInChildren<PooterAI>(true);
        babies     = GetComponentsInChildren<BabyAI>(true);
        dofEnemies = GetComponentsInChildren<DOF_AI>(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        // Kapıları kapat (boss odası gibi davranmak istersen)
        foreach (var door in doorColliders)
            if (door != null) door.enabled = true;

        // Tüm düşmanları uyandır
        foreach (var p in pooters)    if (p != null) p.Activate();
        foreach (var b in babies)     if (b != null) b.Activate();
        foreach (var d in dofEnemies) if (d != null) d.Activate();
    }
}