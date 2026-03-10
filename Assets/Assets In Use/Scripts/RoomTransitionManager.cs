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
    public float transitionCooldown = 0.3f;

    [Header("Room Bounds")]
    public bool clampPlayerToRoom = true;

    private Dictionary<int, Vector2> roomPositions = new();
    private Dictionary<int, RoomShape> roomShapeMap = new();
    private Dictionary<int, Room> cellToRoom = new();
    private Room currentActiveRoom;
    private int currentRoomCellIndex = 45;
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
        player.position = startPos;
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

        Vector2 roomCenter = roomPositions[targetCellIndex];
        Vector2 entryOffset = GetEntryOffset(fromDirection);

        player.position = roomCenter + entryOffset;
        currentRoomCellIndex = targetCellIndex;

        if (currentActiveRoom != null)
            currentActiveRoom.SetCollidersActive(false);

        if (cellToRoom.ContainsKey(targetCellIndex))
        {
            currentActiveRoom = cellToRoom[targetCellIndex];
            currentActiveRoom.SetCollidersActive(true);
        }

        RoomShape shape = roomShapeMap.ContainsKey(targetCellIndex)
            ? roomShapeMap[targetCellIndex]
            : RoomShape.OneByOne;
        bool isLarge = shape != RoomShape.OneByOne;
        cameraController?.SnapToRoom(roomCenter, GetRoomHalfSize(shape), isLarge);

        StartCoroutine(CooldownRoutine());
    }

    private void LateUpdate()
    {
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
            case EdgeDirection.Up:    return new Vector2(0, -1.2f);
            case EdgeDirection.Down:  return new Vector2(0, 1.2f);
            case EdgeDirection.Left:  return new Vector2(3.5f, 0);
            case EdgeDirection.Right: return new Vector2(-3.5f, 0);
        }
        return Vector2.zero;
    }

    private Vector2 GetRoomHalfSize(RoomShape shape)
    {
        switch (shape)
        {
            case RoomShape.OneByOne: return new Vector2(4.5f, 2.0f);
            case RoomShape.OneByTwo: return new Vector2(4.5f, 4.5f);
            case RoomShape.TwoByOne: return new Vector2(10.0f, 2.0f);
            case RoomShape.TwoByTwo: return new Vector2(10.0f, 5.0f);
            case RoomShape.LShape:   return new Vector2(10.0f, 5.0f);
            default:                 return new Vector2(4.5f, 2.0f);
        }
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(transitionCooldown);
        isTransitioning = false;
    }
}
