using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Oyuncu Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Tunnel Nesneleri")]
    [SerializeField] private SpriteRenderer tunnelSpriteRenderer;
    [SerializeField] private Collider2D tunnelCollider2D;

    [Header("Boss Intro")]
    [SerializeField] private Sprite introPhoto;
    [SerializeField] private Sprite isaacIntroPhoto;
    [SerializeField] private Sprite maggyIntroPhoto;
    [SerializeField] private Sprite bombermanIntroPhoto;
    [SerializeField] private AudioClip introMusic;
    [SerializeField] [Range(0f, 1f)] private float introMusicVolume = 1f;
    [SerializeField] private float introFadeToBlackDuration = 2f;

    [Header("Boss Music")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] [Range(0f, 1f)] private float bossMusicVolume = 1f;

    private DOF_AI       dofAI;
    private LokiAI       lokiAI;
    private HollowHead   hollowAI;
    private NightWatchAI nightWatchAI;
    private MaskAI       maskAI;
    private HeartAI      heartAI;
    private bool         triggered = false;

    private RoomEnemyTracker roomTracker;

    private void Awake()
    {
        dofAI        = GetComponentInChildren<DOF_AI>();
        lokiAI       = GetComponentInChildren<LokiAI>();
        hollowAI     = GetComponentInChildren<HollowHead>();
        nightWatchAI = GetComponentInChildren<NightWatchAI>();
        maskAI       = GetComponentInChildren<MaskAI>();
        heartAI      = GetComponentInChildren<HeartAI>();

        roomTracker = GetComponentInParent<RoomEnemyTracker>();
        if (roomTracker == null)
            roomTracker = FindFirstObjectByType<RoomEnemyTracker>();

        if (nightWatchAI != null)
            nightWatchAI.SetManagedDoors(null, this);

        if (heartAI != null)
        {
            heartAI.SetBossRoomTrigger(this);
            if (maskAI != null) heartAI.SetMaskAI(maskAI);
        }
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

        Debug.Log("[BossRoomTrigger] Tetiklendi.");

        // Önce kapıları kapat
        if (roomTracker != null)
            roomTracker.OnPlayerEntered();

        BackgroundMusicManager.instance?.PauseForBossIntro();
        BossIntroOverlay.Play(GetSelectedCharacterIntroPhoto(), introMusic, introMusicVolume, introFadeToBlackDuration, ActivateBoss);

        // Boss'ları aktive et
    }

    private Sprite GetSelectedCharacterIntroPhoto()
    {
        int selectedCharacterIndex = PlayerPrefs.GetInt(CharacterSelectionManager.SelectedCharacterKey, 0);

        Sprite selectedPhoto = selectedCharacterIndex switch
        {
            0 => isaacIntroPhoto,
            1 => maggyIntroPhoto,
            2 => bombermanIntroPhoto,
            _ => null
        };

        return selectedPhoto != null ? selectedPhoto : introPhoto;
    }

    private void ActivateBoss()
    {
        BackgroundMusicManager.instance?.PlayBossMusic(bossMusic, bossMusicVolume);

        if (dofAI != null)
            dofAI.Activate();
        else if (lokiAI != null)
            lokiAI.Activate();
        else if (hollowAI != null)
            hollowAI.Activate();
        else if (nightWatchAI != null)
            nightWatchAI.Activate();
        else if (maskAI != null || heartAI != null)
        {
            if (maskAI  != null) maskAI.Activate();
            if (heartAI != null) heartAI.Activate();
        }
        else
            Debug.LogWarning("[BossRoomTrigger] Hiçbir boss AI bulunamadı!");
    }

    public void OnBossDefeated()
    {
        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = true;
        if (tunnelCollider2D     != null) tunnelCollider2D.enabled     = true;

        if (roomTracker != null)
            roomTracker.OnPlayerExited();
    }
}
