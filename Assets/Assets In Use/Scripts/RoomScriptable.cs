using UnityEngine;

[CreateAssetMenu(fileName = "Room", menuName = "Scriptable Objects/Room")]
public class RoomScriptable : ScriptableObject
{
    public RoomType roomType;
    public RoomShape roomShape;

    public int[] occupiedTiles;

    [Tooltip("Prefab olarak hazırlanmış oda tasarımları. Rastgele biri seçilir.")]
    public GameObject[] roomDesignPrefabs;
}
