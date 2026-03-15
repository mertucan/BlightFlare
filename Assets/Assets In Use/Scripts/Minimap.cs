using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public static Minimap instance;

    [Header("Display")]
    public float minimapScreenSize = 280f;
    public float screenMargin = 10f;
    public float cameraPadding = 0.6f;
    public int textureResolution = 256;

    [Header("Highlight")]
    public Color highlightTint = Color.white;
    public Color defaultTint = new Color(0.6f, 0.6f, 0.6f, 1f);

    private const int MINIMAP_LAYER = 11;

    private Camera minimapCam;
    private RenderTexture renderTexture;
    private Canvas canvas;
    private RawImage rawImage;
    private Image bgImage;

    private List<Cell> trackedCells = new();
    private Cell highlightedCell;
    private int lastHighlightedIndex = -1;

    private void Awake()
    {
        instance = this;
    }

    public void BuildMinimap(List<Cell> cells)
    {
        trackedCells = new List<Cell>(cells);
        lastHighlightedIndex = -1;
        highlightedCell = null;

        foreach (var cell in cells)
            SetLayerRecursive(cell.gameObject, MINIMAP_LAYER);

        ExcludeLayerFromMainCamera();

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var cell in cells)
        {
            Vector3 p = cell.transform.position;
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        float cx = (minX + maxX) * 0.5f;
        float cy = (minY + maxY) * 0.5f;
        float halfW = (maxX - minX) * 0.5f + cameraPadding;
        float halfH = (maxY - minY) * 0.5f + cameraPadding;

        SetupCamera(cx, cy, halfW, halfH);
        SetupUI();
        ApplyDefaultTint();
    }

    private void Update()
    {
        if (RoomTransitionManager.instance == null) return;
        int current = RoomTransitionManager.instance.CurrentRoomIndex;
        if (current == lastHighlightedIndex) return;

        if (highlightedCell != null)
            TintCell(highlightedCell, defaultTint);

        highlightedCell = null;
        foreach (var cell in trackedCells)
        {
            if (cell.cellList.Contains(current))
            {
                highlightedCell = cell;
                TintCell(cell, highlightTint);
                break;
            }
        }

        lastHighlightedIndex = current;
    }

    private void SetupCamera(float cx, float cy, float halfW, float halfH)
    {
        if (renderTexture != null) renderTexture.Release();
        renderTexture = new RenderTexture(textureResolution, textureResolution, 24);
        renderTexture.filterMode = FilterMode.Point;

        if (minimapCam == null)
        {
            var go = new GameObject("MinimapCamera");
            go.transform.SetParent(transform, false);
            minimapCam = go.AddComponent<Camera>();
        }

        minimapCam.orthographic = true;
        minimapCam.orthographicSize = Mathf.Max(halfW, halfH);
        minimapCam.transform.position = new Vector3(cx, cy, -10f);
        minimapCam.cullingMask = 1 << MINIMAP_LAYER;
        minimapCam.clearFlags = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
        minimapCam.targetTexture = renderTexture;
        minimapCam.depth = 10;
    }

    private void SetupUI()
    {
        if (canvas == null)
        {
            var canvasGO = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var bgGO = new GameObject("MinimapBG", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvas.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(1, 1);
            bgRT.anchorMax = new Vector2(1, 1);
            bgRT.pivot = new Vector2(1, 1);
            bgRT.anchoredPosition = new Vector2(-screenMargin, -screenMargin);
            bgRT.sizeDelta = new Vector2(minimapScreenSize + 6f, minimapScreenSize + 6f);
            bgImage = bgGO.GetComponent<Image>();
            bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var imgGO = new GameObject("MinimapImage", typeof(RectTransform), typeof(RawImage));
            imgGO.transform.SetParent(bgGO.transform, false);
            rawImage = imgGO.GetComponent<RawImage>();
            var imgRT = imgGO.GetComponent<RectTransform>();
            imgRT.anchorMin = Vector2.zero;
            imgRT.anchorMax = Vector2.one;
            imgRT.offsetMin = new Vector2(3f, 3f);
            imgRT.offsetMax = new Vector2(-3f, -3f);
        }

        rawImage.texture = renderTexture;
    }

    private void ApplyDefaultTint()
    {
        foreach (var cell in trackedCells)
            TintCell(cell, defaultTint);
    }

    private void TintCell(Cell cell, Color color)
    {
        if (cell.spriteRenderer != null)
            cell.spriteRenderer.color = color;
        if (cell.roomSprite != null)
            cell.roomSprite.color = color;
    }

    private void ExcludeLayerFromMainCamera()
    {
        Camera main = Camera.main;
        if (main != null)
            main.cullingMask &= ~(1 << MINIMAP_LAYER);

        if (RoomTransitionManager.instance != null && RoomTransitionManager.instance.cameraController != null)
        {
            var cam = RoomTransitionManager.instance.cameraController.GetComponent<Camera>();
            if (cam != null)
                cam.cullingMask &= ~(1 << MINIMAP_LAYER);
        }
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
    }
}
