using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager instance;

    private AudioSource audioSource;

    private void Awake()
    {
        // Zaten bir instance varsa bu objeyi yok et (duplicate önleme)
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        audioSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = Mathf.Clamp01(volume);
    }

    public float GetVolume()
    {
        return audioSource != null ? audioSource.volume : 1f;
    }
}