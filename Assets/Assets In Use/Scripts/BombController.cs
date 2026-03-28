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

        Vector2 explosionPos = Vector2.zero;

        if (bomb != null) 
        {
            explosionPos = bomb.transform.position;
        }
        else
        {
            explosionPos = spawnPos;
        }

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

    private void Explode(Vector2 position, Vector2 direction, int length)
    {
        if (length <= 0) return;
        position += direction;

        if (Physics2D.OverlapBox(position, Vector2.one / 2f, 0f, explosionLayerMask))
        {
            ClearDestructible(position);
            return;
        }

        Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.SetActiveRenderer(length > 1 ? explosion.middle : explosion.end);
        explosion.SetDirection(direction);
        explosion.DestroyAfter(explosionDuration);

        Explode(position, direction, length - 1);
    }

    private void ClearDestructible(Vector2 position)
    {
        Vector3Int cell = destructibleTiles.WorldToCell(position);
        TileBase tile = destructibleTiles.GetTile(cell);

        if (tile != null)
        {
            Destructible destructible = Instantiate(destructiblePrefab, position, Quaternion.identity);
            destructible.Explode(); // ← Bunu ekle
            destructibleTiles.SetTile(cell, null);
        }
    }

    public void AddBomb()
    {
        bombAmount++;
        bombsRemaining++;
    }
}
