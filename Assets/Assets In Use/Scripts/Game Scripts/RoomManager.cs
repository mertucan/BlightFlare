using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private List<Room> createdRooms;
    private static readonly HashSet<RoomScriptable> usedTreasureRooms = new();

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

    [Header("Closed Door")]
    public GameObject closedDoorPrefab;

    [Header("Shop Door")]
    public GameObject openedShopDoorPrefab;

    [Header("Room Clear Door Sound")]
    public AudioClip roomClearDoorOpenClip;
    [Range(0f, 1f)] public float roomClearDoorOpenVolume = 1f;

    // ──────────────────────────────────────────────────────────────────────────
    //  GİZLİ ODA AYARLARI
    //  Inspector'da şunları doldurun:
    //    • secretOpenedDoorPrefab → Bomba patladığında duvarın yerinde çıkacak
    //                               sprite-only prefab (sadece SpriteRenderer,
    //                               Collider OLMAMALI). Prefabın varsayılan yönü
    //                               YUKARI bakıyor olmalı; kod gerekli rotasyonu
    //                               otomatik uygular.
    //    • secretExplosionClips   → Açılışta çalacak ses klipleri (opsiyonel).
    // ──────────────────────────────────────────────────────────────────────────
    [Header("Secret Room")]
    [Tooltip("Gizli oda duvarı kırıldığında spawn edilecek açık kapı prefabı")]
    public GameObject openedSecretDoorPrefab;

    [Tooltip("Gizli oda açılma sesi klipleri (boş bırakılabilir)")]
    public AudioClip[] secretExplosionClips;

    // Geriye dönük uyumluluk için property alias
    public GameObject secretOpenedDoorPrefab => openedSecretDoorPrefab;

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
            Destroy(createdRooms[i].gameObject);
        createdRooms.Clear();

        Vector2 measuredSize = MeasureRoomDesignSize();
        float spacingX = measuredSize.x + roomGap;
        float spacingY = measuredSize.y + roomGap;

        RoomInnerHalfSize = new Vector2(
            measuredSize.x / 2f - borderThickness,
            measuredSize.y / 2f - borderThickness);

        foreach (var currentCell in spawnedCells)
        {
            var foundRoom = PickRoomForCell(currentCell);

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

            if (foundRoom == null)
                Debug.LogWarning($"Eşleşen room bulunamadı! Type: {currentCell.roomType}, Shape: {currentCell.roomShape}");
            else
                Debug.Log($"Eşleşti: {foundRoom.name} → Type: {currentCell.roomType}");
        }

        if (RoomTransitionManager.instance != null)
            RoomTransitionManager.instance.PlacePlayerAtStart();
    }

    private RoomScriptable PickRoomForCell(Cell currentCell)
    {
        RoomScriptable[] matchingRooms = GetMatchingRooms(currentCell);

        if (currentCell.roomType == RoomType.Item)
        {
            RoomScriptable[] unusedTreasureRooms = matchingRooms
                .Where(room => !usedTreasureRooms.Contains(room))
                .ToArray();

            if (unusedTreasureRooms.Length == 0 && matchingRooms.Length > 0)
            {
                usedTreasureRooms.Clear();
                unusedTreasureRooms = matchingRooms;
            }

            RoomScriptable treasureRoom = PickRandomRoom(unusedTreasureRooms);
            if (treasureRoom != null)
            {
                usedTreasureRooms.Add(treasureRoom);
            }

            return treasureRoom;
        }

        return PickRandomRoom(matchingRooms);
    }

    private RoomScriptable[] GetMatchingRooms(Cell currentCell)
    {
        return rooms
            .Where(x => x != null
                     && x.roomShape == currentCell.roomShape
                     && x.roomType == currentCell.roomType
                     && DoesTileMatchCell(x.occupiedTiles, currentCell))
            .ToArray();
    }

    private RoomScriptable PickRandomRoom(RoomScriptable[] matchingRooms)
    {
        return matchingRooms.Length > 0
            ? matchingRooms[UnityEngine.Random.Range(0, matchingRooms.Length)]
            : null;
    }

    private Vector2 MeasureRoomDesignSize()
    {
        foreach (var room in rooms)
        {
            if (room == null || room.roomDesignPrefabs == null || room.roomDesignPrefabs.Length == 0)
                continue;

            var temp      = Instantiate(room.roomDesignPrefabs[0], Vector3.one * 9999f, Quaternion.identity);
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
        if (occupiedTiles.Length != cell.cellList.Count) return false;

        int minIndex = cell.cellList.Min();
        List<int> normalizedCell = new List<int>();

        foreach (int index in cell.cellList)
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
