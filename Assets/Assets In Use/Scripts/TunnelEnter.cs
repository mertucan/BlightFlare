using UnityEngine;
// Sahne yönetimi için bu kütüphaneyi eklemek ZORUNDAYIZ
using UnityEngine.SceneManagement; 

public class TunnelEnter : MonoBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";
    
    // Yüklenecek sahnenin adını Inspector'dan yazacağız
    [SerializeField] private string nextSceneName; 

    // Birden fazla tetiklenmeyi önlemek için kontrol
    private bool isTransitioning = false; 

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Giren obje oyuncu değilse veya geçiş zaten başladıysa işlemi iptal et
        if (!other.CompareTag(playerTag) || isTransitioning) return;
        
        Debug.Log("Tünele girildi! Sonraki sahneye geçiliyor: " + nextSceneName);
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        isTransitioning = true;

        // Sahne adının boş olup olmadığını kontrol et (Hata yapmamak için önemli)
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            // Eğer bir önceki kodda Time.timeScale = 0f yaptıysan,
            // yeni sahne donuk başlar. Bunu garantiye almak için zamanı sıfırlayalım.
            Time.timeScale = 1f; 

            // Sahneyi yükle
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogError("TunnelEnter: 'Next Scene Name' boş bırakılmış! Inspector'dan bir sahne adı girin.");
            isTransitioning = false; // Hata varsa tekrar denenebilsin diye kilidi aç
        }
    }
}