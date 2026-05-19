using UnityEngine;

public class EnemyRoomTrigger : MonoBehaviour
{
    [Header("Oyuncu Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Kapı Collider'ları (opsiyonel)")]
    [SerializeField] private Collider2D[] doorColliders;

    private PooterAI[]    pooters;
    private BabyAI[]      babies;
    private DOF_AI[]      dofEnemies;
    private SuckerAI[]    suckers;
    private HostAI[]      hosts;
    private BoomFlyAI[]   boomflies;
    private LokiAI[]      lokis;
    private HollowHead[]  hollows;   // ← YENİ
    private NightWatchAI[] nightWatches; // ← YENİ

    private bool activated = false;  // ← Sadece bir kez Activate() çağır

    private void Awake()
    {
        pooters    = GetComponentsInChildren<PooterAI>(true);
        babies     = GetComponentsInChildren<BabyAI>(true);
        dofEnemies = GetComponentsInChildren<DOF_AI>(true);
        suckers    = GetComponentsInChildren<SuckerAI>(true);
        hosts      = GetComponentsInChildren<HostAI>(true);
        boomflies  = GetComponentsInChildren<BoomFlyAI>(true);
        lokis      = GetComponentsInChildren<LokiAI>(true);
        hollows    = GetComponentsInChildren<HollowHead>(true); // ← YENİ
        nightWatches = GetComponentsInChildren<NightWatchAI>(true); // ← YENİ
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (!activated)
        {
            activated = true;
            // İlk girişte Activate() — hareketi başlatır
            foreach (var h in hollows) if (h != null) h.Activate();
            foreach (var nw in nightWatches) if (nw != null) nw.Activate();
        }
        else
        {
            // Odaya geri dönünce sadece aç
            foreach (var h in hollows) if (h != null) h.SetActive(true);
        }

        SetEnemiesActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        foreach (var h in hollows) if (h != null) h.SetActive(false);
        SetEnemiesActive(false);
    }

    private void SetEnemiesActive(bool active)
    {
        foreach (var p in pooters)    if (p != null) p.SetActive(active);
        foreach (var b in babies)     if (b != null) b.SetActive(active);
        foreach (var d in dofEnemies) if (d != null) d.SetActive(active);
        foreach (var s in suckers)    if (s != null) s.SetActive(active);
        foreach (var h in hosts)      if (h != null) h.SetActive(active);
        foreach (var bf in boomflies) if (bf != null) bf.SetActive(active);
        foreach (var l in lokis)      if (l != null) l.SetActive(active);
        foreach (var h in hollows)    if (h != null) h.SetActive(active); // ← YENİ
        foreach (var nw in nightWatches) if (nw != null) nw.SetActive(active); // ← YENİ
    }
}