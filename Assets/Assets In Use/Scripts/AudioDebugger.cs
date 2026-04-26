using UnityEngine;

/// <summary>
/// Sahnedeki tüm AudioSource'ları ve çalan sesleri loglar.
/// Isaac objesine geçici olarak ekle, sorunu bulduktan sonra sil.
/// </summary>
public class AudioDebugger : MonoBehaviour
{
    private void Start()
    {
        var allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        Debug.Log($"[AudioDebugger] Sahnede {allSources.Length} AudioSource bulundu:");
        foreach (var src in allSources)
        {
            Debug.Log($"  → GO:{src.gameObject.name}, " +
                      $"PlayOnAwake:{src.playOnAwake}, " +
                      $"clip:{(src.clip != null ? src.clip.name : "null")}, " +
                      $"isPlaying:{src.isPlaying}");
        }
    }

    private void Update()
    {
        // Herhangi bir AudioSource ses çalmaya başlarsa logla
        var allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var src in allSources)
        {
            if (src.isPlaying)
            {
                // Her frame loglamak çok fazla — sadece yeni başlayanları yakala
                // Bu basit versiyon her frame basar, sadece kısa test için kullan
            }
        }
    }
}