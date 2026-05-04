using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; //← Bunu ekle

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu instance;

    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.4f;

    private bool isPaused = false;

    private void Awake()
    {
        instance = this;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);

        if (musicSlider != null)
        {
            if (BackgroundMusicManager.instance != null)
                musicSlider.value = BackgroundMusicManager.instance.GetVolume();

            musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }
    }

    private void Update()
    {
        // New Input System ile ESC kontrolü
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pausePanel == null) return;

        isPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;

        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }

    public void ResumeGame()
    {
        if (pausePanel == null) return;

        isPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (BackgroundMusicManager.instance != null)
            BackgroundMusicManager.instance.SetVolume(value);
    }

    private IEnumerator FadeIn()
    {
        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }
}