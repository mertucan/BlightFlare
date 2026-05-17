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
        foreach (var s in GetComponentsInChildren<SuckerAI>(true))
            enemies.Add(s.gameObject);
        foreach (var h in GetComponentsInChildren<HostAI>(true))   // ← YENİ
            enemies.Add(h.gameObject);
        foreach (var h in GetComponentsInChildren<BoomFlyAI>(true))   // ← YENİ
            enemies.Add(h.gameObject);
        foreach (var l in GetComponentsInChildren<LokiAI>(true))
            enemies.Add(l.gameObject);
        foreach (var h in GetComponentsInChildren<HollowHead>(true))
            enemies.Add(h.gameObject);

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
            Debug.Log("[RoomEnemyTracker] Tüm düşmanlar öldü, kapılar açıldı.");
        }
    }

    public void OnPlayerEntered()
    {
        playerInRoom = true;
        if (!initialized) return;
        RefreshDoors();
        UpdateDoorLocks();
        var taurus = FindFirstObjectByType<Taurus>();
        if (taurus != null && hasEnemies)
            taurus.OnEnemyRoomEntered();
    }

    public void OnPlayerExited()
    {
        playerInRoom = false;
        var taurus = FindFirstObjectByType<Taurus>();
        if (taurus != null)
            taurus.OnRoomExited();
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
        bool thisIsShop = room != null && room.roomType == RoomType.Shop;

        foreach (var door in doors)
        {
            if (door == null) continue;

            if (thisIsShop)
            {
                door.ShowOpenedShopVisual();
                door.SetLocked(false, null);
                continue;
            }

            if (door.targetRoomType == RoomType.Shop)
            {
                door.MarkAsShopDoor();
                door.SetLocked(true, null);
                continue;
            }

            // Gizli oda kapıları her zaman açık kalmalı
            if (door.targetRoomType == RoomType.Secret)
            {
                door.SetLocked(false, null);
                continue;
            }

            door.SetLocked(shouldLock, closedDoorPrefab);
        }
    }

    public void ForceUnlockDoor(Door door)
    {
        // Bu kapıyı kalıcı olarak listeden çıkar ki
        // UpdateDoorLocks onu tekrar kilitlemesin
        doors.Remove(door);
        door.SetLocked(false, null);
        Debug.Log($"[RoomEnemyTracker] Kapı kalıcı olarak açıldı → {door.gameObject.name}");
    }
}