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

    private int bombsRemaining;
    private bool initialized = false;

    private void Start()
    {
        if (!initialized)
        {
            bombsRemaining = bombAmount;
            initialized = true;
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (bombsRemaining > 0 && kb[bombKey].wasPressedThisFrame)
            StartCoroutine(PlaceBomb());
    }

    private IEnumerator PlaceBomb()
    {
        Vector3Int cell     = destructibleTiles.WorldToCell(transform.position);
        Vector3    spawnPos = destructibleTiles.GetCellCenterWorld(cell);

        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        bomb.tag = "Bomb";

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

        // Patlama noktalarını topla, SecretRoomWall'ları bildir
        var explodedPositions = new System.Collections.Generic.List<Vector2>();
        explodedPositions.Add(explosionPos);

        Explode(explosionPos, Vector2.up,    explosionRadius, explodedPositions);
        Explode(explosionPos, Vector2.down,  explosionRadius, explodedPositions);
        Explode(explosionPos, Vector2.left,  explosionRadius, explodedPositions);
        Explode(explosionPos, Vector2.right, explosionRadius, explodedPositions);

        // Tüm patlama noktalarına yakın SecretRoomWall'ları tetikle
        NotifySecretWalls(explodedPositions);
        UnlockDoorsInExplosion(explodedPositions);

        Destroy(bomb);

        if (hasPoisonCloud && poisonCloudPrefab != null)
            Instantiate(poisonCloudPrefab, explosionPos, Quaternion.identity);

        bombsRemaining++;
    }

    /// <summary>
    /// Patlama noktaları listesine yakın tüm SecretRoomWall'ları bulur ve tetikler.
    /// </summary>
    private void NotifySecretWalls(System.Collections.Generic.List<Vector2> positions)
    {
        // Sahnedeki tüm SecretRoomWall'ları bul
        var allWalls = FindObjectsByType<SecretRoomWall>(FindObjectsSortMode.None);

        foreach (var wall in allWalls)
        {
            if (wall == null) continue;

            Vector2 wallPos = wall.transform.position;

            foreach (var pos in positions)
            {
                // Patlama noktasına yeterince yakınsa tetikle
                // explosionRadius * ~oda tile boyutu — biraz toleranslı tut
                if (Vector2.Distance(wallPos, pos) <= 1.5f)
                {
                    Debug.Log($"[BombController] SecretRoomWall tetiklendi → " +
                              $"wall:{wall.gameObject.name}, wallPos:{wallPos}, explosionPos:{pos}");
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

            // Gizli oda duvarlarını bu sistemle açma — onları SecretRoomWall yönetiyor
            if (door.GetComponent<SecretRoomWall>() != null) continue;

            Vector2 doorPos = door.transform.position;

            foreach (var pos in positions)
            {
                if (Vector2.Distance(doorPos, pos) <= 1.5f)
                {
                    // Odanın RoomEnemyTracker'ını bul ve kapıyı listeden çıkar
                    var tracker = door.GetComponentInParent<RoomEnemyTracker>();
                    if (tracker != null)
                        tracker.ForceUnlockDoor(door);
                    else
                        door.SetLocked(false, null);

                    Debug.Log($"[BombController] Kapı bombayla açıldı → {door.gameObject.name}");
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