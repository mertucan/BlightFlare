using UnityEngine;

public class EnemyRoomTrigger : MonoBehaviour
{
    [Header("Oyuncu Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Kapı Collider'ları (opsiyonel)")]
    [SerializeField] private Collider2D[] doorColliders;

    private PooterAI[]  pooters;
    private BabyAI[]    babies;
    private DOF_AI[]    dofEnemies;
    private SuckerAI[]  suckers;
    private HostAI[]    hosts;      // ← YENİ
    private BoomFlyAI[] boomflies; // ← YENİ

    private LokiAI[] lokis;

    private void Awake()
    {
        pooters    = GetComponentsInChildren<PooterAI>(true);
        babies     = GetComponentsInChildren<BabyAI>(true);
        dofEnemies = GetComponentsInChildren<DOF_AI>(true);
        suckers    = GetComponentsInChildren<SuckerAI>(true);
        hosts      = GetComponentsInChildren<HostAI>(true);   // ← YENİ
        boomflies  = GetComponentsInChildren<BoomFlyAI>(true); // ← YENİ
        lokis = GetComponentsInChildren<LokiAI>(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        SetEnemiesActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        SetEnemiesActive(false);
    }

    private void SetEnemiesActive(bool active)
    {
        foreach (var p in pooters)    if (p != null) p.SetActive(active);
        foreach (var b in babies)     if (b != null) b.SetActive(active);
        foreach (var d in dofEnemies) if (d != null) d.SetActive(active);
        foreach (var s in suckers)    if (s != null) s.SetActive(active);
        foreach (var h in hosts)      if (h != null) h.SetActive(active);  // ← YENİ
        foreach (var bf in boomflies) if (bf != null) bf.SetActive(active); // ← YENİ
        foreach (var l in lokis) if (l != null) l.SetActive(active);
    }
}