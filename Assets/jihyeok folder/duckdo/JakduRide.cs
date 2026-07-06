using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Player))]
public class JakduRide : MonoBehaviour
{
    private Player player;
    private Coroutine activeRoutine;

    private PlayerStatValues activeBuff;
    private PlayerStatValues activeDebuff;

    private bool activeEffectIsPermanent;

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

        if (player == null ||
            player.stats == null)
        {
            Debug.LogError(
                "[JakduRide] Player 또는 PlayerStats가 없습니다.",
                this
            );

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

        activeEffectIsPermanent =
            effect.permanent;

        player.stats
            .AddRuntimeModifier(
                activeBuff
            );

        player.stats
            .AddRuntimeModifier(
                activeDebuff
            );

        if (!activeEffectIsPermanent &&
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
            activeEffectIsPermanent
                ? "[JakduRide] 영구 버프와 디버프 적용"
                : "[JakduRide] 제한시간 버프와 디버프 적용",
            this
        );
    }

    public void RemoveCurrentEffect()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(
                activeRoutine
            );

            activeRoutine = null;
        }

        if (player != null &&
            player.stats != null)
        {
            if (activeBuff != null)
            {
                player.stats
                    .RemoveRuntimeModifier(
                        activeBuff
                    );
            }

            if (activeDebuff != null)
            {
                player.stats
                    .RemoveRuntimeModifier(
                        activeDebuff
                    );
            }
        }

        activeBuff = null;
        activeDebuff = null;

        activeEffectIsPermanent =
            false;
    }

    private IEnumerator DurationRoutine(
        float duration
    )
    {
        yield return
            new WaitForSeconds(
                duration
            );

        activeRoutine = null;

        RemoveCurrentEffect();
    }

    private void OnDisable()
    {
        /*
         * 일시 효과는 비활성화 시 정리한다.
         * 영구 효과는 오브젝트가 잠시 비활성화됐다는 이유만으로
         * 사라지지 않게 유지한다.
         */
        if (!activeEffectIsPermanent)
        {
            RemoveCurrentEffect();
        }
    }

    private void OnDestroy()
    {
        /*
         * 실제 오브젝트가 파괴될 때는
         * 남아 있는 런타임 Modifier를 정리한다.
         */
        RemoveCurrentEffect();
    }
}
