using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class IntroVideoController : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string nextSceneName = "MainMenu";
    [SerializeField] private bool allowSkipWithAnyKey = true;
    [SerializeField] private bool useFadeTransition = true;
    [SerializeField] private float introFadeFromBlackDuration = 0.5f;
    [SerializeField] private float fadeToBlackDuration = 0.5f;
    [SerializeField] private float fadeFromBlackDuration = 0.5f;
    [SerializeField] private bool pauseBackgroundMusicDuringIntro = true;

    private bool isChangingScene;
    private bool pausedBackgroundMusic;
    private Image introFadeImage;
    private Coroutine introFadeRoutine;

    private void Awake()
    {
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }

        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            ClearTargetTexture();
        }

        CreateIntroFadeOverlay();
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }
    }

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[IntroVideoController] VideoPlayer is not assigned.");
            return;
        }

        PauseBackgroundMusic();
        videoPlayer.Prepare();
    }

    private void Update()
    {
        if (allowSkipWithAnyKey && WasSkipPressed())
        {
            LoadNextScene();
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }

    public void SkipIntro()
    {
        LoadNextScene();
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.Play();
        StartIntroFadeFromBlack();
    }

    private void OnVideoFinished(VideoPlayer source)
    {
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (isChangingScene)
        {
            return;
        }

        isChangingScene = true;

        if (useFadeTransition)
        {
            SceneTransitionFade.LoadSceneWithBlackFade(
                nextSceneName,
                fadeToBlackDuration,
                fadeFromBlackDuration,
                ResumeBackgroundMusic);
            return;
        }

        ResumeBackgroundMusic();
        SceneManager.LoadScene(nextSceneName);
    }

    private void PauseBackgroundMusic()
    {
        if (!pauseBackgroundMusicDuringIntro || pausedBackgroundMusic || BackgroundMusicManager.instance == null)
        {
            return;
        }

        BackgroundMusicManager.instance.PauseCurrentMusic();
        pausedBackgroundMusic = true;
    }

    private void ResumeBackgroundMusic()
    {
        if (!pausedBackgroundMusic || BackgroundMusicManager.instance == null)
        {
            return;
        }

        BackgroundMusicManager.instance.ResumePreviousMusic();
        pausedBackgroundMusic = false;
    }

    private void CreateIntroFadeOverlay()
    {
        GameObject canvasObject = new GameObject("Intro Start Fade Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 1;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("Intro Start Fade Image", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        introFadeImage = imageObject.GetComponent<Image>();
        introFadeImage.color = Color.black;
        introFadeImage.raycastTarget = false;
    }

    private void StartIntroFadeFromBlack()
    {
        if (introFadeImage == null)
        {
            return;
        }

        if (introFadeRoutine != null)
        {
            StopCoroutine(introFadeRoutine);
        }

        introFadeRoutine = StartCoroutine(FadeIntroOverlayToClear());
    }

    private IEnumerator FadeIntroOverlayToClear()
    {
        float duration = Mathf.Max(0.05f, introFadeFromBlackDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetIntroFadeAlpha(Mathf.Lerp(1f, 0f, elapsed / duration));
            yield return null;
        }

        SetIntroFadeAlpha(0f);

        if (introFadeImage != null)
        {
            Destroy(introFadeImage.transform.parent.gameObject);
        }
    }

    private void SetIntroFadeAlpha(float alpha)
    {
        if (introFadeImage == null)
        {
            return;
        }

        Color color = introFadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        introFadeImage.color = color;
    }

    private bool WasSkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame
                || Mouse.current.rightButton.wasPressedThisFrame
                || Mouse.current.middleButton.wasPressedThisFrame))
            || (Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.anyKeyDown;
#else
        return false;
#endif
    }

    private void ClearTargetTexture()
    {
        RenderTexture targetTexture = videoPlayer.targetTexture;
        if (targetTexture == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = targetTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;
    }
}
