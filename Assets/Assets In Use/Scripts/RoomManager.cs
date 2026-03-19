using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private List<Room> createdRooms;

    [Header("Room Spacing")]
    [Tooltip("Odalar arası ekstra boşluk (oda boyutuna eklenir)")]
    public float roomGap = 0.5f;

    [Header("Room Border")]
    [Tooltip("Oda kenarındaki duvar kalınlığı (sprite'ın dış kenarından oynanabilir alana kadar)")]
    public float borderThickness = 1.5f;

    [Header("Prefab References")]
    public Room roomPrefab;
    public Door doorPrefab;

    [Header("Scriptable Object References")]
    public DoorScriptable[] doors;
    public RoomScriptable[] rooms;

    public static RoomManager instance;

    public Vector2 RoomInnerHalfSize { get; private set; }

    private const float CellSize = 0.5f;

    private void Awake()
    {
        instance = this;
        createdRooms = new List<Room>();
    }

    public void SetupRooms(List<Cell> spawnedCells)
    {
        for (int i = createdRooms.Count - 1; i >= 0; i--)
        {
            Destroy(createdRooms[i].gameObject);
        }

        createdRooms.Clear();

        Vector2 measuredSize = MeasureRoomDesignSize();
        float spacingX = measuredSize.x + roomGap;
        float spacingY = measuredSize.y + roomGap;

        RoomInnerHalfSize = new Vector2(
            measuredSize.x / 2f - borderThickness,
            measuredSize.y / 2f - borderThickness);

        foreach (var currentCell in spawnedCells)
        {
            var foundRoom = rooms.FirstOrDefault(x => x.roomShape == currentCell.roomShape && x.roomType == currentCell.roomType && DoesTileMatchCell(x.occupiedTiles, currentCell));

            var currentPosition = currentCell.transform.position;

            var convertedPosition = new Vector2(
                (currentPosition.x / CellSize) * spacingX,
                (currentPosition.y / CellSize) * spacingY);

            var spawnedRoom = Instantiate(roomPrefab, convertedPosition, Quaternion.identity);

            spawnedRoom.SetupRoom(currentCell, foundRoom);

            createdRooms.Add(spawnedRoom);

            if (RoomTransitionManager.instance != null)
            {
                RoomTransitionManager.instance.RegisterMultiCellRoom(
                    currentCell.cellList, convertedPosition, currentCell.roomShape, spawnedRoom);
            }
        }

        if (RoomTransitionManager.instance != null)
        {
            RoomTransitionManager.instance.PlacePlayerAtStart();
        }
    }

    private Vector2 MeasureRoomDesignSize()
    {
        foreach (var room in rooms)
        {
            if (room == null || room.roomDesignPrefabs == null || room.roomDesignPrefabs.Length == 0)
                continue;

            var temp = Instantiate(room.roomDesignPrefabs[0], Vector3.one * 9999f, Quaternion.identity);
            var renderers = temp.GetComponentsInChildren<SpriteRenderer>();

            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                Vector2 size = new Vector2(bounds.size.x, bounds.size.y);
                Destroy(temp);
                return size;
            }

            Destroy(temp);
        }

        return new Vector2(13f, 10f);
    }

    private bool DoesTileMatchCell(int[] occupiedTiles, Cell cell)
    {
        if(occupiedTiles.Length != cell.cellList.Count)
            return false;

        int minIndex = cell.cellList.Min();
        List<int> normalizedCell = new List<int>();

        foreach(int index in cell.cellList)
        {
            int dx = (index % 10) - (minIndex % 10);
            int dy = (index / 10) - (minIndex / 10);

            normalizedCell.Add(dy * 10 + dx);
        }

        normalizedCell.Sort();
        int[] sortedOccupied = (int[])occupiedTiles.Clone();
        Array.Sort(sortedOccupied);

        return normalizedCell.SequenceEqual(sortedOccupied);
    }
}
