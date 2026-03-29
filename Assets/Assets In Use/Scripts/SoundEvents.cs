using UnityEngine;

public class SoundEvents : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] clips; // Tüm sesleri buraya sürükle bırak
    
    public void PlayClip(int index)
    {
        if (index < 0 || index >= clips.Length) return;
        audioSource.PlayOneShot(clips[index]);
    }
}
