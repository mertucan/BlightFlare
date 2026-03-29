using UnityEngine;

public class Destructible : MonoBehaviour
{
    public float destructionTime = 1f;
    [Range(0f, 1f)]
    public float itemSpawnChance = 0.2f;
    public GameObject[] spawnableItems;

    [Header("Sounds")]
    public AudioClip[] explosionClips;
    public int selectedExplosionClip = 0;

    public void Explode()
    {
        if (spawnableItems.Length > 0 && Random.value < itemSpawnChance)
        {
            int randomIndex = Random.Range(0, spawnableItems.Length);
            Instantiate(spawnableItems[randomIndex], transform.position, Quaternion.identity);
        }

        // Sprite'ı hemen gizle
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // Collider'ı hemen kapat
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Ses hemen çal
        if (explosionClips != null && explosionClips.Length > 0 &&
            selectedExplosionClip < explosionClips.Length &&
            explosionClips[selectedExplosionClip] != null)
        {
            AudioSource.PlayClipAtPoint(explosionClips[selectedExplosionClip], transform.position);
        }

        Destroy(gameObject, destructionTime);
    }

    private void OnDestroy() { }
}