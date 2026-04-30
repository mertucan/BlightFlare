using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public static Minimap instance;

    [Header("Display")]
    public float minimapScreenSize = 280f;
    public float screenMargin      = 10f;
    public int   textureResolution = 256;

    [Header("Focus Mode (varsayılan)")]
    [Tooltip("Oyuncuya kilitli modda kamera kaç birim alanı göstersin")]
    public float focusOrthoSize = 1.2f;

    [Header("Overview Mode (Tab)")]
    [Tooltip("Tüm harita görünümü kenar boşluğu")]
    public float overviewPadding = 0.6f;
    [Tooltip("Tab geçişinde zoom animasyon hızı")]
    public float overviewLerpSpeed = 10f;

    [Header("Highlight")]
    public Color highlightTint  = Color.white;
    public Color defaultTint    = new Color(0.6f, 0.6f, 0.6f, 1f);
    public Color unexploredTint = new Color(0.25f, 0.25f, 0.3f, 1f);

    private const int MINIMAP_LAYER = 11;

    private Camera        minimapCam;
    private RenderTexture renderTexture;
    private Canvas        canvas;
    private RawImage      rawImage;
    private Image         bgImage;

    private List<Cell>   trackedCells         = new();
    private Cell         highlightedCell;
    private int          lastHighlightedIndex = -1;

    private readonly HashSet<int> hiddenSecretCells   = new();
    private readonly HashSet<int> visibleCellIndexes  = new();
    private readonly HashSet<int> exploredCellIndexes = new();

    private Vector3 overviewCenter;
    private float   overviewOrthoSize;

    // Tab toggle
    private bool isOverviewMode  = false;
    private bool tabWasPressed   = false;

    // Kamera hedef değerleri (smooth geçiş için)
    private Vector3 camTargetPos;
    private float   camTargetSize;

    // Başlangıç snap flag'i — ilk frame'de lerp yok
    private bool cameraInitialized = false;

    private void Awake() => instance = this;

    public void BuildMinimap(List<Cell> cells)
    {
        trackedCells         = new List<Cell>(cells);
        lastHighlightedIndex = -1;
        highlightedCell      = null;
        isOverviewMode       = false;
        tabWasPressed        = false;
        cameraInitialized    = false;

        hiddenSecretCells.Clear();
        visibleCellIndexes.Clear();
        exploredCellIndexes.Clear();

        foreach (var cell in cells)
        {
            SetLayerRecursive(cell.gameObject, MINIMAP_LAYER);
            if (cell.roomType == RoomType.Secret)
                foreach (int idx in cell.cellList)
                    hiddenSecretCells.Add(idx);
            HideCell(cell);
        }

        ExcludeLayerFromMainCamera();
        ComputeOverviewBounds(cells);
        SetupCamera();
        SetupUI();

        // Başlangıç odasını aç — kamera hemen snap'lenecek
        RevealRoomAndNeighbours(45);
    }

    // ─── Overview bounds ──────────────────────────────────────────────────────
    private void ComputeOverviewBounds(List<Cell> cells)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        foreach (var cell in cells)
        {
            Vector3 p = cell.transform.position;
            minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
        }
        overviewCenter    = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, -10f);
        overviewOrthoSize = Mathf.Max((maxX - minX) * 0.5f, (maxY - minY) * 0.5f) + overviewPadding;
    }

    // ─── Isaac Fog-of-War ─────────────────────────────────────────────────────
    private void RevealRoomAndNeighbours(int centerIndex)
    {
        exploredCellIndexes.Add(centerIndex);
        visibleCellIndexes.Add(centerIndex);

        int[] neighbours = { centerIndex - 1, centerIndex + 1, centerIndex - 10, centerIndex + 10 };
        foreach (int n in neighbours)
            if (n >= 0 && n < 100)
                visibleCellIndexes.Add(n);

        RefreshAllCellVisibility();
    }

    private void RefreshAllCellVisibility()
    {
        foreach (var cell in trackedCells)
        {
            bool isSecret = cell.cellList.Any(idx => hiddenSecretCells.Contains(idx));
            if (isSecret) { HideCell(cell); continue; }

            bool anyVisible  = cell.cellList.Any(idx => visibleCellIndexes.Contains(idx));
            bool anyExplored = cell.cellList.Any(idx => exploredCellIndexes.Contains(idx));

            if (!anyVisible)
                HideCell(cell);
            else if (anyExplored)
            {
                ShowCell(cell);
                if (cell != highlightedCell) TintCell(cell, defaultTint);
            }
            else
            {
                ShowCell(cell);
                TintCell(cell, unexploredTint);
            }
        }
    }

    public void RevealSecretRoom(int secretCellIndex)
    {
        if (!hiddenSecretCells.Contains(secretCellIndex)) return;

        var secretCell = trackedCells.FirstOrDefault(c => c.cellList.Contains(secretCellIndex));
        if (secretCell == null) return;

        foreach (int idx in secretCell.cellList)
        {
            hiddenSecretCells.Remove(idx);
            visibleCellIndexes.Add(idx);
        }

        ShowCell(secretCell);
        TintCell(secretCell, unexploredTint);
    }

    // ─── Update ───────────────────────────────────────────────────────────────
    private void Update()
    {
        if (RoomTransitionManager.instance == null) return;

        HandleTabToggle();

        int current = RoomTransitionManager.instance.CurrentRoomIndex;
        if (current != lastHighlightedIndex)
            OnRoomChanged(current);

        UpdateMinimapCamera();
    }

    private void HandleTabToggle()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool tabDown = kb.tabKey.isPressed;

        // Edge-trigger: sadece basış anında toggle
        if (tabDown && !tabWasPressed)
            isOverviewMode = !isOverviewMode;

        tabWasPressed = tabDown;
    }

    private void OnRoomChanged(int current)
    {
        if (hiddenSecretCells.Contains(current))
            RevealSecretRoom(current);

        if (!exploredCellIndexes.Contains(current))
            RevealRoomAndNeighbours(current);
        else
            exploredCellIndexes.Add(current);

        // Önceki odanın rengi
        if (highlightedCell != null)
        {
            bool wasExplored = highlightedCell.cellList.Any(idx => exploredCellIndexes.Contains(idx));
            TintCell(highlightedCell, wasExplored ? defaultTint : unexploredTint);
        }

        // Yeni oda highlight
        highlightedCell = null;
        foreach (var cell in trackedCells)
        {
            if (cell.cellList.Contains(current))
            {
                highlightedCell = cell;
                ShowCell(cell);
                TintCell(cell, highlightTint);
                break;
            }
        }

        lastHighlightedIndex = current;

        // Oda değişince focus modunda kamera hedefini ANINDA güncelle (titreme yok)
        if (!isOverviewMode)
            SnapCameraToTarget();
    }

    // ─── Kamera güncelleme ────────────────────────────────────────────────────
    private void UpdateMinimapCamera()
    {
        if (minimapCam == null) return;

        // Hedef değerleri belirle
        if (isOverviewMode)
        {
            camTargetPos  = overviewCenter;
            camTargetSize = overviewOrthoSize;
        }
        else
        {
            Vector3 roomPos = GetCurrentRoomWorldPos();
            roomPos.z      = -10f;
            camTargetPos   = roomPos;
            camTargetSize  = focusOrthoSize;
        }

        if (!cameraInitialized)
        {
            // İlk frame: sıfır gecikme ile tam hedefe snap
            minimapCam.transform.position = camTargetPos;
            minimapCam.orthographicSize   = camTargetSize;
            cameraInitialized             = true;
            return;
        }

        // Sadece mod geçişinde smooth (Tab basılınca), normal seyahat anlık
        float lerpT = Time.deltaTime * overviewLerpSpeed;
        minimapCam.transform.position = Vector3.Lerp(minimapCam.transform.position, camTargetPos,  lerpT);
        minimapCam.orthographicSize   = Mathf.Lerp(minimapCam.orthographicSize,    camTargetSize, lerpT);
    }

    /// Focus modunda kamera hedefini sıfır gecikme ile odaya kilitle
    private void SnapCameraToTarget()
    {
        if (minimapCam == null || isOverviewMode) return;
        Vector3 pos = GetCurrentRoomWorldPos();
        pos.z = -10f;
        minimapCam.transform.position = pos;
        minimapCam.orthographicSize   = focusOrthoSize;
    }

    private Vector3 GetCurrentRoomWorldPos()
    {
        if (highlightedCell != null) return highlightedCell.transform.position;
        return overviewCenter;
    }

    // ─── Kamera kurulumu ─────────────────────────────────────────────────────
    private void SetupCamera()
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

        minimapCam.orthographic       = true;
        minimapCam.orthographicSize   = focusOrthoSize;
        minimapCam.cullingMask        = 1 << MINIMAP_LAYER;
        minimapCam.clearFlags         = CameraClearFlags.SolidColor;
        minimapCam.backgroundColor    = new Color(0.04f, 0.04f, 0.08f, 1f);
        minimapCam.targetTexture      = renderTexture;
        minimapCam.depth              = 10;

        // Başlangıç konumunu overview'a koy — ilk snap BuildMinimap'in
        // sonunda RevealRoomAndNeighbours → OnRoomChanged zinciriyle gelecek
        minimapCam.transform.position = overviewCenter;
    }

    private void SetupUI()
    {
        if (canvas == null)
        {
            var canvasGO = new GameObject("MinimapCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight  = 0.5f;

            var bgGO = new GameObject("MinimapBG", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvas.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin        = new Vector2(1, 1);
            bgRT.anchorMax        = new Vector2(1, 1);
            bgRT.pivot            = new Vector2(1, 1);
            bgRT.anchoredPosition = new Vector2(-screenMargin, -screenMargin);
            bgRT.sizeDelta        = new Vector2(minimapScreenSize + 6f, minimapScreenSize + 6f);
            bgImage               = bgGO.GetComponent<Image>();
            bgImage.color         = new Color(0.15f, 0.15f, 0.2f, 0.9f);

            var imgGO = new GameObject("MinimapImage", typeof(RectTransform), typeof(RawImage));
            imgGO.transform.SetParent(bgGO.transform, false);
            rawImage  = imgGO.GetComponent<RawImage>();
            var imgRT = imgGO.GetComponent<RectTransform>();
            imgRT.anchorMin = Vector2.zero;
            imgRT.anchorMax = Vector2.one;
            imgRT.offsetMin = new Vector2(3f, 3f);
            imgRT.offsetMax = new Vector2(-3f, -3f);
        }

        rawImage.texture = renderTexture;
    }

    private void HideCell(Cell cell)
    {
        if (cell.spriteRenderer != null) cell.spriteRenderer.enabled = false;
        if (cell.roomSprite     != null) cell.roomSprite.enabled     = false;
    }

    private void ShowCell(Cell cell)
    {
        if (cell.spriteRenderer != null) cell.spriteRenderer.enabled = true;
        if (cell.roomSprite     != null) cell.roomSprite.enabled     = true;
    }

    private void TintCell(Cell cell, Color color)
    {
        if (cell.spriteRenderer != null) cell.spriteRenderer.color = color;
        if (cell.roomSprite     != null) cell.roomSprite.color     = color;
    }

    private void ExcludeLayerFromMainCamera()
    {
        Camera main = Camera.main;
        if (main != null) main.cullingMask &= ~(1 << MINIMAP_LAYER);

        if (RoomTransitionManager.instance?.cameraController != null)
        {
            var cam = RoomTransitionManager.instance.cameraController.GetComponent<Camera>();
            if (cam != null) cam.cullingMask &= ~(1 << MINIMAP_LAYER);
        }
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
    }
}