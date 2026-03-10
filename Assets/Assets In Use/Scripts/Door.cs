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
        col.size = new Vector2(1.0f, 1.0f);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryTransition(other);
    private void OnTriggerStay2D(Collider2D other) => TryTransition(other);

    private void TryTransition(Collider2D other)
    {
        if (targetCellIndex < 0) return;
        if (!other.CompareTag("Player")) return;

        bool pressingCorrectKey = direction switch
        {
            EdgeDirection.Up => Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
            EdgeDirection.Down => Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
            EdgeDirection.Left => Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
            EdgeDirection.Right => Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
            _ => false
        };

        if (!pressingCorrectKey) return;

        RoomTransitionManager.instance?.TransitionToRoom(targetCellIndex, direction);
    }
}
