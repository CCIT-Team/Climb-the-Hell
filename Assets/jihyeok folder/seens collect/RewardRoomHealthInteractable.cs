using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 보상 오브젝트가 사라지는 연출 방식.
/// </summary>
public enum RewardDisappearMode
{
    Instant,
    FadeAndShrink,
    BreakApart
}

/// <summary>
/// 보상방의 영구 체력 보상 오브젝트.
/// 플레이어가 Trigger 범위 안에서 E키를 누르면
/// 체력 보상을 획득하고 선택한 연출로 사라진다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RewardRoomHealthInteractable : InteractableBase
{
    [Header("영구 체력 보상")]
    [Tooltip(
        "PlayerStatValues에서 최대 체력 증가값만 설정하세요. " +
        "이 값은 현재 런이 끝날 때까지 유지됩니다."
    )]
    [SerializeField]
    private PlayerStatValues permanentHealthBonus =
        new PlayerStatValues();

    [Header("방 진행")]
    [Tooltip("보상 획득 후 다음 방 문을 여는 NonCombatRoomFlow")]
    [SerializeField]
    private NonCombatRoomFlow roomFlow;

    [Header("보상 획득 후 처리")]
    [Tooltip("보상 획득 후 오브젝트를 사라지게 할지 여부")]
    [SerializeField]
    private bool disableAfterClaim = true;

    [Tooltip("오브젝트가 사라지는 방식")]
    [SerializeField]
    private RewardDisappearMode disappearMode =
        RewardDisappearMode.BreakApart;

    [Header("점점 사라지는 연출")]
    [Min(0.05f)]
    [Tooltip("투명해지고 작아지는 데 걸리는 시간")]
    [SerializeField]
    private float fadeDuration = 0.6f;

    [Range(0f, 1f)]
    [Tooltip("사라질 때 마지막에 남을 크기")]
    [SerializeField]
    private float finalScaleRatio = 0.1f;

    [Header("깨지는 연출")]
    [Range(4, 50)]
    [Tooltip("생성할 조각 개수")]
    [SerializeField]
    private int fragmentCount = 16;

    [Min(0.1f)]
    [Tooltip("조각이 유지되는 시간")]
    [SerializeField]
    private float fragmentLifetime = 1.5f;

    [Min(0f)]
    [Tooltip("조각이 사방으로 퍼지는 힘")]
    [SerializeField]
    private float fragmentForce = 4f;

    [Min(0f)]
    [Tooltip("조각이 위쪽으로 솟는 힘")]
    [SerializeField]
    private float fragmentUpwardForce = 2.5f;

    [Range(0.03f, 0.5f)]
    [Tooltip("원본 오브젝트 크기에 비례한 조각 크기")]
    [SerializeField]
    private float fragmentSizeRatio = 0.12f;

    [Min(0f)]
    [Tooltip("조각의 이동 감속 값")]
    [SerializeField]
    private float fragmentDrag = 0.15f;

    [Min(0f)]
    [Tooltip("조각의 회전 감속 값")]
    [SerializeField]
    private float fragmentAngularDrag = 0.05f;

    [Header("디버그")]
    [SerializeField]
    private bool showLogs = true;

    private bool claimed;

    private PlayerInteraction registeredPlayerInteraction;

    private Renderer[] cachedRenderers;
    private Collider[] cachedColliders;

    private Vector3 originalLocalScale;

    private readonly List<FadeMaterialInfo>
        fadeMaterialInfos =
            new List<FadeMaterialInfo>();

    /// <summary>
    /// 투명도 변경에 필요한 머티리얼 정보.
    /// </summary>
    private class FadeMaterialInfo
    {
        public Material material;
        public string colorProperty;
        public Color originalColor;
    }

    private void Awake()
    {
        Collider interactionCollider =
            GetComponent<Collider>();

        /*
         * 상호작용 범위를 감지하도록
         * 이 오브젝트의 Collider를 Trigger로 설정한다.
         */
        interactionCollider.isTrigger = true;

        cachedRenderers =
            GetComponentsInChildren<Renderer>(true);

        cachedColliders =
            GetComponentsInChildren<Collider>(true);

        originalLocalScale =
            transform.localScale;
    }

    /// <summary>
    /// 플레이어가 상호작용 범위에 진입한다.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (claimed)
        {
            return;
        }

        PlayerInteraction playerInteraction =
            other.GetComponentInParent<PlayerInteraction>();

        if (playerInteraction == null)
        {
            return;
        }

        registeredPlayerInteraction =
            playerInteraction;

        playerInteraction.RegisterInteractable(this);

        if (showLogs)
        {
            Debug.Log(
                "[RewardRoomHealthInteractable] " +
                "플레이어가 보상 범위에 들어왔습니다.",
                this
            );
        }
    }

    /// <summary>
    /// 플레이어가 상호작용 범위에서 나간다.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        PlayerInteraction playerInteraction =
            other.GetComponentInParent<PlayerInteraction>();

        if (playerInteraction == null)
        {
            return;
        }

        playerInteraction.UnregisterInteractable(this);

        if (registeredPlayerInteraction ==
            playerInteraction)
        {
            registeredPlayerInteraction = null;
        }

        if (showLogs)
        {
            Debug.Log(
                "[RewardRoomHealthInteractable] " +
                "플레이어가 보상 범위에서 나갔습니다.",
                this
            );
        }
    }

    /// <summary>
    /// 보상을 아직 획득하지 않은 경우에만
    /// 상호작용할 수 있다.
    /// </summary>
    public override bool CanInteract()
    {
        return
            !claimed &&
            isActiveAndEnabled &&
            gameObject.activeInHierarchy;
    }

    /// <summary>
    /// 플레이어가 E키를 눌렀을 때 호출된다.
    /// </summary>
    public override void Interact(Player player)
    {
        if (!CanInteract())
        {
            return;
        }

        if (player == null)
        {
            Debug.LogError(
                "[RewardRoomHealthInteractable] " +
                "Player가 없습니다.",
                this
            );

            return;
        }

        if (player.stats == null)
        {
            Debug.LogError(
                "[RewardRoomHealthInteractable] " +
                "PlayerStats가 없습니다.",
                player
            );

            return;
        }

        if (permanentHealthBonus == null)
        {
            Debug.LogError(
                "[RewardRoomHealthInteractable] " +
                "영구 체력 보상 값이 없습니다.",
                this
            );

            return;
        }

        /*
         * 같은 프레임에 상호작용이 여러 번 들어오는 것을 막기 위해
         * 보상 적용 전에 claimed를 먼저 변경한다.
         */
        claimed = true;

        PlayerStatValues appliedBonus =
            permanentHealthBonus.Clone();

        player.stats.AddBoonBonus(
            appliedBonus
        );

        DamageNumberManager damageNumberManager =
            DamageNumberManager.Instance;

        if (damageNumberManager != null)
        {
            int amount =
                Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        appliedBonus.maxHp
                    )
                );

            damageNumberManager.ShowHeal(
                amount,
                player.transform.position +
                Vector3.up * 2.2f,
                UnityEngine.Random.Range(
                    0,
                    damageNumberManager.GetSlotCount()
                )
            );
        }

        /*
         * 보상을 획득한 순간부터
         * 추가 상호작용과 충돌을 막는다.
         */
        RemoveFromPlayerInteraction();
        DisableAllColliders();

        if (showLogs)
        {
            Debug.Log(
                "[RewardRoomHealthInteractable] " +
                "영구 체력 보상을 획득했습니다.",
                this
            );
        }

        /*
         * 다음 방 진행 처리.
         */
        if (roomFlow != null)
        {
            roomFlow.CompleteRoom();
        }
        else
        {
            Debug.LogWarning(
                "[RewardRoomHealthInteractable] " +
                "NonCombatRoomFlow가 연결되지 않았습니다.",
                this
            );
        }

        if (!disableAfterClaim)
        {
            return;
        }

        switch (disappearMode)
        {
            case RewardDisappearMode.Instant:
                gameObject.SetActive(false);
                break;

            case RewardDisappearMode.FadeAndShrink:
                StartCoroutine(
                    FadeAndShrinkRoutine()
                );
                break;

            case RewardDisappearMode.BreakApart:
                StartCoroutine(
                    BreakApartRoutine()
                );
                break;
        }
    }

    /// <summary>
    /// 오브젝트를 점점 투명하게 만들면서 축소한다.
    /// </summary>
    private IEnumerator FadeAndShrinkRoutine()
    {
        PrepareFadeMaterials();

        float elapsedTime = 0f;

        Vector3 targetScale =
            originalLocalScale *
            finalScaleRatio;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime +=
                Time.unscaledDeltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsedTime / fadeDuration
                );

            /*
             * SmoothStep으로 시작과 끝의 변화를
             * 조금 더 부드럽게 만든다.
             */
            float smoothProgress =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    progress
                );

            transform.localScale =
                Vector3.Lerp(
                    originalLocalScale,
                    targetScale,
                    smoothProgress
                );

            SetFadeAlpha(
                1f - smoothProgress
            );

            yield return null;
        }

        transform.localScale =
            targetScale;

        SetFadeAlpha(0f);

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 원본 오브젝트를 숨기고
    /// 작은 조각들을 생성해 사방으로 터뜨린다.
    /// </summary>
    private IEnumerator BreakApartRoutine()
    {
        Bounds objectBounds =
            CalculateRendererBounds();

        Material fragmentMaterial =
            FindFragmentMaterial();

        /*
         * 조각을 생성하기 전에 원본 모델을 숨긴다.
         */
        SetRenderersEnabled(false);

        /*
         * 조각은 원본 오브젝트의 자식으로 만들지 않는다.
         * 원본이 비활성화되어도 조각이 계속 움직여야 하기 때문이다.
         */
        GameObject fragmentRoot =
            new GameObject(
                $"{gameObject.name}_Fragments"
            );

        float averageSize =
            (
                objectBounds.size.x +
                objectBounds.size.y +
                objectBounds.size.z
            ) / 3f;

        averageSize =
            Mathf.Max(
                averageSize,
                0.1f
            );

        float baseFragmentSize =
            averageSize *
            fragmentSizeRatio;

        for (int i = 0;
             i < fragmentCount;
             i++)
        {
            CreateFragment(
                fragmentRoot.transform,
                objectBounds,
                fragmentMaterial,
                baseFragmentSize
            );
        }

        /*
         * 조각이 일정 시간 동안 움직이게 둔다.
         */
        yield return new WaitForSecondsRealtime(
            fragmentLifetime
        );

        if (fragmentRoot != null)
        {
            Destroy(fragmentRoot);
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 별도 프리팹 없이 Cube 조각을 하나 생성한다.
    /// </summary>
    private void CreateFragment(
        Transform fragmentParent,
        Bounds objectBounds,
        Material fragmentMaterial,
        float baseFragmentSize)
    {
        GameObject fragment =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        fragment.name =
            "RewardFragment";

        fragment.layer =
            gameObject.layer;

        fragment.transform.SetParent(
            fragmentParent,
            true
        );

        /*
         * 원본 Renderer 영역 안에서
         * 무작위 위치를 선택한다.
         */
        Vector3 randomOffset =
            new Vector3(
                Random.Range(
                    -objectBounds.extents.x,
                    objectBounds.extents.x
                ),
                Random.Range(
                    -objectBounds.extents.y,
                    objectBounds.extents.y
                ),
                Random.Range(
                    -objectBounds.extents.z,
                    objectBounds.extents.z
                )
            );

        fragment.transform.position =
            objectBounds.center +
            randomOffset;

        fragment.transform.rotation =
            Random.rotation;

        float randomSize =
            baseFragmentSize *
            Random.Range(
                0.65f,
                1.35f
            );

        fragment.transform.localScale =
            new Vector3(
                randomSize *
                Random.Range(0.5f, 1.2f),

                randomSize *
                Random.Range(0.5f, 1.4f),

                randomSize *
                Random.Range(0.5f, 1.2f)
            );

        Renderer fragmentRenderer =
            fragment.GetComponent<Renderer>();

        /*
         * 원본 오브젝트의 머티리얼을 조각에도 적용한다.
         */
        if (fragmentRenderer != null &&
            fragmentMaterial != null)
        {
            fragmentRenderer.sharedMaterial =
                fragmentMaterial;
        }

        /*
         * 조각은 시각 효과 전용이므로
         * 플레이어와 물리 충돌하지 않게 한다.
         */
        Collider fragmentCollider =
            fragment.GetComponent<Collider>();

        if (fragmentCollider != null)
        {
            fragmentCollider.enabled =
                false;
        }

        Rigidbody fragmentRigidbody =
            fragment.AddComponent<Rigidbody>();

        fragmentRigidbody.mass = 0.05f;
        fragmentRigidbody.useGravity = true;

        /*
         * 이전 Unity Rigidbody API를 사용한다.
         * 이동과 회전이 서서히 느려지게 한다.
         */
        fragmentRigidbody.drag =
            fragmentDrag;

        fragmentRigidbody.angularDrag =
            fragmentAngularDrag;

        Vector3 outwardDirection =
            fragment.transform.position -
            objectBounds.center;

        if (outwardDirection.sqrMagnitude <
            0.001f)
        {
            outwardDirection =
                Random.onUnitSphere;
        }
        else
        {
            outwardDirection.Normalize();
        }

        Vector3 burstForce =
            outwardDirection *
            fragmentForce;

        burstForce +=
            Vector3.up *
            fragmentUpwardForce;

        burstForce +=
            Random.insideUnitSphere *
            fragmentForce *
            0.35f;

        fragmentRigidbody.AddForce(
            burstForce,
            ForceMode.Impulse
        );

        fragmentRigidbody.AddTorque(
            Random.insideUnitSphere *
            fragmentForce,
            ForceMode.Impulse
        );
    }

    /// <summary>
    /// 모든 자식 Renderer를 포함하는
    /// 전체 Bounds를 계산한다.
    /// </summary>
    private Bounds CalculateRendererBounds()
    {
        bool hasBounds = false;

        Bounds combinedBounds =
            new Bounds(
                transform.position,
                Vector3.one
            );

        if (cachedRenderers == null)
        {
            return combinedBounds;
        }

        foreach (Renderer targetRenderer
                 in cachedRenderers)
        {
            if (targetRenderer == null ||
                !targetRenderer.enabled ||
                !targetRenderer.gameObject
                    .activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds =
                    targetRenderer.bounds;

                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(
                    targetRenderer.bounds
                );
            }
        }

        return combinedBounds;
    }

    /// <summary>
    /// 조각에 사용할 원본 머티리얼을 찾는다.
    /// </summary>
    private Material FindFragmentMaterial()
    {
        if (cachedRenderers == null)
        {
            return null;
        }

        foreach (Renderer targetRenderer
                 in cachedRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            Material material =
                targetRenderer.sharedMaterial;

            if (material != null)
            {
                return material;
            }
        }

        return null;
    }

    /// <summary>
    /// Fade 연출을 위해 Renderer별 머티리얼을
    /// 개별 인스턴스로 생성한다.
    /// </summary>
    private void PrepareFadeMaterials()
    {
        fadeMaterialInfos.Clear();

        if (cachedRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer
                 in cachedRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            Material[] materials =
                targetRenderer.materials;

            foreach (Material material
                     in materials)
            {
                if (material == null)
                {
                    continue;
                }

                string colorProperty = null;

                /*
                 * URP Lit는 _BaseColor,
                 * Standard Shader는 _Color를 사용한다.
                 */
                if (material.HasProperty(
                    "_BaseColor"))
                {
                    colorProperty =
                        "_BaseColor";
                }
                else if (material.HasProperty(
                    "_Color"))
                {
                    colorProperty =
                        "_Color";
                }

                if (string.IsNullOrEmpty(
                    colorProperty))
                {
                    continue;
                }

                ConfigureTransparentMaterial(
                    material
                );

                FadeMaterialInfo info =
                    new FadeMaterialInfo
                    {
                        material = material,

                        colorProperty =
                            colorProperty,

                        originalColor =
                            material.GetColor(
                                colorProperty
                            )
                    };

                fadeMaterialInfos.Add(info);
            }
        }
    }

    /// <summary>
    /// Standard 또는 URP Lit 머티리얼을
    /// 투명 렌더링 상태로 변경한다.
    /// </summary>
    private void ConfigureTransparentMaterial(
        Material material)
    {
        /*
         * URP Lit Shader 설정.
         */
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat(
                "_Surface",
                1f
            );

            material.SetFloat(
                "_Blend",
                0f
            );

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );

            material.DisableKeyword(
                "_SURFACE_TYPE_OPAQUE"
            );
        }

        /*
         * Built-in Standard Shader 설정.
         */
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat(
                "_Mode",
                2f
            );

            material.DisableKeyword(
                "_ALPHATEST_ON"
            );

            material.EnableKeyword(
                "_ALPHABLEND_ON"
            );

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON"
            );
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha
            );
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat(
                "_ZWrite",
                0f
            );
        }

        material.renderQueue =
            (int)RenderQueue.Transparent;

        material.SetShaderPassEnabled(
            "ShadowCaster",
            false
        );
    }

    /// <summary>
    /// Fade 연출에 사용하는 모든 머티리얼의
    /// 알파값을 변경한다.
    /// </summary>
    private void SetFadeAlpha(float alpha)
    {
        alpha =
            Mathf.Clamp01(alpha);

        foreach (FadeMaterialInfo info
                 in fadeMaterialInfos)
        {
            if (info.material == null)
            {
                continue;
            }

            Color color =
                info.originalColor;

            color.a *= alpha;

            info.material.SetColor(
                info.colorProperty,
                color
            );
        }
    }

    /// <summary>
    /// 원본 오브젝트의 모든 Renderer를 켜거나 끈다.
    /// </summary>
    private void SetRenderersEnabled(
        bool value)
    {
        if (cachedRenderers == null)
        {
            return;
        }

        foreach (Renderer targetRenderer
                 in cachedRenderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.enabled =
                    value;
            }
        }
    }

    /// <summary>
    /// 보상을 획득한 뒤 모든 Collider를 끈다.
    /// </summary>
    private void DisableAllColliders()
    {
        if (cachedColliders == null)
        {
            return;
        }

        foreach (Collider targetCollider
                 in cachedColliders)
        {
            if (targetCollider != null)
            {
                targetCollider.enabled =
                    false;
            }
        }
    }

    /// <summary>
    /// PlayerInteraction의 상호작용 목록에서 제거한다.
    /// </summary>
    private void RemoveFromPlayerInteraction()
    {
        if (registeredPlayerInteraction == null)
        {
            return;
        }

        registeredPlayerInteraction
            .UnregisterInteractable(this);

        registeredPlayerInteraction = null;
    }

    private void OnDisable()
    {
        /*
         * 플레이어가 범위 안에 있는 상태에서
         * 오브젝트가 꺼져도 상호작용 목록에 남지 않게 한다.
         */
        RemoveFromPlayerInteraction();
    }
}
