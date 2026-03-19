using UnityEngine;
using UnityEngine.InputSystem;

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

        bool isHorizontal = dir == EdgeDirection.Left || dir == EdgeDirection.Right;
        col.size = isHorizontal ? new Vector2(1.5f, 1.2f) : new Vector2(1.2f, 1.5f);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryTransition(other);
    private void OnTriggerStay2D(Collider2D other) => TryTransition(other);

    private void TryTransition(Collider2D other)
    {
        if (targetCellIndex < 0) return;
        if (!other.CompareTag("Player")) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool pressingCorrectKey = direction switch
        {
            EdgeDirection.Up => kb[Key.W].isPressed || kb[Key.UpArrow].isPressed,
            EdgeDirection.Down => kb[Key.S].isPressed || kb[Key.DownArrow].isPressed,
            EdgeDirection.Left => kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed,
            EdgeDirection.Right => kb[Key.D].isPressed || kb[Key.RightArrow].isPressed,
            _ => false
        };

        if (!pressingCorrectKey) return;

        RoomTransitionManager.instance?.TransitionToRoom(targetCellIndex, direction);
    }
}
