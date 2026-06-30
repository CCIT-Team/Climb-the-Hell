using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class JakduRide : MonoBehaviour
{
    private Player player;
    private Coroutine activeRoutine;

    private PlayerStatValues activeBuff;
    private PlayerStatValues activeDebuff;

    private void Awake()
    {
        player =
            GetComponent<Player>();
    }

    public void Apply(
        JakduRideEffectData effect
    )
    {
        if (effect == null ||
            !effect.enabled)
        {
            return;
        }

        RemoveCurrentEffect();

        activeBuff =
            effect.buffStats != null
                ? effect.buffStats.Clone()
                : new PlayerStatValues();

        activeDebuff =
            effect.debuffStats != null
                ? effect.debuffStats.Clone()
                : new PlayerStatValues();

        player.stats.AddRuntimeModifier(
            activeBuff
        );

        player.stats.AddRuntimeModifier(
            activeDebuff
        );

        if (!effect.permanent &&
            effect.duration > 0f)
        {
            activeRoutine =
                StartCoroutine(
                    DurationRoutine(
                        effect.duration
                    )
                );
        }

        Debug.Log(
            "[JakduRide] 버프와 디버프 적용",
            this
        );
    }

    public void RemoveCurrentEffect()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        if (player != null &&
            player.stats != null)
        {
            player.stats.RemoveRuntimeModifier(
                activeBuff
            );

            player.stats.RemoveRuntimeModifier(
                activeDebuff
            );
        }

        activeBuff = null;
        activeDebuff = null;
    }

    private IEnumerator DurationRoutine(
        float duration
    )
    {
        yield return new WaitForSeconds(
            duration
        );

        activeRoutine = null;

        RemoveCurrentEffect();
    }

    private void OnDisable()
    {
        RemoveCurrentEffect();
    }
}
