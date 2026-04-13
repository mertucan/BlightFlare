using UnityEngine;
using UnityEngine.InputSystem;

public class Door : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [HideInInspector] public EdgeDirection direction;
    [HideInInspector] public int targetCellIndex = -1;
    [HideInInspector] public RoomType targetRoomType = RoomType.Regular;

    private GameObject closedDoorInstance;
    private bool isLocked = false;

    private BoxCollider2D blockCollider;
    [SerializeField] private float closedDoorInset = 20f;

    private void UpdateBlockCollider(bool shouldBlock)
    {
        if (shouldBlock)
        {
            if (blockCollider == null)
            {
                blockCollider = gameObject.AddComponent<BoxCollider2D>();
                blockCollider.isTrigger = false; // solid collider

                blockCollider.size = direction switch
                {
                    EdgeDirection.Up    => new Vector2(1.6f, 0.5f),
                    EdgeDirection.Down  => new Vector2(1.6f, 0.5f),
                    EdgeDirection.Left  => new Vector2(0.5f, 1.6f),
                    EdgeDirection.Right => new Vector2(0.5f, 1.6f),
                    _ => new Vector2(1f, 1f)
                };
            }
            blockCollider.enabled = true;
        }
        else
        {
            if (blockCollider != null)
                blockCollider.enabled = false;
        }
    }

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

        col.size = dir switch
        {
            EdgeDirection.Up    => new Vector2(1.6f, 0.4f),
            EdgeDirection.Down  => new Vector2(1.6f, 0.4f),
            EdgeDirection.Left  => new Vector2(0.4f, 1.6f),
            EdgeDirection.Right => new Vector2(0.4f, 1.6f),
            _ => new Vector2(0.6f, 0.6f)
        };
    }

    public void SetLocked(bool locked, GameObject closedDoorPrefab = null)
    {
        isLocked = locked;

        if (locked)
        {
            if (closedDoorInstance == null && closedDoorPrefab != null)
            {
                closedDoorInstance = Instantiate(closedDoorPrefab, transform.position, Quaternion.identity, transform);
                Vector3 worldInsetDir = direction switch
                {
                    EdgeDirection.Up    => Vector3.down,   // Üst duvar → aşağı (odanın içine)
                    EdgeDirection.Down  => Vector3.up,     // Alt duvar → yukarı (odanın içine)
                    EdgeDirection.Left  => Vector3.right,  // Sol duvar → sağa (odanın içine)
                    EdgeDirection.Right => Vector3.left,   // Sağ duvar → sola (odanın içine)
                    _ => Vector3.zero
                };

                // Pozisyon hesaplandıktan hemen sonra ekle
                Vector3 fineAdjust = direction switch
                {
                    EdgeDirection.Left  => new Vector3(0f,  0.1f, 0f),  // sol kapı → biraz yukarı
                    EdgeDirection.Right => new Vector3(0f, -0.1f, 0f),  // sağ kapı → biraz aşağı
                    EdgeDirection.Up    => new Vector3( 0.1f, 0f, 0f),  // üst kapı → biraz sağa
                    EdgeDirection.Down  => new Vector3(-0.1f, 0f, 0f),  // alt kapı → biraz sola
                    _ => Vector3.zero
                };

                closedDoorInstance.transform.position = transform.position + worldInsetDir * closedDoorInset + fineAdjust;

                // Yönüne göre rotasyon
                float zRot = direction switch
                {
                    EdgeDirection.Up    => 0f,
                    EdgeDirection.Down  => 180f,
                    EdgeDirection.Left  => 90f,
                    EdgeDirection.Right => -90f,
                    _ => 0f
                };
                closedDoorInstance.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);

                var leftChild  = closedDoorInstance.transform.Find("Left");
                var rightChild = closedDoorInstance.transform.Find("Right");
                var upChild    = closedDoorInstance.transform.Find("Up");
                var downChild  = closedDoorInstance.transform.Find("Down");

                if (leftChild != null)  leftChild.gameObject.SetActive(true);
                if (rightChild != null) rightChild.gameObject.SetActive(true);
                if (upChild != null)    upChild.gameObject.SetActive(false);
                if (downChild != null)  downChild.gameObject.SetActive(false);
            }
            else if (closedDoorInstance != null)
            {
                closedDoorInstance.SetActive(true);
            }
        }
        else
        {
            if (closedDoorInstance != null)
                closedDoorInstance.SetActive(false);
        }

        UpdateBlockCollider(locked);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryTransition(other);
    private void OnTriggerStay2D(Collider2D other) => TryTransition(other);

    private void TryTransition(Collider2D other)
    {
        if (isLocked) return;
        if (targetCellIndex < 0) return;
        if (!other.CompareTag("Player")) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        bool pressingAnyValidKey = direction switch
        {
            EdgeDirection.Up or EdgeDirection.Down =>
                kb[Key.W].isPressed || kb[Key.UpArrow].isPressed ||
                kb[Key.S].isPressed || kb[Key.DownArrow].isPressed,
            EdgeDirection.Left or EdgeDirection.Right =>
                kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed ||
                kb[Key.D].isPressed || kb[Key.RightArrow].isPressed,
            _ => false
        };

        if (!pressingAnyValidKey) return;

        RoomTransitionManager.instance?.TransitionToRoom(targetCellIndex, direction);
    }
}