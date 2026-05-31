using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 1f;

    public void StartGame()
    {
        PlayButtonClick();
        SceneManager.LoadScene("Level1");
    }

    public void LoadMainMenu()
    {
        PlayButtonClick();
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadPreviousScene()
    {
        PlayButtonClick();

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int previousIndex = currentIndex - 1;

        if (previousIndex < 0)
        {
            Debug.LogWarning("Bir onceki sahne yok.");
            return;
        }

        SceneManager.LoadScene(previousIndex);
    }

    public void LoadNextScene()
    {
        PlayButtonClick();

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("Bir sonraki sahne yok.");
            return;
        }

        SceneManager.LoadScene(nextIndex);
    }

    public void QuitGame()
    {
        PlayButtonClick();
        Debug.Log("Oyun kapatiliyor...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void PlayButtonClick()
    {
        UIButtonSound.Play(buttonClickClip, buttonClickVolume);
    }
}
