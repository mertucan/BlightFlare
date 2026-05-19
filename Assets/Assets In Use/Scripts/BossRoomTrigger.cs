using UnityEngine;
using System.Linq;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Oyuncu Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Tunnel Nesneleri")]
    [SerializeField] private SpriteRenderer tunnelSpriteRenderer;
    [SerializeField] private Collider2D tunnelCollider2D;

    private DOF_AI       dofAI;
    private LokiAI       lokiAI;
    private HollowHead   hollowAI;
    private NightWatchAI nightWatchAI;
    private bool triggered = false;

    // Kapı yönetimi RoomEnemyTracker üzerinden
    private RoomEnemyTracker roomTracker;

    private void Awake()
    {
        dofAI        = GetComponentInChildren<DOF_AI>();
        lokiAI       = GetComponentInChildren<LokiAI>();
        hollowAI     = GetComponentInChildren<HollowHead>();
        nightWatchAI = GetComponentInChildren<NightWatchAI>();

        // BossRoomTrigger bir Room'un child'ı olarak spawn ediliyor,
        // RoomEnemyTracker aynı Room objesinde
        roomTracker = GetComponentInParent<RoomEnemyTracker>();
        if (roomTracker == null)
            roomTracker = FindFirstObjectByType<RoomEnemyTracker>();

        if (nightWatchAI != null)
            nightWatchAI.SetManagedDoors(null, this);
    }

    private void Start()
    {
        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = false;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;
        triggered = true;

        // Kapıları RoomEnemyTracker üzerinden kilitle
        if (roomTracker != null)
            roomTracker.OnPlayerEntered();

        if (dofAI != null)
            dofAI.Activate();
        else if (lokiAI != null)
            lokiAI.Activate();
        else if (hollowAI != null)
            hollowAI.Activate();
        else if (nightWatchAI != null)
            nightWatchAI.Activate();
        else
            Debug.LogWarning("BossRoomTrigger: Hiçbir boss AI bulunamadı!");
    }

    // NightWatchAI ölünce çağrılır
    public void OnBossDefeated()
    {
        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = true;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = true;

        // Kapıları RoomEnemyTracker üzerinden aç
        if (roomTracker != null)
            roomTracker.OnPlayerExited(); // hasEnemies false olduğu için kapılar açılır
    }
}