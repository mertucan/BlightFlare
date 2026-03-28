using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("Oyuncu Tag")]
    [SerializeField] private string playerTag = "Player";

    [Header("Tunnel Nesneleri")]
    [SerializeField] private GameObject tunnelSprite;
    [SerializeField] private GameObject tunnelCollider;

    private DOF_AI dofAI;
    private Collider2D[] doorColliders;
    private bool triggered = false;

    [SerializeField] private SpriteRenderer tunnelSpriteRenderer;
    [SerializeField] private Collider2D tunnelCollider2D;

    private void Start()
    {
        if (tunnelSpriteRenderer != null) tunnelSpriteRenderer.enabled = false;
        if (tunnelCollider2D != null)     tunnelCollider2D.enabled = false;
    }

    private void Awake()
    {
        dofAI = GetComponentInChildren<DOF_AI>();

        doorColliders = new Collider2D[4];
        string[] doorNames = { "TopLeft", "TopRight", "BottomLeft", "BottomRight" };

        for (int i = 0; i < doorNames.Length; i++)
        {
            Transform door = transform.Find(doorNames[i]);
            if (door != null)
                doorColliders[i] = door.GetComponent<Collider2D>();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag(playerTag)) return;

        triggered = true;

        foreach (var door in doorColliders)
            if (door != null) door.enabled = true;

        if (dofAI != null)
            dofAI.Activate();
        else
            Debug.LogWarning("BossRoomTrigger: Duke of Flies bulunamadı!");
    }
}