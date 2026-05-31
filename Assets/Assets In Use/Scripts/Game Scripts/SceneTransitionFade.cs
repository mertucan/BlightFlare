using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionFade : MonoBehaviour
{
    private Image fadeImage;

    public static void LoadSceneWithBlackFade(int sceneBuildIndex, float fadeOutDuration)
    {
        LoadSceneWithBlackFade(sceneBuildIndex, 0f, fadeOutDuration);
    }

    public static void LoadSceneWithBlackFade(int sceneBuildIndex, float fadeToBlackDuration, float fadeFromBlackDuration)
    {
        GameObject fadeObject = new GameObject("Scene Transition Fade");
        DontDestroyOnLoad(fadeObject);

        SceneTransitionFade transitionFade = fadeObject.AddComponent<SceneTransitionFade>();
        transitionFade.StartCoroutine(transitionFade.LoadAndFade(sceneBuildIndex, fadeToBlackDuration, fadeFromBlackDuration));
    }

    public static void LoadSceneWithBlackFade(string sceneName, float fadeToBlackDuration, float fadeFromBlackDuration)
    {
        LoadSceneWithBlackFade(sceneName, fadeToBlackDuration, fadeFromBlackDuration, null);
    }

    public static void LoadSceneWithBlackFade(string sceneName, float fadeToBlackDuration, float fadeFromBlackDuration, Action onSceneLoaded)
    {
        GameObject fadeObject = new GameObject("Scene Transition Fade");
        DontDestroyOnLoad(fadeObject);

        SceneTransitionFade transitionFade = fadeObject.AddComponent<SceneTransitionFade>();
        transitionFade.StartCoroutine(transitionFade.LoadAndFade(sceneName, fadeToBlackDuration, fadeFromBlackDuration, onSceneLoaded));
    }

    private IEnumerator LoadAndFadeOut(int sceneBuildIndex, float fadeOutDuration)
    {
        yield return LoadAndFade(sceneBuildIndex, 0f, fadeOutDuration);
    }

    private IEnumerator LoadAndFade(int sceneBuildIndex, float fadeToBlackDuration, float fadeFromBlackDuration)
    {
        CreateOverlay();
        SetAlpha(0f);

        yield return FadeToBlack(fadeToBlackDuration);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneBuildIndex);
        while (load != null && !load.isDone)
        {
            yield return null;
        }

        yield return FadeToClear(Mathf.Max(0.05f, fadeFromBlackDuration));
        Destroy(gameObject);
    }

    private IEnumerator LoadAndFade(string sceneName, float fadeToBlackDuration, float fadeFromBlackDuration, Action onSceneLoaded)
    {
        CreateOverlay();
        SetAlpha(0f);

        yield return FadeToBlack(fadeToBlackDuration);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        while (load != null && !load.isDone)
        {
            yield return null;
        }

        onSceneLoaded?.Invoke();

        yield return FadeToClear(Mathf.Max(0.05f, fadeFromBlackDuration));
        Destroy(gameObject);
    }

    private void CreateOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        GameObject imageObject = new GameObject("Black Fade Image", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
    }

    private IEnumerator FadeToClear(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }

        SetAlpha(0f);
    }

    private IEnumerator FadeToBlack(float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(1f);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(0f, 1f, elapsed / duration));
            yield return null;
        }

        SetAlpha(1f);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }
}
