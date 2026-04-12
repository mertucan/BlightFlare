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

    private bool _exploded = false;

    // ─── Explosion layer'ından gelen trigger'ı yakala ──────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
            Explode();
    }

    public void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        if (spawnableItems != null && spawnableItems.Length > 0 && Random.value < itemSpawnChance)
        {
            int randomIndex = Random.Range(0, spawnableItems.Length);
            Instantiate(spawnableItems[randomIndex], transform.position, Quaternion.identity);
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        // ✅ Collider'ı hemen kapat — destructionTime bekleme
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (explosionClips != null && explosionClips.Length > 0 &&
            selectedExplosionClip < explosionClips.Length &&
            explosionClips[selectedExplosionClip] != null)
        {
            AudioSource.PlayClipAtPoint(explosionClips[selectedExplosionClip], transform.position);
        }

        Destroy(gameObject, destructionTime);
    }
}