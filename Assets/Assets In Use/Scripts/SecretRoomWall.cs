using UnityEngine;

public class SecretRoomWall : MonoBehaviour
{
    [Header("Görsel")]
    public GameObject openedDoorPrefab;

    [Header("Ses")]
    public AudioClip[] explosionClips;
    [Range(0, 10)] public int selectedClipIndex = 0;

    [HideInInspector] public Door          linkedDoor;
    [HideInInspector] public int           secretCellIndex = -1;
    [HideInInspector] public int           pairedCellIndex = -1;
    [HideInInspector] public EdgeDirection wallDirection;

    private bool          _revealed = false;
    private BoxCollider2D _solidCol;

    public void Setup(Vector2 size)
    {
        // Collider'ı inceltelim — orijinal size'ın yarısı kadar kalınlık
        Vector2 thinSize = (size.x > size.y)
            ? new Vector2(size.x, 0.35f)   // yatay duvar
            : new Vector2(0.35f, size.y);   // dikey duvar

        _solidCol           = gameObject.AddComponent<BoxCollider2D>();
        _solidCol.isTrigger = false;
        _solidCol.size      = thinSize;
        Debug.Log($"[SecretRoomWall] Setup → GO:{gameObject.name}, size:{thinSize}");
    }

    private void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = null;

        // linkedDoor'un sprite'ını da gizle — normal kapı sprite'ı görünmesin
        if (linkedDoor != null && linkedDoor.spriteRenderer != null)
            linkedDoor.spriteRenderer.enabled = false;

        if (linkedDoor == null)
            Debug.LogError($"[SecretRoomWall] linkedDoor NULL! GO:{gameObject.name}");
        if (openedDoorPrefab == null)
            Debug.LogError($"[SecretRoomWall] openedDoorPrefab NULL! GO:{gameObject.name}");
        if (_solidCol == null)
            Debug.LogError($"[SecretRoomWall] _solidCol NULL! GO:{gameObject.name}");
    }

    public void TriggerByExplosion()
    {
        if (_revealed) return;
        Debug.Log($"[SecretRoomWall] 💥 TriggerByExplosion → dir:{wallDirection}, idx:{secretCellIndex}");
        Reveal();
        NotifyPairedWall();
    }

    private void NotifyPairedWall()
    {
        var allWalls = FindObjectsByType<SecretRoomWall>(FindObjectsSortMode.None);
        foreach (var wall in allWalls)
        {
            if (wall == this || wall._revealed) continue;

            if (wall.secretCellIndex == pairedCellIndex &&
                wall.pairedCellIndex == secretCellIndex)
            {
                Debug.Log($"[SecretRoomWall] Karşı duvar açılıyor → {wall.gameObject.name}");
                wall.Reveal();
                break;
            }
        }
    }

    private void Reveal()
    {
        _revealed = true;

        if (_solidCol != null) _solidCol.enabled = false;

        if (linkedDoor != null)
        {
            bool hasTrigger = false;
            foreach (var col in linkedDoor.GetComponents<BoxCollider2D>())
                if (col.isTrigger) { hasTrigger = true; break; }

            if (!hasTrigger)
            {
                var trigger       = linkedDoor.gameObject.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size      = _solidCol != null ? _solidCol.size : new Vector2(1.6f, 0.15f);
            }

            linkedDoor.SetLocked(false, null);
        }
        else
            Debug.LogError("[SecretRoomWall] linkedDoor NULL!");

        SpawnOpenedDoor();

        if (secretCellIndex >= 0 && Minimap.instance != null)
            Minimap.instance.RevealSecretRoom(secretCellIndex);

        PlayExplosionSound();

        Debug.Log($"[SecretRoomWall] ✅ Açıldı → dir:{wallDirection}, secretIdx:{secretCellIndex}");
    }

    private void SpawnOpenedDoor()
    {
        if (openedDoorPrefab == null || linkedDoor == null) return;

        float zRot = wallDirection switch
        {
            EdgeDirection.Up    =>   0f,
            EdgeDirection.Down  => 180f,
            EdgeDirection.Left  =>  90f,
            EdgeDirection.Right => -90f,
            _                   =>   0f
        };

        var half = RoomManager.instance.RoomInnerHalfSize;

        Transform roomTransform = transform.parent;
        Vector3 roomCenter = roomTransform != null ? roomTransform.position : transform.position;

        float inset = -0.65f;

        Vector3 spawnPos = wallDirection switch
        {
            EdgeDirection.Up    => roomCenter + new Vector3(0f,   half.y + 0.65f, 0f),
            EdgeDirection.Down  => roomCenter + new Vector3(0f,  -half.y - 0.65f, 0f),
            EdgeDirection.Left  => roomCenter + new Vector3(-half.x + inset, 0f, 0f),
            EdgeDirection.Right => roomCenter + new Vector3( half.x - inset, 0f, 0f),
            _                   => roomCenter
        };

        // fineOffset: Up/Down için X=0 olmalı, sadece Y ince ayarı
        Vector3 fineOffset = wallDirection switch
        {
            EdgeDirection.Up    => new Vector3(2.7f,  -0.3f, 0f),
            EdgeDirection.Down  => new Vector3(-2.7f,   0f, 0f),
            EdgeDirection.Left  => new Vector3(0f,   2.7f, 0f),
            EdgeDirection.Right => new Vector3(0f,  -2.7f, 0f),
            _                   => Vector3.zero
        };

        spawnPos += fineOffset;

        var spawned = Instantiate(
            openedDoorPrefab,
            spawnPos,
            Quaternion.Euler(0f, 0f, zRot),
            roomTransform
        );
        spawned.tag = "Door";

        var sr = spawned.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Default";
            sr.sortingOrder     = 6;
        }

        Debug.Log($"[SecretRoomWall] Spawn → spawnPos:{spawnPos}, zRot:{zRot}");
    }

    private void PlayExplosionSound()
    {
        if (explosionClips == null || explosionClips.Length == 0) return;
        int idx = Mathf.Clamp(selectedClipIndex, 0, explosionClips.Length - 1);
        if (explosionClips[idx] == null) return;
        AudioSource.PlayClipAtPoint(explosionClips[idx], transform.position);
    }
}