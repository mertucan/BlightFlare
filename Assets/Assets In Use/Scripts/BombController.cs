using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class BombController : MonoBehaviour
{
    [Header("Bomb")]
    public Key bombKey = Key.Space;
    public GameObject bombPrefab;
    public float bombFuseTime = 3f;
    public int bombAmount = 1;

    [Header("Bobby Bomb")]
    public bool hasBobbyBomb = false;
    public float bobbyMoveSpeed     = 2.5f;
    public float bobbySearchRadius  = 15f;
    public float bobbySearchDelay   = 0.15f;
    public int   bobbySearchRetries = 5;
    public float bobbyRetryInterval = 0.1f;

    [Header("Hot Bomb")]
    public bool hasHotBomb = false;
    public GameObject fireHazardPrefab;

    [Header("Explosion")]
    public Explosion explosionPrefab;
    public LayerMask explosionLayerMask;
    public float explosionDuration = 1f;
    public int explosionRadius = 1;

    [Header("Bob's Curse")]
    public bool hasPoisonCloud = false;
    public GameObject poisonCloudPrefab;

    [Header("Destructible")]
    public Tilemap destructibleTiles;
    public Destructible destructiblePrefab;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip[] explosionClips;
    public int selectedClipIndex = 0;

    [Header("Bomb Placement")]
    [Tooltip("Wall ve Door layer'larını buraya ekleyin. Bomba bu layer'larla çakışan hücreye konmaz.")]
    public LayerMask bombBlockingLayers;

    private int bombsRemaining;
    private bool initialized = false;
    private IsaacMovement isaacMovement;

    // Idle'da da doğru yönü bilmek için son geçerli hareket yönü
    private Vector2 lastFacingDir = Vector2.down;

    private void Start()
    {
        if (!initialized)
        {
            bombsRemaining = bombAmount;
            initialized = true;
        }

        isaacMovement = GetComponent<IsaacMovement>();
        RefreshTilemap(); // Başlangıçta da bul
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        RefreshTilemap();
    }

    private void RefreshTilemap()
    {
        // Sahnedeki Destructible tag'li veya isimli Tilemap'i bul
        Tilemap[] allTilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        foreach (var tm in allTilemaps)
        {
            if (tm.CompareTag("Destructible") || tm.name.Contains("Destructible"))
            {
                destructibleTiles = tm;
                Debug.Log($"[BombController] Tilemap bulundu: {tm.name}");
                return;
            }
        }
        Debug.LogWarning("[BombController] Destructible Tilemap bulunamadı! Tag veya isim 'Destructible' içermeli.");
    }

    private void Update()
    {
        // IsaacMovement.direction sıfır olmadığında güncelle
        if (isaacMovement != null && isaacMovement.direction != Vector2.zero)
            lastFacingDir = isaacMovement.direction;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (bombsRemaining > 0 && kb[bombKey].wasPressedThisFrame)
            StartCoroutine(PlaceBomb());
    }

    private IEnumerator PlaceBomb()
    {

        if (destructibleTiles == null)
        {
            RefreshTilemap();
            if (destructibleTiles == null)
            {
                Debug.LogError("[BombController] Tilemap bulunamadı, bomba konulamadı.");
                yield break;
            }
        }

        Vector3Int originCell = destructibleTiles.WorldToCell(transform.position);
        Vector3    spawnPos   = FindValidBombPosition(originCell, lastFacingDir);

        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[BombController] Bomba için uygun hücre bulunamadı.");
            yield break;
        }

        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        bomb.tag = "Bomb";

        if (hasBobbyBomb)
        {
            if (bomb.GetComponent<Rigidbody2D>() == null)
            {
                var rb2d = bomb.AddComponent<Rigidbody2D>();
                rb2d.gravityScale   = 0f;
                rb2d.freezeRotation = true;
            }

            var bb = bomb.GetComponent<BobbyBombBehaviour>()
                    ?? bomb.AddComponent<BobbyBombBehaviour>();

            bb.moveSpeed     = bobbyMoveSpeed;
            bb.searchRadius  = bobbySearchRadius;
            bb.searchDelay   = bobbySearchDelay;
            bb.searchRetries = bobbySearchRetries;
            bb.retryInterval = bobbyRetryInterval;
            bb.fuseTime      = bombFuseTime;
        }

        if (hasPoisonCloud)
        {
            var bc = bomb.GetComponent<BobsCurseBomb>()
                    ?? bomb.AddComponent<BobsCurseBomb>();
            bc.fuseTime = bombFuseTime;
        }

        bombsRemaining--;

        yield return new WaitForSeconds(bombFuseTime);

        Vector2 explosionPos = bomb != null
            ? (Vector2)bomb.transform.position
            : (Vector2)spawnPos;

        PlayExplosionSound();

        Explosion explosion = Instantiate(explosionPrefab, explosionPos, Quaternion.identity);
        explosion.gameObject.tag = "PlayerBomb";
        explosion.SetActiveRenderer(explosion.start);
        explosion.DestroyAfter(explosionDuration);

        var explodedPositions = new System.Collections.Generic.List<Vector2>();
        explodedPositions.Add(explosionPos);

        Explode(explosionPos, Vector2.up,    explosionRadius, explodedPositions);
        Explode(explosionPos, Vector2.down,  explosionRadius, explodedPositions);
        Explode(explosionPos, Vector2.left,  explosionRadius, explodedPositions);
        Explode(explosionPos, Vector2.right, explosionRadius, explodedPositions);

        NotifySecretWalls(explodedPositions);
        UnlockDoorsInExplosion(explodedPositions);

        Destroy(bomb);

        if (hasHotBomb && fireHazardPrefab != null)
            Instantiate(fireHazardPrefab, explosionPos, Quaternion.identity);

        if (hasPoisonCloud && poisonCloudPrefab != null)
            Instantiate(poisonCloudPrefab, explosionPos, Quaternion.identity);

        bombsRemaining++;
    }

    /// <summary>
    /// Oyuncunun bulunduğu hücreden başlayarak geçerli bir spawn pozisyonu bulur.
    ///
    /// Öncelik sırası:
    ///   1. Oyuncunun tam hücresi
    ///   2. Baktığı yönün TERSİ  ← duvara yapışıkken bomba karşı tarafa gider
    ///   3. Dik eksenler (sol/sağ)
    ///   4. Baktığı yönün kendisi (son çare)
    /// </summary>
    private Vector3 FindValidBombPosition(Vector3Int originCell, Vector2 facingDir)
    {
        Vector3 originWorldPos = destructibleTiles.GetCellCenterWorld(originCell);

        // Oyuncunun tam hücresi temizse direkt kullan
        if (!IsCellBlocked(originWorldPos))
            return originWorldPos;

        // Baktığı yönü kardinal'e snap et
        Vector2 snapped = SnapToCardinal(facingDir);

        // Öncelikli arama sırası
        Vector2[] priority = new Vector2[]
        {
            -snapped,                              // Baktığı yönün TERSİ
            new Vector2(-snapped.y,  snapped.x),  // Sola dik
            new Vector2( snapped.y, -snapped.x),  // Sağa dik
             snapped,                              // Baktığı yön (son çare)
        };

        foreach (var dir in priority)
        {
            Vector3Int neighborCell = originCell + new Vector3Int(
                Mathf.RoundToInt(dir.x),
                Mathf.RoundToInt(dir.y),
                0);

            Vector3 candidatePos = destructibleTiles.GetCellCenterWorld(neighborCell);

            if (!IsCellBlocked(candidatePos))
                return candidatePos;
        }

        return Vector3.zero; // Hiçbir hücre uygun değil
    }

    /// <summary>
    /// Verilen yönü en baskın eksene göre 4 kardinal yöne (Up/Down/Left/Right) snap eder.
    /// </summary>
    private Vector2 SnapToCardinal(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return dir.x >= 0 ? Vector2.right : Vector2.left;
        else
            return dir.y >= 0 ? Vector2.up : Vector2.down;
    }

    /// <summary>
    /// Verilen dünya pozisyonunda bomba koyulmasını engelleyen bir collider var mı?
    /// </summary>
    private bool IsCellBlocked(Vector3 worldPos)
    {
        Collider2D hit = Physics2D.OverlapBox(worldPos, Vector2.one * 0.4f, 0f, bombBlockingLayers);
        if (hit == null) return false;

        bool isBlockingLayer = ((1 << hit.gameObject.layer) & bombBlockingLayers) != 0;
        bool isBlockingTag   = hit.CompareTag("Door") || hit.CompareTag("Wall");

        return isBlockingLayer || isBlockingTag;
    }

    private void NotifySecretWalls(System.Collections.Generic.List<Vector2> positions)
    {
        var allWalls = FindObjectsByType<SecretRoomWall>(FindObjectsSortMode.None);

        foreach (var wall in allWalls)
        {
            if (wall == null) continue;
            Vector2 wallPos = wall.transform.position;
            foreach (var pos in positions)
            {
                if (Vector2.Distance(wallPos, pos) <= 1.5f)
                {
                    wall.TriggerByExplosion();
                    break;
                }
            }
        }
    }

    private void UnlockDoorsInExplosion(System.Collections.Generic.List<Vector2> positions)
    {
        var allDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);

        foreach (var door in allDoors)
        {
            if (door == null) continue;
            if (door.GetComponent<SecretRoomWall>() != null) continue;
            if (door.isIndestructible) continue; // ← Boss kapısını atla

            Vector2 doorPos = door.transform.position;
            foreach (var pos in positions)
            {
                if (Vector2.Distance(doorPos, pos) <= 1.5f)
                {
                    var tracker = door.GetComponentInParent<RoomEnemyTracker>();
                    if (tracker != null)
                        tracker.ForceUnlockDoor(door);
                    else
                        door.SetLocked(false, null);
                    break;
                }
            }
        }
    }

    private void PlayExplosionSound()
    {
        if (audioSource == null || explosionClips.Length == 0) return;
        if (selectedClipIndex < 0 || selectedClipIndex >= explosionClips.Length) return;
        audioSource.PlayOneShot(explosionClips[selectedClipIndex]);
    }

    private void Explode(Vector2 position, Vector2 direction, int length,
        System.Collections.Generic.List<Vector2> explodedPositions = null)
    {
        if (length <= 0) return;
        position += direction;

        Collider2D hit = Physics2D.OverlapBox(
            position, Vector2.one / 2f, 0f, explosionLayerMask);

        if (hit != null)
        {
            ClearDestructible(hit);
            return;
        }

        Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.gameObject.tag = "PlayerBomb";
        explosion.SetActiveRenderer(length > 1 ? explosion.middle : explosion.end);
        explosion.SetDirection(direction);
        explosion.DestroyAfter(explosionDuration);

        explodedPositions?.Add(position);

        Explode(position, direction, length - 1, explodedPositions);
    }

    private void ClearDestructible(Collider2D hit)
    {
        Vector3Int cell = destructibleTiles.WorldToCell(hit.transform.position);
        TileBase   tile = destructibleTiles.GetTile(cell);
        if (tile != null)
        {
            destructibleTiles.SetTile(cell, null);
            return;
        }

        Destructible destructible = hit.GetComponent<Destructible>();
        if (destructible != null)
            destructible.Explode();
    }

    public void AddBomb()
    {
        bombAmount++;
        bombsRemaining++;
    }
}