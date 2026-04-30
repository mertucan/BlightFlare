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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_exploded) return;
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion"))
        {
            if (other.GetComponent<PooterProjectile>() != null) return;
            if (other.GetComponent<BabyProjectile>() != null) return;
            Explode();
        }
    }

    public void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        if (spawnableItems != null && spawnableItems.Length > 0 && Random.value < itemSpawnChance)
        {
            GameObject[] validItems = GetValidSpawnableItems();
            if (validItems.Length > 0)
            {
                int randomIndex = Random.Range(0, validItems.Length);
                Instantiate(validItems[randomIndex], transform.position, Quaternion.identity);
            }
        }

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

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

    private GameObject[] GetValidSpawnableItems()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        bool blastRadiusCapped = false;

        if (player != null && player.TryGetComponent<BombController>(out BombController bc))
            blastRadiusCapped = bc.explosionRadius >= 3;

        var valid = new System.Collections.Generic.List<GameObject>();
        foreach (var item in spawnableItems)
        {
            if (item == null) continue;

            if (blastRadiusCapped
                && item.TryGetComponent<ItemPickup>(out ItemPickup ip)
                && ip.type == ItemPickup.ItemType.BlastRadius)
                continue;

            valid.Add(item);
        }
        return valid.ToArray();
    }
}