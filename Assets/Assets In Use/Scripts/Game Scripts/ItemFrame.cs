using System.Collections;
using UnityEngine;

/// <summary>
/// Isaac tarzı item alma.
///
/// GEÇİLEMEZLİK:
///   ItemFrame'e bir Rigidbody2D ekle → Body Type = Static yapın.
///   Static Rigidbody2D + non-trigger Collider2D = fizik motoru player'ı geçirmez.
///   Kod, Rigidbody2D yoksa otomatik ekler.
///
/// PICKUP:
///   Player collider'a girince item alınır. Küçük yukarı animasyon → Destroy.
///   Item alındıktan sonra da frame solid durmaya devam eder (sadece collider kapanır
///   istersen; istersan devam etsin — aşağıdaki ayarla kontrol edebilirsin).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemFrame : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Hierarchy'deki Item child objesi. Boş bırakılırsa 'Item' adlı child aranır.")]
    public GameObject itemObject;

    [Header("Visual Feedback")]
    public Sprite emptyFrameSprite;

    [Header("Pickup Sound")]
    public AudioSource audioSource;
    public AudioClip   pickupClip;

    [Header("Pickup Animation")]
    public float riseDistance = 0.35f;  // Toplam yükselme mesafesi (çok az)
    public float riseDuration = 0.25f;  // Yükselme süresi
    public float fadeDuration = 0.2f;   // Solma süresi

    [Header("After Pickup")]
    [Tooltip("Item alındıktan sonra frame collider'ı kapatılsın mı?")]
    public bool disableColliderAfterPickup = false;

    // ── runtime ──────────────────────────────────────────────────────────
    private bool           collected;
    private SpriteRenderer frameRenderer;
    private Collider2D     frameCollider;

    private void Awake()
    {
        frameRenderer = GetComponent<SpriteRenderer>();
        frameCollider = GetComponent<Collider2D>();

        // NON-TRIGGER olmalı — fizik motoru geçişi engeller
        if (frameCollider != null)
            frameCollider.isTrigger = false;

        // Static Rigidbody2D yoksa ekle — bu olmadan kinematik olmayan
        // player rigidbody'si collider'ı zorlayabilir
        if (GetComponent<Rigidbody2D>() == null)
        {
            Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }
        else
        {
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        }

        // itemObject atanmadıysa child'dan bul
        if (itemObject == null)
        {
            Transform child = transform.Find("Item");
            if (child != null) itemObject = child.gameObject;
        }
    }

    // ── Collision callbacks (non-trigger) ─────────────────────────────────
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (collected) return;
        if (!col.gameObject.CompareTag("Player")) return;
        DoPickup(col.gameObject);
    }

    // OnCollisionStay da dinle — player yavaş yaklaşırsa Enter kaçabilir
    private void OnCollisionStay2D(Collision2D col)
    {
        if (collected) return;
        if (!col.gameObject.CompareTag("Player")) return;
        DoPickup(col.gameObject);
    }

    // ── Pickup ────────────────────────────────────────────────────────────
    private void DoPickup(GameObject player)
    {
        if (collected) return;
        collected = true;

        // 1) IItem.Pickup()
        if (itemObject != null)
        {
            if (!itemObject.scene.IsValid())
            {
                Debug.LogError("[ItemFrame] itemObject bir prefab asset'i! " +
                               "Hierarchy'deki Item child objesini sürükleyin.");
            }
            else
            {
                IItem item = itemObject.GetComponent<IItem>()
                          ?? itemObject.GetComponentInChildren<IItem>();

                if (item != null)
                    item.Pickup(player);
                else
                    Debug.LogWarning($"[ItemFrame] '{itemObject.name}' üzerinde IItem bulunamadı!");
            }
        }

        // 2) Ses
        PlayPickupSound();

        // 3) Frame görseli
        if (frameRenderer != null && emptyFrameSprite != null)
            frameRenderer.sprite = emptyFrameSprite;

        // 4) Collider — ayara göre kapat ya da bırak
        if (disableColliderAfterPickup && frameCollider != null)
            frameCollider.enabled = false;

        // 5) Item animasyonu
        if (itemObject != null && itemObject.scene.IsValid())
        {
            var floatScript = itemObject.GetComponent<ItemFloat>();
            if (floatScript != null) floatScript.enabled = false;

            // ItemFloat'un pozisyonu kaydırmış olabileceğinden sıfırla
            // (startPos'a döndür — yoksa animasyon kaymış noktadan başlar)
            StartCoroutine(PickupAnimation(itemObject));
        }
    }

    // ── Animasyon: az yüksel → solar → Destroy ────────────────────────────
    private IEnumerator PickupAnimation(GameObject target)
    {
        if (target == null) yield break;

        SpriteRenderer[] renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i] != null ? renderers[i].color : Color.white;

        Vector3 startPosition = target.transform.position;
        Vector3 endPosition   = startPosition + Vector3.up * riseDistance;

        // Yükseliş (lerp — sabit hız değil, smooth)
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / riseDuration);
            target.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        // Solma (pozisyon sabit kalır)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = originalColors[i];
                renderers[i].color = new Color(c.r, c.g, c.b, alpha);
            }
            yield return null;
        }

        if (target != null)
            Destroy(target);
    }

    private void PlayPickupSound()
    {
        if (pickupClip == null) return;
        if (audioSource != null)
            audioSource.PlayOneShot(pickupClip);
        else
            AudioSource.PlayClipAtPoint(pickupClip, transform.position);
    }
}