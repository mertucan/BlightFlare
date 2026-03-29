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
    private int bombsRemaining;

    [Header("Explosion")]
    public Explosion explosionPrefab;
    public LayerMask explosionLayerMask;
    public float explosionDuration = 1f;
    public int explosionRadius = 1;

    [Header("Destructible")]
    public Tilemap destructibleTiles;
    public Destructible destructiblePrefab;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip[] explosionClips; // İstediğin kadar ses ekle
    public int selectedClipIndex = 0;  // Inspector'dan hangisinin çalacağını seç

    private void OnEnable()
    {
        bombsRemaining = bombAmount;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (bombsRemaining > 0 && kb[bombKey].wasPressedThisFrame) {
            StartCoroutine(PlaceBomb());
        }
    }

    private IEnumerator PlaceBomb()
    {
        Vector3Int cell = destructibleTiles.WorldToCell(transform.position);
        Vector3 spawnPos = destructibleTiles.GetCellCenterWorld(cell);

        GameObject bomb = Instantiate(bombPrefab, spawnPos, Quaternion.identity);
        bomb.tag = "Bomb";

        bombsRemaining--;

        yield return new WaitForSeconds(bombFuseTime);

        Vector2 explosionPos = bomb != null ? bomb.transform.position : (Vector2)spawnPos;

        // 💥 Patlama anında ses çal
        PlayExplosionSound();

        Explosion explosion = Instantiate(explosionPrefab, explosionPos, Quaternion.identity);
        explosion.SetActiveRenderer(explosion.start);
        explosion.DestroyAfter(explosionDuration);

        Explode(explosionPos, Vector2.up, explosionRadius);
        Explode(explosionPos, Vector2.down, explosionRadius);
        Explode(explosionPos, Vector2.left, explosionRadius);
        Explode(explosionPos, Vector2.right, explosionRadius);

        Destroy(bomb);
        bombsRemaining++;
    }

    private void PlayExplosionSound()
    {
        if (audioSource == null || explosionClips.Length == 0) return;
        if (selectedClipIndex < 0 || selectedClipIndex >= explosionClips.Length) return;

        audioSource.PlayOneShot(explosionClips[selectedClipIndex]);
    }

    private void Explode(Vector2 position, Vector2 direction, int length)
    {
        if (length <= 0) return;
        position += direction;

        Collider2D hit = Physics2D.OverlapBox(position, Vector2.one / 2f, 0f, explosionLayerMask);
        if (hit != null)
        {
            ClearDestructible(hit);
            return;
        }

        Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.SetActiveRenderer(length > 1 ? explosion.middle : explosion.end);
        explosion.SetDirection(direction);
        explosion.DestroyAfter(explosionDuration);

        Explode(position, direction, length - 1);
    }

    private void ClearDestructible(Collider2D hit)
    {
        Vector3Int cell = destructibleTiles.WorldToCell(hit.transform.position);
        TileBase tile = destructibleTiles.GetTile(cell);
        if (tile != null)
        {
            destructibleTiles.SetTile(cell, null);
            return;
        }

        Destructible destructible = hit.GetComponent<Destructible>();
        if (destructible != null)
        {
            destructible.Explode();
        }
    }

    public void AddBomb()
    {
        bombAmount++;
        bombsRemaining++;
    }
}