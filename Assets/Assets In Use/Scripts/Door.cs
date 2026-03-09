using UnityEngine;

public class Door : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [HideInInspector] public EdgeDirection direction;
    [HideInInspector] public int targetCellIndex = -1;

    public void SetDoorSprite(Sprite door)
    {
        spriteRenderer.sprite = door;
    }

    public void SetupTransition(EdgeDirection dir, int targetIndex)
    {
        direction = dir;
        targetCellIndex = targetIndex;

        var col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.2f, 1.2f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (targetCellIndex < 0) return;
        if (!other.CompareTag("Player")) return;

        RoomTransitionManager.instance?.TransitionToRoom(targetCellIndex, direction);
    }
}
