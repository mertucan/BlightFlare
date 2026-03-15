using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public enum EdgeDirection
{
    Up,
    Down,
    Left,
    Right
}

public class Room : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    private readonly List<(Vector2 offset, EdgeDirection dir)> placedDoorInfos = new();
    private const float WallThickness = 0.5f;
    private const float DoorGapHalf = 0.8f;

    public void SetupRoom(Cell currentCell, RoomScriptable room)
    {
        spriteRenderer.sprite = room.roomVariations[Random.Range(0, room.roomVariations.Length)];

        if (currentCell.roomType == RoomType.Secret) return;

        placedDoorInfos.Clear();

        var floorplan = MapGenerator.instance.getFloorPlan;
        var cellList = MapGenerator.instance.getSpawnedCells;

        switch (currentCell.roomShape)
        {
            case RoomShape.OneByOne:
                SetupOneByOne(currentCell, floorplan, cellList);
                break;

            case RoomShape.OneByTwo:
                SetupOneByTwo(currentCell, floorplan, cellList);
                break;

            case RoomShape.TwoByOne:
                SetupTwoByOne(currentCell, floorplan, cellList);
                break;

            case RoomShape.TwoByTwo:
                SetupTwoByTwo(currentCell, floorplan, cellList);
                break;

            case RoomShape.LShape:
                SetupLShapeRoom(currentCell, floorplan, cellList);
                break;

            default:
                break;
        }

        GenerateWalls(currentCell.roomShape, currentCell);
    }

    public void SetupOneByOne(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var currentCell = cell.cellList[0];

        TryPlaceDoor(currentCell, new Vector2(0, 1.75f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(0, -1.75f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(-4.25f, 0), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(4.25f, 0), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupOneByTwo(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];

        TryPlaceDoor(cellA, new Vector2(0f, 4f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(-4.25f, 2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(4.25f, 2.6125f), EdgeDirection.Right, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(0f, -4f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(-4.25f, -2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(4.25f, -2.6125f), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupTwoByOne(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];

        TryPlaceDoor(cellA, new Vector2(-5f, 1.5f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(-9.75f, 0f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(-5f, -1.5f), EdgeDirection.Down, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(5f, 1.5f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(5f, -1.5f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(9.75f, 0f), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupTwoByTwo(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];
        var cellC = cell.cellList[2];
        var cellD = cell.cellList[3];

        TryPlaceDoor(cellA, new Vector2(-5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);

        TryPlaceDoor(cellA, new Vector2(-9.75f, 2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(cellC, new Vector2(-9.75f, -2.6125f), EdgeDirection.Left, floorplan, cellList, cell);

        TryPlaceDoor(cellC, new Vector2(-5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellD, new Vector2(5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(9.75f, 2.6125f), EdgeDirection.Right, floorplan, cellList, cell);
        TryPlaceDoor(cellD, new Vector2(9.75f, -2.6125f), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupLShapeRoom(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];
        var cellC = cell.cellList[2];

        if (cellA + 1 == cellB && cellA + 10 == cellC)
        {
            TryPlaceDoor(cellA, new Vector2(-5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-9.75f, 2.6125f), EdgeDirection.Left, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(9.75f, 2.6375f), EdgeDirection.Right, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(5.3125f, 1f), EdgeDirection.Down, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(-5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(-1f, -2.6375f), EdgeDirection.Right, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(-9.75f, -2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
        }
        else if (cellA + 1 == cellB && cellB + 10 == cellC)
        {
            TryPlaceDoor(cellA, new Vector2(-5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-9.75f, 2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-5.3125f, 1f), EdgeDirection.Down, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(9.75f, 2.6375f), EdgeDirection.Right, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(9.75f, -2.6375f), EdgeDirection.Right, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(1, -2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
        }
        else if (cellA + 10 == cellB)
        {
            TryPlaceDoor(cellA, new Vector2(-5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-9.75f, 2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-1f, 2.6125f), EdgeDirection.Right, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(-5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-9.75f, -2.6125f), EdgeDirection.Left, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(5.3125f, -1), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(9.75f, -2.6375f), EdgeDirection.Right, floorplan, cellList, cell);
        }
        else if (cellA + 10 == cellC)
        {
            TryPlaceDoor(cellA, new Vector2(5.3125f, 4.5f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(1, 2.6125f), EdgeDirection.Left, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(9.75f, 2.6125f), EdgeDirection.Right, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(-5.3125f, -1f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-9.75f, -2.6125f), EdgeDirection.Left, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(5.3125f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(9.75f, -2.6375f), EdgeDirection.Right, floorplan, cellList, cell);
        }
    }

    private void TryPlaceDoor(int fromIndex, Vector2 positionOffset, EdgeDirection direction, int[] floorplan, List<Cell> cellList, Cell currentCell)
    {
        int neighbourIndex = fromIndex + GetOffset(direction);

        if (neighbourIndex < 0 || neighbourIndex >= floorplan.Length) return;

        if (floorplan[neighbourIndex] != 1) return;

        var foundCell = cellList.FirstOrDefault(x => x.cellList.Contains(neighbourIndex));

        if (foundCell.roomType == RoomType.Secret) return;

        var door = Instantiate(RoomManager.instance.doorPrefab, transform);

        door.transform.localPosition = new Vector3(positionOffset.x, positionOffset.y, 0f);

        float hScale = RoomManager.instance != null ? RoomManager.instance.roomHeightScale : 1f;
        if (hScale > 0f && Mathf.Abs(hScale - 1f) > 0.001f)
            door.transform.localScale = new Vector3(1f, 1f / hScale, 1f);

        SetupDoor(door, direction, currentCell.roomType == RoomType.Regular ? foundCell.roomType : currentCell.roomType);
        door.SetupTransition(direction, neighbourIndex);

        placedDoorInfos.Add((positionOffset, direction));
    }

    private void SetupDoor(Door door, EdgeDirection direction, RoomType roomType)
    {
        var doorTypes = GetDoorOptions(roomType);

        switch (direction)
        {
            case EdgeDirection.Up:
                door.SetDoorSprite(doorTypes.upDoor);
                break;

            case EdgeDirection.Down:
                door.SetDoorSprite(doorTypes.downDoor);
                break;

            case EdgeDirection.Left:
                door.SetDoorSprite(doorTypes.leftDoor);
                break;

            case EdgeDirection.Right:
                door.SetDoorSprite(doorTypes.rightDoor);
                break;

            default:
                break;
        }
    }

    private DoorScriptable GetDoorOptions(RoomType roomType)
    {
        return RoomManager.instance.doors.FirstOrDefault(x => x.roomType == roomType);
    }

    private int GetOffset(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.Up:
                return -10;

            case EdgeDirection.Down:
                return 10;

            case EdgeDirection.Right:
                return 1;

            case EdgeDirection.Left:
                return -1;
        }

        return 0;
    }

    #region Wall Generation

    private void GenerateWalls(RoomShape shape, Cell cell)
    {
        Vector2 halfSize = GetWallHalfSize(shape);

        GenerateRectWalls(halfSize);

        if (shape == RoomShape.LShape)
            GenerateLShapeInnerWalls(cell, halfSize);
    }

    private void GenerateRectWalls(Vector2 halfSize)
    {
        GenerateEdgeWall(true, halfSize.y, -halfSize.x, halfSize.x, EdgeDirection.Up);
        GenerateEdgeWall(true, -halfSize.y, -halfSize.x, halfSize.x, EdgeDirection.Down);
        GenerateEdgeWall(false, -halfSize.x, -halfSize.y, halfSize.y, EdgeDirection.Left);
        GenerateEdgeWall(false, halfSize.x, -halfSize.y, halfSize.y, EdgeDirection.Right);
    }

    private void GenerateLShapeInnerWalls(Cell cell, Vector2 halfSize)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];
        var cellC = cell.cellList[2];

        if (cellA + 1 == cellB && cellA + 10 == cellC)
        {
            GenerateEdgeWall(true, 0f, 0f, halfSize.x, EdgeDirection.Down);
            GenerateEdgeWall(false, 0f, -halfSize.y, 0f, EdgeDirection.Right);
        }
        else if (cellA + 1 == cellB && cellB + 10 == cellC)
        {
            GenerateEdgeWall(true, 0f, -halfSize.x, 0f, EdgeDirection.Down);
            GenerateEdgeWall(false, 0f, -halfSize.y, 0f, EdgeDirection.Left);
        }
        else if (cellA + 10 == cellB)
        {
            GenerateEdgeWall(true, 0f, 0f, halfSize.x, EdgeDirection.Up);
            GenerateEdgeWall(false, 0f, 0f, halfSize.y, EdgeDirection.Right);
        }
        else if (cellA + 10 == cellC)
        {
            GenerateEdgeWall(true, 0f, -halfSize.x, 0f, EdgeDirection.Up);
            GenerateEdgeWall(false, 0f, 0f, halfSize.y, EdgeDirection.Left);
        }
    }

    private void GenerateEdgeWall(bool isHorizontal, float perpPos, float start, float end, EdgeDirection doorDir)
    {
        List<float> doorPositions = new();

        foreach (var (offset, dir) in placedDoorInfos)
        {
            if (dir != doorDir) continue;

            float doorPerp = isHorizontal ? offset.y : offset.x;
            if (Mathf.Abs(doorPerp - perpPos) > 2.5f) continue;

            float doorAlongAxis = isHorizontal ? offset.x : offset.y;
            if (doorAlongAxis < start - 0.5f || doorAlongAxis > end + 0.5f) continue;

            doorPositions.Add(doorAlongAxis);
        }

        doorPositions.Sort();

        float current = start;
        foreach (float doorPos in doorPositions)
        {
            float gapStart = doorPos - DoorGapHalf;
            float gapEnd = doorPos + DoorGapHalf;

            if (gapStart > current + 0.05f)
                CreateWallCollider(isHorizontal, perpPos, current, gapStart);

            current = gapEnd;
        }

        if (current < end - 0.05f)
            CreateWallCollider(isHorizontal, perpPos, current, end);
    }

    private void CreateWallCollider(bool isHorizontal, float perpPos, float start, float end)
    {
        float length = end - start;
        if (length <= 0.01f) return;

        float mid = (start + end) / 2f;

        GameObject wall = new("Wall");
        wall.transform.SetParent(transform);

        if (isHorizontal)
        {
            wall.transform.localPosition = new Vector3(mid, perpPos, 0);
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = new Vector2(length, WallThickness);
        }
        else
        {
            wall.transform.localPosition = new Vector3(perpPos, mid, 0);
            var col = wall.AddComponent<BoxCollider2D>();
            col.size = new Vector2(WallThickness, length);
        }
    }

    private Vector2 GetWallHalfSize(RoomShape shape)
    {
        return shape switch
        {
            RoomShape.OneByOne => new Vector2(4.5f, 2.0f),
            RoomShape.OneByTwo => new Vector2(4.5f, 4.5f),
            RoomShape.TwoByOne => new Vector2(10.0f, 2.0f),
            RoomShape.TwoByTwo => new Vector2(10.0f, 5.0f),
            RoomShape.LShape => new Vector2(10.0f, 5.0f),
            _ => new Vector2(4.5f, 2.0f),
        };
    }

    #endregion

    public void SetCollidersActive(bool active)
    {
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
            col.enabled = active;
    }
}
