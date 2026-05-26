using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager instance;

    private AudioSource audioSource;
    private AudioClip previousClip;
    private float previousTime;
    private bool previousWasPlaying;
    private float previousVolume = 1f;

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

    public void PauseForBossIntro()
    {
        if (audioSource == null) return;

        previousClip = audioSource.clip;
        previousTime = audioSource.time;
        previousWasPlaying = audioSource.isPlaying;
        previousVolume = audioSource.volume;
        audioSource.Pause();
    }

    public void PlayBossMusic(AudioClip bossMusic, float volume = 1f)
    {
        if (audioSource == null) return;
        if (bossMusic == null)
        {
            ResumePreviousMusic();
            return;
        }

        audioSource.Stop();
        audioSource.clip = bossMusic;
        audioSource.loop = true;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.time = 0f;
        audioSource.Play();
    }

    public void PlayBossDeathMusicThenResume(AudioClip deathMusic, float volume = 1f)
    {
        if (audioSource == null)
            return;

        StopAllCoroutines();
        StartCoroutine(BossDeathMusicRoutine(deathMusic, Mathf.Clamp01(volume)));
    }

    private System.Collections.IEnumerator BossDeathMusicRoutine(AudioClip deathMusic, float volume)
    {
        if (deathMusic != null)
        {
            audioSource.Stop();
            audioSource.clip = deathMusic;
            audioSource.loop = false;
            audioSource.volume = volume;
            audioSource.time = 0f;
            audioSource.Play();
            yield return new WaitForSeconds(deathMusic.length);
        }

        ResumePreviousMusic();
    }

    public void ResumePreviousMusic()
    {
        if (audioSource == null) return;
        if (previousClip == null)
        {
            audioSource.Stop();
            return;
        }

        audioSource.Stop();
        audioSource.clip = previousClip;
        audioSource.loop = true;
        audioSource.volume = previousVolume;
        audioSource.time = Mathf.Clamp(previousTime, 0f, Mathf.Max(0f, previousClip.length - 0.01f));

        if (previousWasPlaying)
            audioSource.Play();
    }
}
