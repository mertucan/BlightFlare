using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BossIntroOverlay : MonoBehaviour
{
    private static BossIntroOverlay instance;

    public static bool IsPlaying { get; private set; }

    private Canvas canvas;
    private Image photoImage;
    private Image blackImage;
    private AudioSource audioSource;
    private Coroutine sequenceRoutine;
    private bool skipRequested;

    public static void Play(Sprite photo, AudioClip music, float volume, float fadeToBlackDuration, Action onComplete = null)
    {
        if (photo == null && music == null)
        {
            IsPlaying = false;
            onComplete?.Invoke();
            return;
        }

        if (instance == null)
            instance = CreateInstance();

        instance.StartSequence(photo, music, volume, fadeToBlackDuration, onComplete);
    }

    private static BossIntroOverlay CreateInstance()
    {
        GameObject go = new GameObject("BossIntroOverlay");
        DontDestroyOnLoad(go);
        BossIntroOverlay overlay = go.AddComponent<BossIntroOverlay>();
        overlay.BuildUi();
        return overlay;
    }

    private void BuildUi()
    {
        GameObject canvasGO = new GameObject("BossIntroCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9900;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        photoImage = CreateFullScreenImage(canvasGO.transform, "BossIntroPhoto", Color.clear);
        photoImage.preserveAspect = true;

        blackImage = CreateFullScreenImage(canvasGO.transform, "BossIntroBlackFade", Color.clear);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        canvasGO.SetActive(false);
    }

    private Image CreateFullScreenImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void StartSequence(Sprite photo, AudioClip music, float volume, float fadeToBlackDuration, Action onComplete)
    {
        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        sequenceRoutine = StartCoroutine(Sequence(photo, music, Mathf.Clamp01(volume), Mathf.Max(0.01f, fadeToBlackDuration), onComplete));
    }

    private void Update()
    {
        if (sequenceRoutine == null) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard[Key.Escape].wasPressedThisFrame || keyboard[Key.Space].wasPressedThisFrame)
            skipRequested = true;
    }

    private IEnumerator Sequence(Sprite photo, AudioClip music, float volume, float fadeToBlackDuration, Action onComplete)
    {
        IsPlaying = true;
        skipRequested = false;
        canvas.gameObject.SetActive(true);

        photoImage.sprite = photo;
        photoImage.enabled = photo != null;
        photoImage.color = Color.white;
        blackImage.color = Color.clear;

        float waitTime = 0f;
        if (music != null)
        {
            audioSource.clip = music;
            audioSource.volume = volume;
            audioSource.Play();
            waitTime = music.length;
        }

        if (waitTime > 0f)
        {
            float elapsedMusic = 0f;
            while (elapsedMusic < waitTime && !skipRequested)
            {
                elapsedMusic += Time.deltaTime;
                yield return null;
            }
        }

        if (skipRequested)
        {
            Finish(onComplete);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < fadeToBlackDuration && !skipRequested)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeToBlackDuration);
            blackImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        if (skipRequested)
        {
            Finish(onComplete);
            yield break;
        }

        blackImage.color = Color.black;
        Finish(onComplete);
    }

    private void Finish(Action onComplete)
    {
        if (audioSource != null)
            audioSource.Stop();

        canvas.gameObject.SetActive(false);
        sequenceRoutine = null;
        IsPlaying = false;
        onComplete?.Invoke();
    }
}
