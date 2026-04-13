using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomEnemyTracker : MonoBehaviour
{
    public GameObject closedDoorPrefab;

    private List<Door> doors = new();
    private List<GameObject> enemies = new();
    private bool hasEnemies = false;
    private bool playerInRoom = false;
    private bool initialized = false;

    public void Initialize(GameObject prefab)
    {
        closedDoorPrefab = prefab;
        // Kapılar henüz eklenmedi, burada alma
    }

    private void Start()
    {
        enemies = new List<GameObject>();

        foreach (var p in GetComponentsInChildren<PooterAI>(true))
            enemies.Add(p.gameObject);
        foreach (var b in GetComponentsInChildren<BabyAI>(true))
            enemies.Add(b.gameObject);
        foreach (var d in GetComponentsInChildren<DOF_AI>(true))
            enemies.Add(d.gameObject);

        hasEnemies = enemies.Count > 0;
        initialized = true;

        Debug.Log($"[RoomEnemyTracker] {gameObject.name} → {enemies.Count} düşman bulundu");

        if (hasEnemies && playerInRoom)
        {
            RefreshDoors();
            UpdateDoorLocks();
        }
    }

    private void Update()
    {
        if (!initialized) return;
        if (!hasEnemies) return;
        if (!playerInRoom) return;

        bool anyAlive = enemies.Any(e => e != null && e.activeInHierarchy);

        if (!anyAlive)
        {
            hasEnemies = false;
            UpdateDoorLocks();
            Debug.Log($"[RoomEnemyTracker] Tüm düşmanlar öldü, kapılar açıldı.");
        }
    }

    public void OnPlayerEntered()
    {
        playerInRoom = true;

        if (!initialized) return;

        RefreshDoors(); // Her girişte kapıları taze al
        
        if (hasEnemies)
            UpdateDoorLocks();
    }

    public void OnPlayerExited()
    {
        playerInRoom = false;
    }

    private void RefreshDoors()
    {
        doors = GetComponentsInChildren<Door>().ToList();
        Debug.Log($"[RoomEnemyTracker] RefreshDoors → {doors.Count} kapı bulundu");
    }

    private void UpdateDoorLocks()
    {
        bool shouldLock = hasEnemies && playerInRoom;
        

        var room = GetComponent<Room>();
        Debug.Log($"[DoorLock] roomType: {room?.roomType}, shouldLock: {shouldLock}, roomName: {gameObject.name}");
        bool isShop = room != null && room.roomType == RoomType.Shop;

        foreach (var door in doors)
        {
            if (door == null) continue;
            bool doorLeadsToShop = door.targetRoomType == RoomType.Shop;
            door.SetLocked(shouldLock, doorLeadsToShop ? null : closedDoorPrefab);
        }
    }
}