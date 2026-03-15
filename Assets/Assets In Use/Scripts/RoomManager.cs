using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private List<Room> createdRooms;

    [Header("Offset Variables")]
    public float offsetX;
    public float offsetY;

    [Header("Room Scale")]
    [Tooltip("Odaları dikeyde ölçekler (1.4 ≈ Isaac oranı)")]
    public float roomHeightScale = 1.4f;

    [Header("Prefab References")]
    public Room roomPrefab;
    public Door doorPrefab;

    [Header("Scriptable Object References")]
    public DoorScriptable[] doors;
    public RoomScriptable[] rooms;

    public static RoomManager instance;

    private void Awake()
    {
        instance = this;
        createdRooms = new List<Room>();
    }

    public void SetupRooms(List<Cell> spawnedCells)
    {
        for(int i = createdRooms.Count - 1; i >= 0; i--)
        {
            Destroy(createdRooms[i].gameObject);
        }

        createdRooms.Clear();

        if (RoomTransitionManager.instance != null)
            RoomTransitionManager.instance.SetHeightScale(roomHeightScale);

        foreach(var currentCell in spawnedCells)
        {
            var foundRoom = rooms.FirstOrDefault(x => x.roomShape == currentCell.roomShape && x.roomType == currentCell.roomType && DoesTileMatchCell(x.occupiedTiles, currentCell));

            var currentPosition = currentCell.transform.position;

            var convertedPosition = new Vector2(
                currentPosition.x * offsetX,
                currentPosition.y * offsetY * roomHeightScale);

            var spawnedRoom = Instantiate(roomPrefab, convertedPosition, Quaternion.identity);
            spawnedRoom.transform.localScale = new Vector3(1f, roomHeightScale, 1f);

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
