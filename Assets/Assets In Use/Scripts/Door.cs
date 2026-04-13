using UnityEngine;
using UnityEngine.InputSystem;

public class Door : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [HideInInspector] public EdgeDirection direction;
    [HideInInspector] public int targetCellIndex = -1;
    [HideInInspector] public RoomType targetRoomType = RoomType.Regular;

    private GameObject closedDoorInstance;
    private GameObject openedDoorInstance;
    private bool isShopUnlocked = false;
    private bool isLocked = false;
    private bool isShopDoor = false;
    private bool _shopUnlockUsed = false;

    private BoxCollider2D blockCollider;

    [SerializeField] private float closedDoorInset = 0f;
[SerializeField] private float openedDoorInset = 0.5f; // bunu Inspector'dan ayarla
    public GameObject openedShopDoorPrefab;

    private void UpdateBlockCollider(bool shouldBlock)
    {
        if (shouldBlock)
        {
            if (blockCollider == null)
            {
                blockCollider = gameObject.AddComponent<BoxCollider2D>();
                blockCollider.isTrigger = false;
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

    public void MarkAsShopDoor()
    {
        isShopDoor = true;
    }

    private Vector3 GetOpenedDoorPosition()
    {
        if (closedDoorInstance != null)
            return closedDoorInstance.transform.position;

        Vector3 insetDir = direction switch
        {
            EdgeDirection.Up    => Vector3.down,
            EdgeDirection.Down  => Vector3.up,
            EdgeDirection.Left  => Vector3.right,
            EdgeDirection.Right => Vector3.left,
            _ => Vector3.zero
        };

        return transform.position + insetDir * openedDoorInset;
    }

    private Quaternion GetOpenedDoorRotation()
    {
        float zRot = direction switch
        {
            EdgeDirection.Down  => 180f,  // eskiden 0f
            EdgeDirection.Up    =>   0f,  // eskiden 180f
            EdgeDirection.Right => -90f,
            EdgeDirection.Left  =>  90f,
            _ => 0f
        };
        return Quaternion.Euler(0f, 0f, zRot);
    }

    private void SpawnOpenedDoor()
    {
        if (openedDoorInstance != null) return;
        if (openedShopDoorPrefab == null) return;

        Transform parentTransform = transform.parent != null ? transform.parent : transform;
        openedDoorInstance = Instantiate(openedShopDoorPrefab, GetOpenedDoorPosition(), GetOpenedDoorRotation(), parentTransform);
        openedDoorInstance.tag = "Door";
    }

    public void ShowOpenedShopVisual()
    {
        SpawnOpenedDoor();
    }

    public void SetLocked(bool locked, GameObject closedDoorPrefab = null)
    {
        if (isShopUnlocked) return;

        isLocked = locked;

        if (locked)
        {
            if (closedDoorInstance == null && closedDoorPrefab != null)
            {
                closedDoorInstance = Instantiate(closedDoorPrefab, transform.position, Quaternion.identity, transform);

                Vector3 worldInsetDir = direction switch
                {
                    EdgeDirection.Up    => Vector3.down,
                    EdgeDirection.Down  => Vector3.up,
                    EdgeDirection.Left  => Vector3.right,
                    EdgeDirection.Right => Vector3.left,
                    _ => Vector3.zero
                };

                Vector3 fineAdjust = direction switch
                {
                    EdgeDirection.Left  => new Vector3(0f,  0.1f, 0f),
                    EdgeDirection.Right => new Vector3(0f, -0.1f, 0f),
                    EdgeDirection.Up    => new Vector3( 0.1f, 0f, 0f),
                    EdgeDirection.Down  => new Vector3(-0.1f, 0f, 0f),
                    _ => Vector3.zero
                };

                closedDoorInstance.transform.position = transform.position + worldInsetDir * closedDoorInset + fineAdjust;

                float zRot = direction switch
                {
                    EdgeDirection.Up    =>   0f,
                    EdgeDirection.Down  => 180f,
                    EdgeDirection.Left  =>  90f,
                    EdgeDirection.Right => -90f,
                    _ => 0f
                };
                closedDoorInstance.transform.localRotation = Quaternion.Euler(0f, 0f, zRot);

                var leftChild  = closedDoorInstance.transform.Find("Left");
                var rightChild = closedDoorInstance.transform.Find("Right");
                var upChild    = closedDoorInstance.transform.Find("Up");
                var downChild  = closedDoorInstance.transform.Find("Down");

                if (leftChild  != null) leftChild.gameObject.SetActive(true);
                if (rightChild != null) rightChild.gameObject.SetActive(true);
                if (upChild    != null) upChild.gameObject.SetActive(false);
                if (downChild  != null) downChild.gameObject.SetActive(false);
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
    private void OnTriggerStay2D(Collider2D other)  => TryTransition(other);

    private void TryTransition(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (targetCellIndex < 0) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (isShopDoor && isLocked)
        {
            if (_shopUnlockUsed) return;

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

            _shopUnlockUsed = true;

            if (PlayerInventory.instance == null || !PlayerInventory.instance.UseKey())
            {
                Debug.Log("[Door] Anahtar yok!");
                _shopUnlockUsed = false;
                return;
            }

            UnlockShopDoor();
            return;
        }

        if (isLocked) return;

        bool pressingKey = direction switch
        {
            EdgeDirection.Up or EdgeDirection.Down =>
                kb[Key.W].isPressed || kb[Key.UpArrow].isPressed ||
                kb[Key.S].isPressed || kb[Key.DownArrow].isPressed,
            EdgeDirection.Left or EdgeDirection.Right =>
                kb[Key.A].isPressed || kb[Key.LeftArrow].isPressed ||
                kb[Key.D].isPressed || kb[Key.RightArrow].isPressed,
            _ => false
        };

        if (!pressingKey) return;

        RoomTransitionManager.instance?.TransitionToRoom(targetCellIndex, direction);
    }

    private void UnlockShopDoor()
    {
        isLocked = false;
        isShopDoor = false;
        isShopUnlocked = true;
        _shopUnlockUsed = false;
        UpdateBlockCollider(false);

        if (closedDoorInstance != null)
            closedDoorInstance.SetActive(false);

        SpawnOpenedDoor();

        RoomTransitionManager.instance?.TransitionToRoom(targetCellIndex, direction);
    }
}