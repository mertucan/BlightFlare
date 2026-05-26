using UnityEngine;
using UnityEngine.SceneManagement;

public class TunnelEnter : MonoBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float sceneFadeOutDuration = 1.5f;

    private bool isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || isTransitioning) return;

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError("Bu zaten son sahne, sonraki sahne yok!");
            return;
        }

        Debug.Log($"Tünele girildi! {currentIndex} → {nextIndex}");
        LoadNextScene(nextIndex);
    }

    private void LoadNextScene(int index)
    {
        isTransitioning = true;
        Time.timeScale = 1f;
        SceneTransitionFade.LoadSceneWithBlackFade(index, sceneFadeOutDuration);
    }
}
