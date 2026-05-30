using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectionManager : MonoBehaviour
{
    public const string SelectedCharacterKey = "SelectedCharacterIndex";

    [System.Serializable]
    private struct CharacterInfo
    {
        public string characterName;
        [Min(0)] public int hearts;
        [Min(0)] public int bombAmount;
        [Min(0)] public int bombPower;
    }

    [Header("Characters")]
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private RawImage previewImage;
    [SerializeField] private Vector3 previewWorldPosition = new Vector3(1000f, 1000f, 0f);
    [SerializeField] private float previewOrthographicSize = 1.25f;
    [SerializeField] private Vector3 previewScale = Vector3.one;
    [SerializeField] private int renderTextureSize = 512;

    [Header("Character Info")]
    [SerializeField] private CharacterInfo[] characterInfos =
    {
        new CharacterInfo { characterName = "Isaac", hearts = 3, bombAmount = 1, bombPower = 1 },
        new CharacterInfo { characterName = "Maggy", hearts = 5, bombAmount = 1, bombPower = 1 },
        new CharacterInfo { characterName = "Bomberman", hearts = 3, bombAmount = 3, bombPower = 2 }
    };
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text heartsText;
    [SerializeField] private TMP_Text bombAmountText;
    [SerializeField] private TMP_Text bombPowerText;
    [SerializeField] private string filledStatMark = "| ";

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Level1";

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField, Range(0f, 1f)] private float buttonClickVolume = 1f;

    private int currentIndex;
    private Camera previewCamera;
    private RenderTexture previewTexture;
    private GameObject currentPreview;

    private void Start()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;

        SetupPreviewCamera();
        currentIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedCharacterKey, 0), 0, characterPrefabs.Length - 1);
        ShowCurrentCharacter();
    }

    private void OnDestroy()
    {
        if (previewTexture != null)
        {
            previewTexture.Release();
            Destroy(previewTexture);
        }
    }

    public void NextCharacter()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;

        PlayButtonClick();

        currentIndex++;
        if (currentIndex >= characterPrefabs.Length)
        {
            currentIndex = 0;
        }

        ShowCurrentCharacter();
    }

    public void PreviousCharacter()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;

        PlayButtonClick();

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = characterPrefabs.Length - 1;
        }

        ShowCurrentCharacter();
    }

    public void StartGameWithSelectedCharacter()
    {
        PlayButtonClick();

        PlayerPrefs.SetInt(SelectedCharacterKey, currentIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(gameSceneName);
    }

    private void SetupPreviewCamera()
    {
        if (previewImage == null) return;

        previewTexture = new RenderTexture(renderTextureSize, renderTextureSize, 16, RenderTextureFormat.ARGB32);
        previewTexture.name = "Character Preview Texture";

        GameObject cameraObject = new GameObject("Character Preview Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.position = previewWorldPosition + new Vector3(0f, 0f, -10f);

        previewCamera = cameraObject.AddComponent<Camera>();
        previewCamera.orthographic = true;
        previewCamera.orthographicSize = previewOrthographicSize;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        previewCamera.targetTexture = previewTexture;
        previewCamera.allowHDR = false;
        previewCamera.allowMSAA = false;

        previewImage.texture = previewTexture;
        previewImage.color = Color.white;
        previewImage.raycastTarget = false;
    }

    private void ShowCurrentCharacter()
    {
        if (previewCamera == null) return;

        if (currentPreview != null)
        {
            currentPreview.SetActive(false);
            Destroy(currentPreview);
            currentPreview = null;
        }

        GameObject prefab = characterPrefabs[currentIndex];
        if (prefab == null) return;

        currentPreview = CreatePreviewFromPrefabSprites(prefab);
        currentPreview.name = prefab.name + " Preview";
        currentPreview.transform.localScale = previewScale;

        UpdateCharacterInfoUi();
        previewCamera.Render();
    }

    private void UpdateCharacterInfoUi()
    {
        CharacterInfo info = GetCurrentCharacterInfo();

        if (characterNameText != null)
        {
            characterNameText.text = info.characterName;
        }

        if (heartsText != null)
        {
            heartsText.text = BuildStatText(info.hearts);
        }

        if (bombAmountText != null)
        {
            bombAmountText.text = BuildStatText(info.bombAmount);
        }

        if (bombPowerText != null)
        {
            bombPowerText.text = BuildStatText(info.bombPower);
        }
    }

    private CharacterInfo GetCurrentCharacterInfo()
    {
        if (characterInfos != null && currentIndex >= 0 && currentIndex < characterInfos.Length)
        {
            CharacterInfo info = characterInfos[currentIndex];
            if (!string.IsNullOrWhiteSpace(info.characterName))
            {
                return info;
            }
        }

        string fallbackName = characterPrefabs != null
                              && currentIndex >= 0
                              && currentIndex < characterPrefabs.Length
                              && characterPrefabs[currentIndex] != null
            ? characterPrefabs[currentIndex].name
            : "Character";

        return new CharacterInfo
        {
            characterName = fallbackName,
            hearts = 0,
            bombAmount = 0,
            bombPower = 0
        };
    }

    private string BuildStatText(int amount)
    {
        if (amount <= 0) return string.Empty;

        string text = string.Empty;
        for (int i = 0; i < amount; i++)
        {
            text += filledStatMark;
        }

        return text;
    }

    private GameObject CreatePreviewFromPrefabSprites(GameObject prefab)
    {
        GameObject previewRoot = new GameObject(prefab.name + " Preview Root");
        previewRoot.transform.position = previewWorldPosition;

        IsaacMovement movement = prefab.GetComponent<IsaacMovement>();
        if (movement != null && movement.spritesDown.body != null && movement.spritesDown.head != null)
        {
            CopyPreviewSprite(prefab.transform, movement.spritesDown.body.transform, previewRoot.transform);
            CopyPreviewSprite(prefab.transform, movement.spritesDown.head.transform, previewRoot.transform);
            return previewRoot;
        }

        SpriteRenderer firstRenderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
        if (firstRenderer != null)
        {
            CopyPreviewSprite(prefab.transform, firstRenderer.transform, previewRoot.transform);
        }

        return previewRoot;
    }

    private void CopyPreviewSprite(Transform prefabRoot, Transform source, Transform previewRoot)
    {
        SpriteRenderer sourceRenderer = source.GetComponent<SpriteRenderer>();
        if (sourceRenderer == null || sourceRenderer.sprite == null) return;

        GameObject spriteObject = new GameObject(source.name);
        spriteObject.transform.SetParent(previewRoot, false);
        spriteObject.transform.localPosition = prefabRoot.InverseTransformPoint(source.position);
        spriteObject.transform.localRotation = source.localRotation;
        spriteObject.transform.localScale = source.lossyScale;

        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sourceRenderer.sprite;
        renderer.color = sourceRenderer.color;
        renderer.flipX = sourceRenderer.flipX;
        renderer.flipY = sourceRenderer.flipY;
        renderer.drawMode = sourceRenderer.drawMode;
        renderer.size = sourceRenderer.size;
        renderer.tileMode = sourceRenderer.tileMode;
        renderer.maskInteraction = sourceRenderer.maskInteraction;
        renderer.sortingLayerID = sourceRenderer.sortingLayerID;
        renderer.sortingOrder = sourceRenderer.sortingOrder;
    }

    private void PlayButtonClick()
    {
        UIButtonSound.Play(buttonClickClip, buttonClickVolume);
    }
}
