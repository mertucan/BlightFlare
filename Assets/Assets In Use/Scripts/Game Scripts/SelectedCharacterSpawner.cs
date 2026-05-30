using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class SelectedCharacterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string childTag = "Untagged";
    [SerializeField] private int minimumSpriteSortingOrder = 20;

    [Header("Character Bonuses")]
    [SerializeField] private int maggyIndex = 1;
    [SerializeField] private int maggyMaxHalfHearts = 10;
    [SerializeField] private int bombermanIndex = 2;
    [SerializeField] private int bombermanBombAmount = 3;
    [SerializeField] private int bombermanExplosionRadius = 2;

    private void Awake()
    {
        if (ExistingPlayerRootExists()) return;
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;

        int selectedIndex = PlayerPrefs.GetInt(CharacterSelectionManager.SelectedCharacterKey, 0);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, characterPrefabs.Length - 1);

        GameObject selectedPrefab = characterPrefabs[selectedIndex];
        if (selectedPrefab == null) return;

        GameObject player = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        PrepareSpawnedPlayer(player);
        ApplySelectedCharacterBonuses(player, selectedIndex);

        if (RoomTransitionManager.instance != null)
        {
            RoomTransitionManager.instance.player = player.transform;
        }
    }

    private bool ExistingPlayerRootExists()
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(playerTag);
        for (int i = 0; i < taggedObjects.Length; i++)
        {
            if (taggedObjects[i].GetComponent<IsaacMovement>() != null) return true;
            if (taggedObjects[i].GetComponentInParent<IsaacMovement>() != null) return true;
        }

        return false;
    }

    private void PrepareSpawnedPlayer(GameObject player)
    {
        player.tag = playerTag;
        player.SetActive(true);

        Transform[] children = player.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == player.transform) continue;
            children[i].gameObject.tag = childTag;
        }

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;
            rb.bodyType = RigidbodyType2D.Dynamic;
        }

        Collider2D[] colliders = player.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = true;
        }

        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].sortingOrder < minimumSpriteSortingOrder)
            {
                renderers[i].sortingOrder += minimumSpriteSortingOrder;
            }
        }

        ResetPlayerVisualState(player);
        EnableCorePlayerScripts(player);
    }

    private void EnableCorePlayerScripts(GameObject player)
    {
        IsaacMovement movement = player.GetComponent<IsaacMovement>();
        if (movement != null) movement.enabled = true;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        if (health != null) health.enabled = true;

        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null) inventory.enabled = true;

        BombController bombController = player.GetComponent<BombController>();
        if (bombController != null) bombController.enabled = true;
    }

    private void ResetPlayerVisualState(GameObject player)
    {
        IsaacMovement movement = player.GetComponent<IsaacMovement>();
        if (movement == null) return;

        AnimatedSpriteRenderer[] animatedSprites = player.GetComponentsInChildren<AnimatedSpriteRenderer>(true);
        for (int i = 0; i < animatedSprites.Length; i++)
        {
            animatedSprites[i].enabled = false;
        }

        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].enabled = false;
        }

        EnableDirectionSprite(movement.spritesDown.body);
        EnableDirectionSprite(movement.spritesDown.head);
    }

    private void EnableDirectionSprite(AnimatedSpriteRenderer animatedSprite)
    {
        if (animatedSprite == null) return;

        animatedSprite.enabled = true;

        SpriteRenderer renderer = animatedSprite.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
    }

    private void ApplySelectedCharacterBonuses(GameObject player, int selectedIndex)
    {
        if (selectedIndex == maggyIndex)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.maxHearts = maggyMaxHalfHearts;
                health.currentHearts = maggyMaxHalfHearts;
            }
        }

        if (selectedIndex == bombermanIndex)
        {
            BombController bombController = player.GetComponent<BombController>();
            if (bombController != null)
            {
                bombController.bombAmount = bombermanBombAmount;
                bombController.explosionRadius = bombermanExplosionRadius;
            }
        }

        StartCoroutine(RefreshHealthUiNextFrame(player));
    }

    private System.Collections.IEnumerator RefreshHealthUiNextFrame(GameObject player)
    {
        yield return null;

        PlayerHealth health = player.GetComponent<PlayerHealth>();
        HeartUI heartUI = FindFirstObjectByType<HeartUI>();
        if (health == null || heartUI == null) yield break;

        heartUI.maxHalfHearts = health.maxHearts;
        heartUI.RebuildSlots();
        heartUI.UpdateHearts(health.currentHearts);
    }
}
