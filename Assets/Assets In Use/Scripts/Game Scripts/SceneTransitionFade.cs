using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionFade : MonoBehaviour
{
    private Image fadeImage;

    public static void LoadSceneWithBlackFade(int sceneBuildIndex, float fadeOutDuration)
    {
        GameObject fadeObject = new GameObject("Scene Transition Fade");
        DontDestroyOnLoad(fadeObject);

        SceneTransitionFade transitionFade = fadeObject.AddComponent<SceneTransitionFade>();
        transitionFade.StartCoroutine(transitionFade.LoadAndFadeOut(sceneBuildIndex, fadeOutDuration));
    }

    private IEnumerator LoadAndFadeOut(int sceneBuildIndex, float fadeOutDuration)
    {
        CreateOverlay();
        SetAlpha(1f);

        yield return null;

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneBuildIndex);
        while (load != null && !load.isDone)
        {
            yield return null;
        }

        yield return FadeToClear(Mathf.Max(0.05f, fadeOutDuration));
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

    private void SetAlpha(float alpha)
    {
        if (fadeImage == null) return;

        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }
}
