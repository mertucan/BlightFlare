using System.Collections;
using UnityEngine;

/// <summary>
/// Bob's Curse bombasının görsel davranışı.
/// Animasyon olmadan kod ile yanıp söner ve
/// patlamadan önce hızlanır (normal bomba gibi).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BobsCurseBomb : MonoBehaviour
{
    [Header("Blink Settings")]
    [Tooltip("Toplam fitil süresi — BombController.bombFuseTime ile eşleşmeli.")]
    public float fuseTime = 3f;

    [Tooltip("Başlangıç yanıp sönme hızı (saniye).")]
    public float initialBlinkRate = 0.5f;

    [Tooltip("Patlama öncesi en hızlı yanıp sönme hızı (saniye).")]
    public float finalBlinkRate = 0.07f;

    [Tooltip("Yeşil parıltı rengi.")]
    public Color glowColor = new Color(0.3f, 1f, 0.3f, 1f);

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        float elapsed = 0f;
        bool glowing = false;

        while (elapsed < fuseTime)
        {
            // Süre ilerledikçe yanıp sönme hızlanır
            float t = elapsed / fuseTime;
            float blinkRate = Mathf.Lerp(initialBlinkRate, finalBlinkRate, t);

            // Rengi değiştir (normal <-> yeşil parıltı)
            sr.color = glowing ? glowColor : Color.white;
            glowing = !glowing;

            yield return new WaitForSeconds(blinkRate);
            elapsed += blinkRate;
        }

        // Son frame: renderer kapat (BombController destroy edecek)
        sr.enabled = false;
    }
}