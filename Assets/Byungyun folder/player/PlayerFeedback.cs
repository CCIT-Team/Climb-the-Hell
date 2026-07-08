using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerFeedback : MonoBehaviour
{
    private static bool pendingLobbyEmerge;

    [Header("Numbers")]
    [SerializeField]
    private Vector3 numberOffset =
        new Vector3(0f, 2.2f, 0f);

    [Header("Flash")]
    [SerializeField]
    private Color hitColor =
        new Color(1f, 0.18f, 0.12f, 1f);

    [SerializeField]
    private Color healColor =
        new Color(0.25f, 1f, 0.25f, 1f);

    [Min(0.01f)]
    [SerializeField]
    private float flashDuration = 0.18f;

    [Header("Death")]
    [Min(0f)]
    [SerializeField]
    private float fallDuration = 0.45f;

    [SerializeField]
    private float fallAngle = 82f;

    [Min(0f)]
    [SerializeField]
    private float sinkDuration = 0.9f;

    [SerializeField]
    private float sinkDepth = 1.8f;

    [Header("Lobby Emerge")]
    [Min(0f)]
    [SerializeField]
    private float emergeDuration = 0.9f;

    [SerializeField]
    private float emergeDepth = 1.8f;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashRoutine;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        renderers =
            GetComponentsInChildren<Renderer>(true);

        propertyBlock =
            new MaterialPropertyBlock();
    }

    public static void RequestLobbyEmerge()
    {
        pendingLobbyEmerge = true;
    }

    public static bool HasPendingLobbyEmerge =>
        pendingLobbyEmerge;

    public void ShowDamage(
        int amount)
    {
        ShowNumber(amount, false);
        Flash(hitColor);
    }

    public void ShowHeal(
        int amount)
    {
        ShowNumber(amount, true);
        Flash(healColor);
    }

    public bool BeginLobbyEmergeIfRequested()
    {
        if (!pendingLobbyEmerge ||
            transitionRoutine != null)
        {
            return false;
        }

        pendingLobbyEmerge = false;

        transitionRoutine =
            StartCoroutine(LobbyEmergeRoutine());

        return true;
    }

    public void PlayDeathAndReturnToLobby(
        System.Action onDeathEvent)
    {
        if (transitionRoutine != null)
        {
            return;
        }

        transitionRoutine =
            StartCoroutine(
                DeathRoutine(onDeathEvent)
            );
    }

    private void ShowNumber(
        int amount,
        bool heal)
    {
        DamageNumberManager manager =
            DamageNumberManager.Instance;

        if (manager == null ||
            amount <= 0)
        {
            return;
        }

        Vector3 position =
            transform.position +
            numberOffset;

        int slot =
            Random.Range(
                0,
                manager.GetSlotCount()
            );

        if (heal)
        {
            manager.ShowHeal(
                amount,
                position,
                slot
            );
        }
        else
        {
            manager.ShowDamage(
                amount,
                position,
                slot,
                false,
                hitColor
            );
        }
    }

    private void Flash(
        Color color)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine =
            StartCoroutine(
                FlashRoutine(color)
            );
    }

    private IEnumerator FlashRoutine(
        Color color)
    {
        SetRendererColor(color);

        yield return
            new WaitForSeconds(
                flashDuration
            );

        ClearRendererColor();
        flashRoutine = null;
    }

    private IEnumerator DeathRoutine(
        System.Action onDeathEvent)
    {
        SetPlayerControl(false);

        Quaternion startRotation =
            transform.rotation;

        Quaternion fallRotation =
            Quaternion.Euler(
                fallAngle,
                transform.eulerAngles.y,
                transform.eulerAngles.z
            );

        yield return RotateRoutine(
            startRotation,
            fallRotation,
            fallDuration
        );

        Vector3 startPosition =
            transform.position;

        Vector3 endPosition =
            startPosition +
            Vector3.down * sinkDepth;

        yield return MoveRoutine(
            startPosition,
            endPosition,
            sinkDuration
        );

        RequestLobbyEmerge();

        onDeathEvent?.Invoke();

        Player player =
            GetComponent<Player>();

        if (player != null)
        {
            player.ResetRunGoldAndHeal();
        }

        if (RunFlowManager.Instance != null)
        {
            RunFlowManager.Instance.GoToLobby();
        }
    }

    private IEnumerator LobbyEmergeRoutine()
    {
        SetPlayerControl(false);

        Player player =
            GetComponent<Player>();

        if (player != null)
        {
            player.ResetRunGoldAndHeal();
        }

        Quaternion rotation =
            Quaternion.Euler(
                0f,
                transform.eulerAngles.y,
                0f
            );

        transform.rotation = rotation;

        Vector3 endPosition =
            transform.position;

        Vector3 startPosition =
            endPosition +
            Vector3.down * emergeDepth;

        transform.position =
            startPosition;

        yield return MoveRoutine(
            startPosition,
            endPosition,
            emergeDuration
        );

        SetPlayerControl(true);

        transitionRoutine = null;
    }

    private IEnumerator RotateRoutine(
        Quaternion from,
        Quaternion to,
        float duration)
    {
        if (duration <= 0f)
        {
            transform.rotation = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / duration
                );

            transform.rotation =
                Quaternion.Slerp(
                    from,
                    to,
                    SmoothStep(ratio)
                );

            yield return null;
        }

        transform.rotation = to;
    }

    private IEnumerator MoveRoutine(
        Vector3 from,
        Vector3 to,
        float duration)
    {
        if (duration <= 0f)
        {
            transform.position = to;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / duration
                );

            transform.position =
                Vector3.Lerp(
                    from,
                    to,
                    SmoothStep(ratio)
                );

            yield return null;
        }

        transform.position = to;
    }

    private void SetPlayerControl(
        bool value)
    {
        PlayerController controller =
            GetComponent<PlayerController>();

        if (controller == null)
        {
            controller =
                GetComponentInChildren<PlayerController>(
                    true
                );
        }

        if (controller != null)
        {
            controller.enabled = value;
        }
    }

    private void SetRendererColor(
        Color color)
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            Renderer target =
                renderers[i];

            if (target == null)
            {
                continue;
            }

            target.GetPropertyBlock(
                propertyBlock
            );

            propertyBlock.SetColor(
                "_BaseColor",
                color
            );

            propertyBlock.SetColor(
                "_Color",
                color
            );

            target.SetPropertyBlock(
                propertyBlock
            );
        }
    }

    private void ClearRendererColor()
    {
        if (renderers == null)
        {
            return;
        }

        for (int i = 0;
             i < renderers.Length;
             i++)
        {
            if (renderers[i] != null)
            {
                renderers[i]
                    .SetPropertyBlock(null);
            }
        }
    }

    private static float SmoothStep(
        float value)
    {
        value =
            Mathf.Clamp01(value);

        return
            value *
            value *
            (3f - 2f * value);
    }
}
