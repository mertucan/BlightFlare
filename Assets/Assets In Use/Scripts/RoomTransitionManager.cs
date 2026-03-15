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

    [Header("Room Bounds")]
    public bool clampPlayerToRoom = true;

    private Dictionary<int, Vector2> roomPositions = new();
    private Dictionary<int, RoomShape> roomShapeMap = new();
    private Dictionary<int, Room> cellToRoom = new();
    private Room currentActiveRoom;
    private int currentRoomCellIndex = 45;
    public int CurrentRoomIndex => currentRoomCellIndex;
    private bool isTransitioning;
    private float heightScale = 1f;

    private void Awake()
    {
        instance = this;
    }

    public void SetHeightScale(float scale)
    {
        heightScale = scale;
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
    }

    public void TransitionToRoom(int targetCellIndex, EdgeDirection fromDirection)
    {
        if (isTransitioning) return;
        if (!roomPositions.ContainsKey(targetCellIndex)) return;

        isTransitioning = true;
        StartCoroutine(TransitionRoutine(targetCellIndex, fromDirection));
    }

    private IEnumerator TransitionRoutine(int targetCellIndex, EdgeDirection fromDirection)
    {
        SetPlayerMovement(false);

        if (currentActiveRoom != null)
            currentActiveRoom.SetCollidersActive(false);

        Vector2 newRoomCenter = roomPositions[targetCellIndex];
        Vector2 entryOffset = GetEntryOffset(fromDirection);

        TeleportPlayer(newRoomCenter + entryOffset);

        yield return new WaitForFixedUpdate();

        currentRoomCellIndex = targetCellIndex;

        if (cellToRoom.ContainsKey(targetCellIndex))
        {
            currentActiveRoom = cellToRoom[targetCellIndex];
            currentActiveRoom.SetCollidersActive(true);
        }

        RoomShape shape = roomShapeMap.ContainsKey(targetCellIndex)
            ? roomShapeMap[targetCellIndex]
            : RoomShape.OneByOne;
        bool isLarge = shape != RoomShape.OneByOne;

        cameraController?.SlideToRoom(newRoomCenter, GetRoomHalfSize(shape), isLarge, transitionDuration);

        yield return new WaitForSeconds(transitionDuration);

        SetPlayerMovement(true);

        yield return new WaitForSeconds(transitionCooldown);
        isTransitioning = false;
    }

    private void TeleportPlayer(Vector2 position)
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = position;
        }

        player.position = new Vector3(position.x, position.y, player.position.z);
        Physics2D.SyncTransforms();
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

        Vector2 center = roomPositions[currentRoomCellIndex];
        RoomShape shape = roomShapeMap.ContainsKey(currentRoomCellIndex)
            ? roomShapeMap[currentRoomCellIndex]
            : RoomShape.OneByOne;
        Vector2 halfSize = GetRoomHalfSize(shape);

        Vector3 pos = player.position;
        pos.x = Mathf.Clamp(pos.x, center.x - halfSize.x, center.x + halfSize.x);
        pos.y = Mathf.Clamp(pos.y, center.y - halfSize.y, center.y + halfSize.y);
        player.position = pos;
    }

    private Vector2 GetEntryOffset(EdgeDirection doorDirection)
    {
        switch (doorDirection)
        {
            case EdgeDirection.Up:    return new Vector2(0, -1.2f * heightScale);
            case EdgeDirection.Down:  return new Vector2(0, 1.2f * heightScale);
            case EdgeDirection.Left:  return new Vector2(3.5f, 0);
            case EdgeDirection.Right: return new Vector2(-3.5f, 0);
        }
        return Vector2.zero;
    }

    private Vector2 GetRoomHalfSize(RoomShape shape)
    {
        Vector2 baseSize = shape switch
        {
            RoomShape.OneByOne => new Vector2(4.5f, 2.0f),
            RoomShape.OneByTwo => new Vector2(4.5f, 4.5f),
            RoomShape.TwoByOne => new Vector2(10.0f, 2.0f),
            RoomShape.TwoByTwo => new Vector2(10.0f, 5.0f),
            RoomShape.LShape   => new Vector2(10.0f, 5.0f),
            _                  => new Vector2(4.5f, 2.0f),
        };
        baseSize.y *= heightScale;
        return baseSize;
    }
}
