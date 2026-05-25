using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelRestartController : MonoBehaviour
{
    private const string ControllerName = "LevelRestartController";

    [Header("Restart")]
    [SerializeField] private string restartSceneName = "Level1";
    [SerializeField] private float holdDuration = 5f;
    [SerializeField] private float fadeStartTime = 3f;

    private Image fadeImage;
    private Coroutine fadeRoutine;
    private float holdTimer;
    private bool isRestarting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindFirstObjectByType<LevelRestartController>() != null) return;

        GameObject go = new GameObject(ControllerName);
        go.AddComponent<LevelRestartController>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        EnsureFadeOverlay();
    }

    private void Update()
    {
        if (isRestarting) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.isPressed)
        {
            StopFadeRoutine();
            holdTimer += Time.unscaledDeltaTime;
            UpdateHoldFade();

            if (holdTimer >= holdDuration)
                StartCoroutine(RestartRoutine());

            return;
        }

        if (holdTimer > 0f)
        {
            holdTimer = 0f;
            fadeRoutine = StartCoroutine(FadeTo(0f, Mathf.Max(0.05f, holdDuration - fadeStartTime)));
        }
    }

    private IEnumerator RestartRoutine()
    {
        isRestarting = true;
        Time.timeScale = 1f;
        StopFadeRoutine();

        yield return FadeTo(1f, 0.05f);

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            Destroy(player);

        AsyncOperation load = SceneManager.LoadSceneAsync(restartSceneName);
        while (load != null && !load.isDone)
            yield return null;

        holdTimer = 0f;
        yield return FadeTo(0f, Mathf.Max(0.05f, holdDuration - fadeStartTime));
        isRestarting = false;
    }

    private void StopFadeRoutine()
    {
        if (fadeRoutine == null) return;
        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    private void UpdateHoldFade()
    {
        if (fadeImage == null) EnsureFadeOverlay();
        if (fadeImage == null) return;

        if (holdTimer < fadeStartTime)
        {
            SetFadeAlpha(0f);
            return;
        }

        float fadeDuration = Mathf.Max(0.05f, holdDuration - fadeStartTime);
        float t = Mathf.Clamp01((holdTimer - fadeStartTime) / fadeDuration);
        SetFadeAlpha(t);
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null) EnsureFadeOverlay();
        if (fadeImage == null) yield break;

        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }

    private void EnsureFadeOverlay()
    {
        if (fadeImage != null) return;

        GameObject canvasGO = new GameObject("RestartFadeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasGO);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject imageGO = new GameObject("RestartFadeImage", typeof(RectTransform), typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = imageGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        fadeImage = imageGO.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;
        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }
}
