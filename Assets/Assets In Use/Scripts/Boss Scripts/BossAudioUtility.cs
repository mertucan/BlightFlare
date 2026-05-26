using UnityEngine;

public static class BossAudioUtility
{
    public static void Play2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        GameObject go = new GameObject("BossOneShotAudio");
        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = Mathf.Clamp01(volume);
        source.PlayOneShot(clip);
        Object.Destroy(go, clip.length + 0.1f);
    }
}
