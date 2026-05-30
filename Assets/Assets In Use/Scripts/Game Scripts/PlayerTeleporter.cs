using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTeleporter : MonoBehaviour
{
    private static readonly Dictionary<GameObject, PlayerTeleporter> PlayersWaitingForExit =
        new Dictionary<GameObject, PlayerTeleporter>();

    [Header("Teleport")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform teleportTarget;
    [SerializeField] private float targetExitCheckRadius = 0.35f;
    [SerializeField] private float whiteFadeInDuration = 0.12f;
    [SerializeField] private float whiteFadeOutDuration = 0.35f;

    [Header("Audio")]
    [SerializeField] private AudioClip teleportClip;
    [SerializeField, Range(0f, 1f)] private float teleportVolume = 1f;

    private bool isTeleporting;

    private void Awake()
    {
        MakeCollidersTrigger();
    }

    private void OnValidate()
    {
        MakeCollidersTrigger();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        GameObject player = GetPlayerObject(other);
        if (player == null) return;

        TryTeleport(player);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        GameObject player = GetPlayerObject(other);
        if (player == null) return;

        if (PlayersWaitingForExit.TryGetValue(player, out PlayerTeleporter exitTeleporter)
            && exitTeleporter == this)
        {
            PlayersWaitingForExit.Remove(player);
        }
    }

    private void TryTeleport(GameObject player)
    {
        if (isTeleporting || teleportTarget == null || !player.CompareTag(playerTag)) return;
        if (PlayersWaitingForExit.ContainsKey(player)) return;

        StartCoroutine(TeleportRoutine(player));
    }

    private IEnumerator TeleportRoutine(GameObject player)
    {
        isTeleporting = true;

        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>();
        Color[] originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
        }

        yield return TintPlayer(renderers, originalColors, Color.white, whiteFadeInDuration);

        MovePlayer(player);
        WaitForTargetExitBeforeRearming(player);
        PlayTeleportSound();

        yield return TintPlayer(renderers, originalColors, Color.white, whiteFadeOutDuration, true);
        RestoreOriginalColors(renderers, originalColors);

        isTeleporting = false;
    }

    private void MovePlayer(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        Vector2 targetPosition = teleportTarget.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.position = targetPosition;
            return;
        }

        player.transform.position = teleportTarget.position;
    }

    private void WaitForTargetExitBeforeRearming(GameObject player)
    {
        PlayerTeleporter targetTeleporter = FindTargetTeleporter();

        if (targetTeleporter != null)
        {
            PlayersWaitingForExit[player] = targetTeleporter;
        }
    }

    private PlayerTeleporter FindTargetTeleporter()
    {
        PlayerTeleporter teleporterOnTarget = teleportTarget.GetComponentInParent<PlayerTeleporter>();
        if (teleporterOnTarget != null && teleporterOnTarget != this)
        {
            return teleporterOnTarget;
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(teleportTarget.position, targetExitCheckRadius);
        PlayerTeleporter fallbackTeleporter = null;

        for (int i = 0; i < colliders.Length; i++)
        {
            PlayerTeleporter teleporter = colliders[i].GetComponentInParent<PlayerTeleporter>();
            if (teleporter == null) continue;

            if (teleporter != this)
            {
                return teleporter;
            }

            fallbackTeleporter = teleporter;
        }

        return fallbackTeleporter;
    }

    private GameObject GetPlayerObject(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            return other.gameObject;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag))
        {
            return other.attachedRigidbody.gameObject;
        }

        Transform current = other.transform.parent;
        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private void MakeCollidersTrigger()
    {
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] == null) continue;
            colliders[i].isTrigger = true;
        }
    }

    private void PlayTeleportSound()
    {
        if (teleportClip == null) return;

        GameObject audioObject = new GameObject("Teleport Sound");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = teleportClip;
        audioSource.volume = teleportVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        Destroy(audioObject, teleportClip.length);
    }

    private IEnumerator TintPlayer(
        SpriteRenderer[] renderers,
        Color[] originalColors,
        Color tintColor,
        float duration,
        bool reverse = false)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (reverse) t = 1f - t;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].color = Color.Lerp(originalColors[i], tintColor, t);
            }

            yield return null;
        }
    }

    private void RestoreOriginalColors(SpriteRenderer[] renderers, Color[] originalColors)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].color = originalColors[i];
        }
    }
}
