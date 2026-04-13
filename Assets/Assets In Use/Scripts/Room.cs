using System.Collections.Generic;
using System.Linq;
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
    private readonly List<(Vector2 offset, EdgeDirection dir)> placedDoorInfos = new();
    private const float WallThickness = 0.3f;
    private const float DoorGapHalf = 0.8f;
    [HideInInspector] public RoomType roomType;
    public void SetRoomType(RoomType type)
    {
        roomType = type;
    }
    public void SetupRoom(Cell currentCell, RoomScriptable room)
    {
        roomType = currentCell.roomType;
        InstantiateRoomDesign(room, currentCell.roomShape);

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
        var tracker = gameObject.AddComponent<RoomEnemyTracker>();
        tracker.Initialize(RoomManager.instance.closedDoorPrefab);
    }

    private void InstantiateRoomDesign(RoomScriptable room, RoomShape shape)
    {
        GameObject designPrefab = null;

        if (room != null && room.roomDesignPrefabs != null && room.roomDesignPrefabs.Length > 0)
        {
            designPrefab = room.roomDesignPrefabs[Random.Range(0, room.roomDesignPrefabs.Length)];
        }
        else
        {
            var fallback = System.Array.Find(RoomManager.instance.rooms,
                r => r != null && r.roomShape == shape && r.roomDesignPrefabs != null && r.roomDesignPrefabs.Length > 0);
            if (fallback != null)
                designPrefab = fallback.roomDesignPrefabs[Random.Range(0, fallback.roomDesignPrefabs.Length)];
        }

        if (designPrefab != null)
        {
            var design = Instantiate(designPrefab, transform);
            design.transform.localPosition = Vector3.zero;
            design.transform.localRotation = Quaternion.identity;

            Debug.Log($"[RoomDesign] '{designPrefab.name}' instantiate edildi. Parent: {design.transform.parent?.name}");
            var pooters = design.GetComponentsInChildren<PooterAI>(true);
            var babies = design.GetComponentsInChildren<BabyAI>(true);
            var dofs = design.GetComponentsInChildren<DOF_AI>(true);
            Debug.Log($"[RoomDesign] Prefab içinde → Pooter: {pooters.Length}, Baby: {babies.Length}, DOF: {dofs.Length}");

            var renderers = design.GetComponentsInChildren<SpriteRenderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                Vector3 centerOffset = bounds.center - transform.position;
                design.transform.localPosition = -centerOffset;
            }
        }
    }
    private const float DoorInset = 0.2f; // Bu değeri değiştir
    public void SetupOneByOne(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var currentCell = cell.cellList[0];
        var half = RoomManager.instance.RoomInnerHalfSize;

        TryPlaceDoor(currentCell, new Vector2(0, half.y - DoorInset), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(0, -half.y + DoorInset), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(-half.x + DoorInset, 0), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(half.x - DoorInset, 0), EdgeDirection.Right, floorplan, cellList, cell);
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

    private EdgeDirection GetOppositeDirection(EdgeDirection dir)
    {
        return dir switch
        {
            EdgeDirection.Up    => EdgeDirection.Down,
            EdgeDirection.Down  => EdgeDirection.Up,
            EdgeDirection.Left  => EdgeDirection.Right,
            EdgeDirection.Right => EdgeDirection.Left,
            _                   => dir
        };
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

    private void TryPlaceDoor(int fromIndex, Vector2 positionOffset, EdgeDirection direction, 
    int[] floorplan, List<Cell> cellList, Cell currentCell)
    {
        int neighbourIndex = fromIndex + GetOffset(direction);

        if (neighbourIndex < 0 || neighbourIndex >= floorplan.Length) return;
        if (floorplan[neighbourIndex] != 1) return;

        var foundCell = cellList.FirstOrDefault(x => x.cellList.Contains(neighbourIndex));

        if (foundCell == null) return; // null guard eklendi
        if (foundCell.roomType == RoomType.Secret) return;

        var door = Instantiate(RoomManager.instance.doorPrefab, transform);
        door.gameObject.tag = "Door";
        door.transform.localPosition = new Vector3(positionOffset.x, positionOffset.y, 0f);

        // İki odanın tipini karşılaştır, hangisi daha "özel" ise onu kullan
        RoomType displayType = GetDominantRoomType(currentCell.roomType, foundCell.roomType);

        SetupDoor(door, direction, displayType);
        door.SetupTransition(direction, neighbourIndex);
        door.targetRoomType = foundCell.roomType;

        placedDoorInfos.Add((positionOffset, direction));
    }

    private RoomType GetDominantRoomType(RoomType a, RoomType b)
    {
        // Kapı sprite'ı olmayan/olmaması gereken tipler
        bool IsNeutral(RoomType t) => t == RoomType.Regular || t == RoomType.Start;

        if (!IsNeutral(a)) return a;
        if (!IsNeutral(b)) return b;
        return RoomType.Regular;
    }

    private void SetupDoor(Door door, EdgeDirection direction, RoomType roomType)
    {
        var doorTypes = GetDoorOptions(roomType);

        Sprite chosenSprite = direction switch
        {
            EdgeDirection.Up    => doorTypes.upDoor,
            EdgeDirection.Down  => doorTypes.downDoor,
            EdgeDirection.Left  => doorTypes.leftDoor,
            EdgeDirection.Right => doorTypes.rightDoor,
            _                   => null
        };

        if (chosenSprite == null)
            Debug.LogWarning($"Sprite null! RoomType: {roomType}, Direction: {direction}");

        door.SetDoorSprite(chosenSprite);
    }

    private DoorScriptable GetDoorOptions(RoomType roomType)
    {
        var door = RoomManager.instance.doors.FirstOrDefault(x => x.roomType == roomType);
        if (door == null)
        {
            Debug.LogWarning($"DoorScriptable bulunamadı: {roomType}, Regular'a fallback yapıldı.");
            door = RoomManager.instance.doors.FirstOrDefault(x => x.roomType == RoomType.Regular);
        }
        return door;
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
        wall.tag = "Wall";
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
        var inner = RoomManager.instance.RoomInnerHalfSize;
        return shape switch
        {
            RoomShape.OneByOne => inner,
            RoomShape.OneByTwo => new Vector2(inner.x, inner.y * 2f + borderThickness()),
            RoomShape.TwoByOne => new Vector2(inner.x * 2f + borderThickness(), inner.y),
            RoomShape.TwoByTwo => new Vector2(inner.x * 2f + borderThickness(), inner.y * 2f + borderThickness()),
            RoomShape.LShape => new Vector2(inner.x * 2f + borderThickness(), inner.y * 2f + borderThickness()),
            _ => inner,
        };

        static float borderThickness() => RoomManager.instance.borderThickness;
    }

    #endregion

    public void SetCollidersActive(bool active)
    {
        var colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders)
        {
            // Kapıları ve trigger'ları atlat, sadece duvarları aç/kapat
            if (col.gameObject.CompareTag("Door")) continue;
            if (col.isTrigger) continue;
            col.enabled = active;
        }
    }
}
