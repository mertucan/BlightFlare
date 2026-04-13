using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTransitionManager : MonoBehaviour
{
    public static RoomTransitionManager instance;

    [Header("References")]
    public Transform player;
    public CameraController cameraController;

    [Header("Transition Settings")]
    public float transitionDuration = 0.4f;
    public float transitionCooldown = 0.15f;

    [Header("Entry Position")]
    [Tooltip("Kapıdan ne kadar içeri ışınlanacağı (unit cinsinden)")]
    public float doorEntryInset = 0.01f;

    [Tooltip("Geçiş sırasında karakterin kapıdan içeri kayma mesafesi")]
    public float playerSlideDistance = 1.5f;

    [Header("Room Bounds")]
    public bool clampPlayerToRoom = true;

    private Dictionary<int, Vector2> roomPositions = new();
    private Dictionary<int, RoomShape> roomShapeMap = new();
    private Dictionary<int, Room> cellToRoom = new();
    private Room currentActiveRoom;
    private int currentRoomCellIndex = 45;
    public int CurrentRoomIndex => currentRoomCellIndex;
    private bool isTransitioning;

    private void Awake()
    {
        instance = this;
    }

    public void RegisterMultiCellRoom(List<int> cellIndices, Vector2 worldPosition, RoomShape shape, Room room)
    {
        foreach (int index in cellIndices)
        {
            roomPositions[index] = worldPosition;
            roomShapeMap[index] = shape;
            cellToRoom[index] = room;
        }

        room.SetCollidersActive(false);
    }

    public void ClearRooms()
    {
        roomPositions.Clear();
        roomShapeMap.Clear();
        cellToRoom.Clear();
        currentActiveRoom = null;
        currentRoomCellIndex = 45;
    }

    public void PlacePlayerAtStart()
    {
        if (player == null) return;
        if (!roomPositions.ContainsKey(45)) return;

        Vector2 startPos = roomPositions[45];
        TeleportPlayer(startPos);
        currentRoomCellIndex = 45;

        if (currentActiveRoom != null)
            currentActiveRoom.SetCollidersActive(false);

        if (cellToRoom.ContainsKey(45))
        {
            currentActiveRoom = cellToRoom[45];
            currentActiveRoom.SetCollidersActive(true);
        }

        RoomShape shape = roomShapeMap.ContainsKey(45) ? roomShapeMap[45] : RoomShape.OneByOne;
        bool isLarge = shape != RoomShape.OneByOne;
        cameraController?.SnapToRoom(startPos, GetRoomHalfSize(shape), isLarge);

        // Start() bitmesini bekle, sonra tracker'ı bilgilendir
        StartCoroutine(NotifyStartRoomTrackerNextFrame());
    }

    private System.Collections.IEnumerator NotifyStartRoomTrackerNextFrame()
    {
        yield return null;
        if (cellToRoom.ContainsKey(45))
        {
            var tracker = cellToRoom[45].GetComponent<RoomEnemyTracker>();
            tracker?.OnPlayerEntered();
            Debug.Log($"[RTM] Start odası tracker'ı bilgilendirildi");
        }
    }
    public void TransitionToRoom(int targetCellIndex, EdgeDirection fromDirection)
    {
        Debug.Log($"TransitionToRoom çağrıldı. target: {targetCellIndex}, isTransitioning: {isTransitioning}, roomPositions içinde: {roomPositions.ContainsKey(targetCellIndex)}");
        
        if (isTransitioning) return;
        if (!roomPositions.ContainsKey(targetCellIndex)) return;

        isTransitioning = true;
        StartCoroutine(TransitionRoutine(targetCellIndex, fromDirection));
    }
    private IEnumerator TransitionRoutine(int targetCellIndex, EdgeDirection fromDirection)
    {
        SetPlayerMovement(false);
        DestroyAllProjectiles();

        var rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
        RigidbodyType2D prevBodyType = RigidbodyType2D.Dynamic;
        RigidbodyInterpolation2D prevInterp = RigidbodyInterpolation2D.None;

        if (rb != null)
        {
            prevBodyType = rb.bodyType;
            prevInterp = rb.interpolation;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (currentActiveRoom != null)
            currentActiveRoom.SetCollidersActive(false);

        RoomShape targetShape = roomShapeMap.ContainsKey(targetCellIndex)
            ? roomShapeMap[targetCellIndex]
            : RoomShape.OneByOne;

        Vector2 newRoomCenter = roomPositions[targetCellIndex];
        Vector2 entryOffset = GetEntryOffset(fromDirection, targetShape);
        Vector2 doorPosition = newRoomCenter + entryOffset;

        SetPlayerPos(doorPosition, rb);
        Physics2D.SyncTransforms();

        yield return new WaitForFixedUpdate();

        SetPlayerPos(doorPosition, rb);
        Physics2D.SyncTransforms();

        // Önceki odadan çık
        var leavingTracker = cellToRoom.ContainsKey(currentRoomCellIndex)
            ? cellToRoom[currentRoomCellIndex]?.GetComponent<RoomEnemyTracker>()
            : null;
        leavingTracker?.OnPlayerExited();

        currentRoomCellIndex = targetCellIndex;

        // Yeni odaya gir
        var enteringTracker = cellToRoom.ContainsKey(targetCellIndex)
            ? cellToRoom[targetCellIndex]?.GetComponent<RoomEnemyTracker>()
            : null;
        enteringTracker?.OnPlayerEntered();

        if (cellToRoom.ContainsKey(targetCellIndex))
        {
            currentActiveRoom = cellToRoom[targetCellIndex];
            currentActiveRoom.SetCollidersActive(true);
        }

        bool isLarge = targetShape != RoomShape.OneByOne;

        cameraController?.SlideToRoom(newRoomCenter, GetRoomHalfSize(targetShape), isLarge, transitionDuration);

        Vector2 slideDir = GetInwardDirection(fromDirection);
        Vector2 slideEnd = doorPosition + slideDir * playerSlideDistance;
        float slideDuration = Mathf.Min(transitionDuration * 0.5f, 0.25f);
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / slideDuration));
            SetPlayerPos(Vector2.Lerp(doorPosition, slideEnd, t), rb);
            yield return null;
        }

        SetPlayerPos(slideEnd, rb);

        float remaining = transitionDuration - slideDuration;
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        if (rb != null)
        {
            rb.bodyType = prevBodyType;
            rb.interpolation = prevInterp;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        SetPlayerMovement(true);

        yield return new WaitForSeconds(transitionCooldown);
        isTransitioning = false;
    }
    private void DestroyAllProjectiles()
    {
        foreach (var p in FindObjectsByType<BabyProjectile>(FindObjectsSortMode.None))
            Destroy(p.gameObject);

        foreach (var p in FindObjectsByType<PooterProjectile>(FindObjectsSortMode.None))
            Destroy(p.gameObject);
    }

    private void TeleportPlayer(Vector2 position)
    {
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var prevType = rb.bodyType;
            var prevInterp = rb.interpolation;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.interpolation = RigidbodyInterpolation2D.None;
            rb.bodyType = RigidbodyType2D.Kinematic;

            player.position = new Vector3(position.x, position.y, player.position.z);
            rb.position = position;
            Physics2D.SyncTransforms();

            rb.bodyType = prevType;
            rb.interpolation = prevInterp;
        }
        else
        {
            player.position = new Vector3(position.x, position.y, player.position.z);
            Physics2D.SyncTransforms();
        }
    }

    private void SetPlayerPos(Vector2 position, Rigidbody2D rb)
    {
        if (player == null) return;
        player.position = new Vector3(position.x, position.y, player.position.z);
        if (rb != null) rb.position = position;
    }

    private Vector2 GetInwardDirection(EdgeDirection fromDirection)
    {
        return fromDirection switch
        {
            EdgeDirection.Up    => Vector2.up,
            EdgeDirection.Down  => Vector2.down,
            EdgeDirection.Left  => Vector2.left,
            EdgeDirection.Right => Vector2.right,
            _ => Vector2.zero,
        };
    }

    private void SetPlayerMovement(bool enabled)
    {
        if (player == null) return;

        var mc = player.GetComponent<MovementController>();
        if (mc != null) mc.enabled = enabled;

        var im = player.GetComponent<IsaacMovement>();
        if (im != null) im.enabled = enabled;
    }

    private void LateUpdate()
    {
        if (isTransitioning) return;
        if (!clampPlayerToRoom || player == null) return;
        if (!roomPositions.ContainsKey(currentRoomCellIndex)) return;

        // Player herhangi bir Door trigger'ı içindeyse clamp yapma
        var playerCol = player.GetComponent<Collider2D>();
        if (playerCol != null)
        {
            var results = new List<Collider2D>();
            playerCol.Overlap(ContactFilter2D.noFilter, results);
            foreach (var col in results)
            {
                if (col != null && col.CompareTag("Door"))
                    return;
            }
        }

        Vector2 center = roomPositions[currentRoomCellIndex];
        RoomShape shape = roomShapeMap.ContainsKey(currentRoomCellIndex)
            ? roomShapeMap[currentRoomCellIndex]
            : RoomShape.OneByOne;
        Vector2 halfSize = GetRoomHalfSize(shape);

        // Clamp sınırlarını biraz genişlet ki kapılara ulaşılabilsin
        float margin = 1.5f;
        Vector3 pos = player.position;
        pos.x = Mathf.Clamp(pos.x, center.x - halfSize.x - margin, center.x + halfSize.x + margin);
        pos.y = Mathf.Clamp(pos.y, center.y - halfSize.y - margin, center.y + halfSize.y + margin);
        player.position = pos;
    }

    private Vector2 GetEntryOffset(EdgeDirection doorDirection, RoomShape targetShape)
    {
        Vector2 half = GetRoomHalfSize(targetShape);
        float inset = doorEntryInset;

        switch (doorDirection)
        {
            case EdgeDirection.Up:    return new Vector2(0, -half.y + inset);
            case EdgeDirection.Down:  return new Vector2(0,  half.y - inset);
            case EdgeDirection.Left:  return new Vector2( half.x - inset, 0);
            case EdgeDirection.Right: return new Vector2(-half.x + inset, 0);
        }
        return Vector2.zero;
    }

    private Vector2 GetRoomHalfSize(RoomShape shape)
    {
        var inner = RoomManager.instance != null
            ? RoomManager.instance.RoomInnerHalfSize
            : new Vector2(4f, 3f);

        return shape switch
        {
            RoomShape.OneByOne => inner,
            RoomShape.OneByTwo => new Vector2(inner.x, inner.y * 2.2f),
            RoomShape.TwoByOne => new Vector2(inner.x * 2.2f, inner.y),
            RoomShape.TwoByTwo => new Vector2(inner.x * 2.2f, inner.y * 2.2f),
            RoomShape.LShape   => new Vector2(inner.x * 2.2f, inner.y * 2.2f),
            _                  => inner,
        };
    }
}
