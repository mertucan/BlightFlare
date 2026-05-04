using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Oyun bitti ekranını yönetir.
/// Inspector'da:
///   • gameOverPanel   → Game Over UI'ının root GameObject'i
///   • tryAgainButton  → "TRY AGAIN" butonu
///   • quitButton      → "QUIT" butonu
///   • canvasGroup     → Panel'deki CanvasGroup (fade-in için)
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    public static GameOverScreen instance;

    [Header("UI References")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.8f;

    private void Awake()
    {
        instance = this;

        // Panel başlangıçta kapalı
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Start()
    {
        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(OnTryAgain);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuit);
    }

    /// <summary>
    /// Oyuncu öldüğünde bu metodu çağır.
    /// Örnek: GameOverScreen.instance.Show();
    /// </summary>
    public void Show()
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);
        Time.timeScale = 0f; // Oyunu dondur

        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            // Time.timeScale 0 olduğu için unscaledDeltaTime kullan
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    private void OnTryAgain()
    {
        Time.timeScale = 1f; // Önce zamanı normale al
        gameOverPanel.SetActive(false);

        // Haritayı sıfırdan oluştur
        if (MapGenerator.instance != null)
        {
            // MapGenerator'ın SetupDungeon metodunu reflection olmadan çağır.
            // SetupDungeon private olduğu için public bir wrapper ekledik (aşağıya bak)
            // ya da doğrudan sahneyi yeniden yükle:
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void OnQuit()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}