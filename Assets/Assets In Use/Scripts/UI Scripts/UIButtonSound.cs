using UnityEngine;

public static class UIButtonSound
{
    public static void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;

        GameObject audioObject = new GameObject("UI Button Sound");
        Object.DontDestroyOnLoad(audioObject);

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = Mathf.Clamp01(volume);
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        Object.Destroy(audioObject, clip.length);
    }
}
