using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 클리어 후 플레이어 위치 위에서 득도 보상이 낙하한다.
///
/// 기능:
/// 1. BoonCategory에 맞는 월드 프리팹 생성
/// 2. 플레이어 위에서 빠르게 낙하
/// 3. 착지 순간 Point Light와 스케일로 발광 연출
/// 4. 착지 완료 후에만 E키 상호작용 허용
/// 5. 득도 선택 완료 시 간단한 파편 연출
/// 6. 보상 UI가 열릴 때 지정한 폰트를 적용
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BoonRewardInteractable : InteractableBase
{
    [Header("보상 선택")]
    [Range(1, 3)]
    [SerializeField] private int choiceCount = 3;

    [Header("보상 UI")]
    [SerializeField] private BoonRewardUI boonRewardUI;

    [Header("보상 UI 폰트")]
    [Tooltip("보상 UI가 열렸을 때 폰트를 적용할지 여부")]
    [SerializeField] private bool applyRewardUIFont = true;

    [Tooltip("decide1, decide2, decide3 또는 그 부모 오브젝트를 넣는다. 비워두면 BoonRewardUI 오브젝트 아래 전체에 적용한다.")]
    [SerializeField] private GameObject[] rewardFontTargets =
        new GameObject[0];

    [Tooltip("TextMeshPro용 폰트. TMP Font Asset을 넣어야 한다.")]
    [SerializeField] private TMP_FontAsset rewardTmpFont;

    [Tooltip("기본 UI Text용 폰트. TMP가 아니라 UnityEngine.UI.Text일 때만 사용한다.")]
    [SerializeField] private Font rewardLegacyFont;

    [Header("득도 데이터")]
    [SerializeField] private BoonDatabase boonDatabase;

    [Tooltip("BoonDatabase가 비어 있을 때 검색할 Resources 폴더")]
    [SerializeField] private string resourcesPath = "Boons";

    [Header("계열별 월드 프리팹")]
    [Tooltip("보상 프리팹은 이 오브젝트의 자식으로 바로 생성됩니다.")]

    [SerializeField] private GameObject defaultRewardPrefab;
    [SerializeField] private GameObject attackRewardPrefab;
    [SerializeField] private GameObject defenseRewardPrefab;
    [SerializeField] private GameObject mobilityRewardPrefab;
    [SerializeField] private GameObject debuffRewardPrefab;

    [Tooltip("작두방에서만 표시할 작두 월드 프리팹")]
    [SerializeField] private GameObject jakduRewardPrefab;

    [Header("작두 프리팹 위치 보정")]

    [Tooltip("작두 프리팹 자식의 로컬 위치 보정")]
    [SerializeField] private Vector3 jakduVisualLocalPosition;

    [Tooltip("작두 프리팹 자식의 로컬 회전 보정")]
    [SerializeField] private Vector3 jakduVisualLocalRotation;

    [Tooltip("작두 프리팹 원본 크기에 곱할 배율")]
    [SerializeField] private Vector3 jakduVisualScaleMultiplier =
        Vector3.one;

    [Header("플레이어 위치")]
    [Tooltip("플레이어 Transform 위치에 더할 최종 착지 오프셋")]
    [SerializeField] private Vector3 landingOffset =
        new Vector3(0f, 0.5f, 0f);

    [Header("낙하 연출")]
    [Min(0f)]
    [SerializeField] private float dropHeight = 5f;

    [Min(0.01f)]
    [SerializeField] private float dropDuration = 0.28f;

    [Tooltip("낙하 후 아주 짧게 위로 튀는 높이")]
    [Min(0f)]
    [SerializeField] private float landingBounceHeight = 0.18f;

    [Min(0.01f)]
    [SerializeField] private float landingBounceDuration = 0.1f;

    [Header("착지 발광")]
    [Tooltip("비어 있으면 런타임에 Point Light를 자동 생성")]
    [SerializeField] private Light landingLight;

    [Min(0f)]
    [SerializeField] private float flashIntensity = 12f;

    [Min(0f)]
    [SerializeField] private float flashRange = 5f;

    [Min(0.01f)]
    [SerializeField] private float flashDuration = 0.18f;

    [Min(1f)]
    [SerializeField] private float flashScaleMultiplier = 1.18f;

    [Header("획득 파편 연출")]
    [SerializeField] private bool playBreakEffect = true;

    [Range(4, 30)]
    [SerializeField] private int fragmentCount = 14;

    [Min(0.1f)]
    [SerializeField] private float fragmentLifetime = 0.9f;

    [Min(0f)]
    [SerializeField] private float fragmentForce = 3.5f;

    [Min(0f)]
    [SerializeField] private float fragmentUpwardForce = 2f;

    [Min(0f)]
    [SerializeField] private float fragmentGravity = 5f;

    [Range(0.02f, 0.4f)]
    [SerializeField] private float fragmentSizeRatio = 0.1f;

    [Header("디버그")]
    [SerializeField] private bool showLogs = true;

    private readonly List<BoonData> allBoons =
        new List<BoonData>();

    private readonly List<BoonData> availableBoons =
        new List<BoonData>(32);

    private readonly List<BoonData> selectedChoices =
        new List<BoonData>(3);

    private readonly HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    private Player currentPlayer;
    private BoonInfo currentBoonInfo;
    private PlayerInteraction currentPlayerInteraction;

    private BoonCategory targetCategory =
        BoonCategory.None;

    private Action completedCallback;

    private BoxCollider triggerCollider;
    private GameObject activeVisual;
    private Renderer[] activeRenderers =
        Array.Empty<Renderer>();

    private Vector3 activeVisualBaseScale =
        Vector3.one;

    private Coroutine appearanceRoutine;

    private bool rewardReady;
    private bool rewardUsed;
    private bool selectionInProgress;

    private void Awake()
    {
        triggerCollider =
            GetComponent<BoxCollider>();

        triggerCollider.isTrigger = true;
        triggerCollider.enabled = false;

        Rigidbody body =
            GetComponent<Rigidbody>();

        body.isKinematic = true;
        body.useGravity = false;
        body.detectCollisions = true;

        EnsureLandingLight();
        ResolveUI();
        LoadBoonData();
    }

    private void OnValidate()
    {
        BoxCollider box =
            GetComponent<BoxCollider>();

        if (box != null)
        {
            box.isTrigger = true;
        }

        Rigidbody body =
            GetComponent<Rigidbody>();

        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }

        if (jakduVisualScaleMultiplier.x == 0f)
        {
            jakduVisualScaleMultiplier.x = 1f;
        }

        if (jakduVisualScaleMultiplier.y == 0f)
        {
            jakduVisualScaleMultiplier.y = 1f;
        }

        if (jakduVisualScaleMultiplier.z == 0f)
        {
            jakduVisualScaleMultiplier.z = 1f;
        }
    }

    /// <summary>
    /// 기존 CombatRoomFlow와 호환되는 Prepare.
    /// 씬의 Player를 한 번 찾아 그 위치를 착지점으로 사용한다.
    /// </summary>
    public void Prepare(
        BoonCategory category,
        Action onCompleted)
    {
        Player player =
            FindFirstObjectByType<Player>();

        Vector3 landingPosition =
            player != null
                ? player.transform.position +
                  landingOffset
                : transform.position;

        PrepareAt(
            category,
            landingPosition,
            onCompleted
        );
    }

    /// <summary>
    /// 외부에서 착지 위치를 정확히 지정하고 싶을 때 사용한다.
    /// </summary>
    public void PrepareAt(
        BoonCategory category,
        Vector3 landingPosition,
        Action onCompleted)
    {
        /*
         * Jakdu 카테고리는 실제 작두방에서만 허용한다.
         * 다른 방에서 잘못 호출돼도 작두가 생성되지 않는다.
         */
        if (category == BoonCategory.Jakdu &&
            !IsCurrentRoomJakdu())
        {
            HideReward();

            if (showLogs)
            {
                Debug.LogWarning(
                    "[BoonRewardInteractable] " +
                    "현재 방이 Jakdu가 아니므로 작두 보상을 생성하지 않습니다.",
                    this
                );
            }

            return;
        }

        if (appearanceRoutine != null)
        {
            StopCoroutine(appearanceRoutine);
            appearanceRoutine = null;
        }

        targetCategory = category;
        completedCallback = onCompleted;

        rewardReady = false;
        rewardUsed = false;
        selectionInProgress = false;

        ClearPlayerReference(true);
        BuildCategoryVisual(category);

        if (triggerCollider == null)
        {
            triggerCollider =
                GetComponent<BoxCollider>();
        }

        triggerCollider.enabled = false;
        triggerCollider.isTrigger = true;

        gameObject.SetActive(true);

        appearanceRoutine =
            StartCoroutine(
                DropAndFlashRoutine(
                    landingPosition
                )
            );

        if (showLogs)
        {
            Debug.Log(
                $"[BoonRewardInteractable] 보상 준비 / " +
                $"계열={category}, 착지={landingPosition}",
                this
            );
        }
    }

    /// <summary>
    /// 전투 시작 전에 보상을 감춘다.
    /// </summary>
    public void HideReward()
    {
        if (appearanceRoutine != null)
        {
            StopCoroutine(appearanceRoutine);
            appearanceRoutine = null;
        }

        rewardReady = false;
        rewardUsed = false;
        selectionInProgress = false;
        completedCallback = null;

        ClearPlayerReference(true);

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        if (activeVisual != null)
        {
            activeVisual.SetActive(false);
        }

        if (landingLight != null)
        {
            landingLight.intensity = 0f;
            landingLight.enabled = false;
        }
    }

    public override bool CanInteract()
    {
        if (boonRewardUI == null)
        {
            ResolveUI();
        }

        return
            rewardReady &&
            !rewardUsed &&
            !selectionInProgress &&
            targetCategory != BoonCategory.None &&
            currentPlayer != null &&
            currentBoonInfo != null &&
            currentPlayerInteraction != null &&
            boonRewardUI != null &&
            !boonRewardUI.IsOpen;
    }

    public override void Interact(
        Player player)
    {
        if (!CanInteract() ||
            player == null ||
            player != currentPlayer)
        {
            return;
        }

        CreateChoices();

        if (selectedChoices.Count == 0)
        {
            Debug.LogWarning(
                $"[BoonRewardInteractable] " +
                $"{targetCategory} 계열에서 획득 가능한 득도가 없습니다.",
                this
            );

            CompleteReward();
            return;
        }

        bool opened =
            boonRewardUI.Open(
                selectedChoices,
                HandleSelected,
                HandleCancelled
            );

        if (!opened)
        {
            return;
        }

        /*
         * 보상 UI가 실제로 열린 직후,
         * decide1, decide2, decide3 또는 지정한 대상에 폰트를 적용한다.
         */
        ApplyRewardUIFont();

        selectionInProgress = true;

        currentPlayerInteraction
            .SetInteractionBlocked(true);
    }

    private IEnumerator DropAndFlashRoutine(
        Vector3 landingPosition)
    {
        Vector3 startPosition =
            landingPosition +
            Vector3.up * dropHeight;

        transform.position =
            startPosition;

        if (activeVisual != null)
        {
            activeVisual.SetActive(true);
            activeVisual.transform.localScale =
                activeVisualBaseScale;
        }

        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / dropDuration
                );

            // 처음에는 빠르게 가속하고 착지 직전까지 강하게 떨어진다.
            float fallRatio =
                ratio * ratio;

            transform.position =
                Vector3.LerpUnclamped(
                    startPosition,
                    landingPosition,
                    fallRatio
                );

            yield return null;
        }

        transform.position =
            landingPosition;

        if (landingBounceHeight > 0f)
        {
            yield return BounceRoutine(
                landingPosition
            );
        }

        yield return FlashRoutine();

        rewardReady = true;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        appearanceRoutine = null;

        if (showLogs)
        {
            Debug.Log(
                "[BoonRewardInteractable] 낙하 완료 / 상호작용 가능",
                this
            );
        }
    }

    private IEnumerator BounceRoutine(
        Vector3 landingPosition)
    {
        float halfDuration =
            landingBounceDuration * 0.5f;

        if (halfDuration <= 0f)
        {
            yield break;
        }

        Vector3 bouncePosition =
            landingPosition +
            Vector3.up * landingBounceHeight;

        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / halfDuration
                );

            transform.position =
                Vector3.Lerp(
                    landingPosition,
                    bouncePosition,
                    SmoothStep(ratio)
                );

            yield return null;
        }

        elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / halfDuration
                );

            transform.position =
                Vector3.Lerp(
                    bouncePosition,
                    landingPosition,
                    SmoothStep(ratio)
                );

            yield return null;
        }

        transform.position =
            landingPosition;
    }

    private IEnumerator FlashRoutine()
    {
        EnsureLandingLight();

        if (landingLight != null)
        {
            landingLight.range =
                flashRange;

            landingLight.intensity =
                flashIntensity;

            landingLight.enabled = true;
        }

        Transform scaleTarget =
            activeVisual != null
                ? activeVisual.transform
                : transform;

        Vector3 baseScale =
            activeVisual != null
                ? activeVisualBaseScale
                : transform.localScale;

        Vector3 flashScale =
            baseScale *
            flashScaleMultiplier;

        if (scaleTarget != null)
        {
            scaleTarget.localScale =
                flashScale;
        }

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;

            float ratio =
                Mathf.Clamp01(
                    elapsed / flashDuration
                );

            float smoothRatio =
                SmoothStep(ratio);

            if (landingLight != null)
            {
                landingLight.intensity =
                    Mathf.Lerp(
                        flashIntensity,
                        0f,
                        smoothRatio
                    );
            }

            if (scaleTarget != null)
            {
                scaleTarget.localScale =
                    Vector3.Lerp(
                        flashScale,
                        baseScale,
                        smoothRatio
                    );
            }

            yield return null;
        }

        if (landingLight != null)
        {
            landingLight.intensity = 0f;
            landingLight.enabled = false;
        }

        if (scaleTarget != null)
        {
            scaleTarget.localScale =
                baseScale;
        }
    }

    private void BuildCategoryVisual(
        BoonCategory category)
    {
        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }

        GameObject selectedPrefab =
            GetRewardPrefab(category);

        if (selectedPrefab == null)
        {
            Debug.LogError(
                $"[BoonRewardInteractable] " +
                $"{category} 보상 프리팹이 연결되지 않았습니다.",
                this
            );

            activeRenderers =
                Array.Empty<Renderer>();

            return;
        }

        activeVisual =
            Instantiate(
                selectedPrefab,
                transform
            );

        activeVisual.name =
            $"{selectedPrefab.name}_{category}";

        if (category == BoonCategory.Jakdu)
        {
            activeVisual.transform.localPosition =
                jakduVisualLocalPosition;

            activeVisual.transform.localRotation =
                Quaternion.Euler(
                    jakduVisualLocalRotation
                );

            activeVisual.transform.localScale =
                Vector3.Scale(
                    activeVisual.transform.localScale,
                    jakduVisualScaleMultiplier
                );
        }
        else
        {
            activeVisual.transform.localPosition =
                Vector3.zero;

            activeVisual.transform.localRotation =
                Quaternion.identity;
        }

        activeVisualBaseScale =
            activeVisual.transform.localScale;

        /*
         * 프리팹 원본이 비활성화 상태여도
         * 월드 보상에서는 반드시 보이게 한다.
         */
        activeVisual.SetActive(true);

        // 월드 보상 프리팹은 시각 전용으로 사용한다.
        // 상호작용 판정은 루트의 BoxCollider 하나만 담당한다.
        Collider[] childColliders =
            activeVisual.GetComponentsInChildren<
                Collider>(true);

        for (int i = 0;
             i < childColliders.Length;
             i++)
        {
            childColliders[i].enabled =
                false;
        }

        activeRenderers =
            activeVisual.GetComponentsInChildren<
                Renderer>(true);
    }

    private GameObject GetRewardPrefab(
        BoonCategory category)
    {
        switch (category)
        {
            case BoonCategory.Attack:
                return attackRewardPrefab != null
                    ? attackRewardPrefab
                    : defaultRewardPrefab;

            case BoonCategory.Defense:
                return defenseRewardPrefab != null
                    ? defenseRewardPrefab
                    : defaultRewardPrefab;

            case BoonCategory.Mobility:
                return mobilityRewardPrefab != null
                    ? mobilityRewardPrefab
                    : defaultRewardPrefab;

            case BoonCategory.Debuff:
                return debuffRewardPrefab != null
                    ? debuffRewardPrefab
                    : defaultRewardPrefab;

            case BoonCategory.Jakdu:
                /*
                 * 작두 프리팹이 비어 있을 때 일반 보상으로
                 * 몰래 대체하지 않는다. 연결 오류를 바로 확인한다.
                 */
                return jakduRewardPrefab;

            default:
                return defaultRewardPrefab;
        }
    }

    private void CreateChoices()
    {
        availableBoons.Clear();
        selectedChoices.Clear();

        if (currentBoonInfo == null)
        {
            return;
        }

        for (int i = 0;
             i < allBoons.Count;
             i++)
        {
            BoonData boon =
                allBoons[i];

            if (boon == null ||
                boon.category != targetCategory ||
                !currentBoonInfo.CanOffer(boon))
            {
                continue;
            }

            availableBoons.Add(boon);
        }

        int resultCount =
            Mathf.Min(
                choiceCount,
                availableBoons.Count
            );

        // 필요한 개수만 부분 Fisher-Yates 셔플한다.
        for (int i = 0;
             i < resultCount;
             i++)
        {
            int randomIndex =
                UnityEngine.Random.Range(
                    i,
                    availableBoons.Count
                );

            BoonData temporary =
                availableBoons[i];

            availableBoons[i] =
                availableBoons[randomIndex];

            availableBoons[randomIndex] =
                temporary;

            selectedChoices.Add(
                availableBoons[i]
            );
        }
    }

    /// <summary>
    /// 보상 UI 텍스트에 인스펙터에서 지정한 폰트를 적용한다.
    /// </summary>
    private void ApplyRewardUIFont()
    {
        if (!applyRewardUIFont)
        {
            return;
        }

        if (rewardFontTargets == null ||
            rewardFontTargets.Length == 0)
        {
            if (boonRewardUI != null)
            {
                ApplyFontsToObject(
                    boonRewardUI.gameObject
                );
            }

            return;
        }

        for (int i = 0;
             i < rewardFontTargets.Length;
             i++)
        {
            GameObject target =
                rewardFontTargets[i];

            if (target == null)
            {
                continue;
            }

            ApplyFontsToObject(target);
        }
    }

    /// <summary>
    /// 지정한 오브젝트 아래의 TMP 텍스트와 기본 UI Text에 폰트를 적용한다.
    /// </summary>
    private void ApplyFontsToObject(
        GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (rewardTmpFont != null)
        {
            TMP_Text[] tmpTexts =
                target.GetComponentsInChildren<TMP_Text>(
                    true
                );

            for (int i = 0;
                 i < tmpTexts.Length;
                 i++)
            {
                TMP_Text text =
                    tmpTexts[i];

                if (text == null)
                {
                    continue;
                }

                text.font =
                    rewardTmpFont;

                text.ForceMeshUpdate();
            }
        }

        if (rewardLegacyFont != null)
        {
            Text[] legacyTexts =
                target.GetComponentsInChildren<Text>(
                    true
                );

            for (int i = 0;
                 i < legacyTexts.Length;
                 i++)
            {
                Text text =
                    legacyTexts[i];

                if (text == null)
                {
                    continue;
                }

                text.font =
                    rewardLegacyFont;
            }
        }
    }

    private void HandleSelected(
        BoonData selectedBoon)
    {
        selectionInProgress = false;

        if (selectedBoon == null ||
            currentBoonInfo == null)
        {
            UnblockInteraction();
            return;
        }

        if (!currentBoonInfo.TryAddBoon(
                selectedBoon
            ))
        {
            Debug.LogWarning(
                $"[BoonRewardInteractable] " +
                $"{selectedBoon.displayName} 획득 실패",
                this
            );

            UnblockInteraction();
            return;
        }

        CompleteReward();
    }

    private void HandleCancelled()
    {
        selectionInProgress = false;
        UnblockInteraction();

        if (currentPlayerInteraction != null &&
            playerColliders.Count > 0 &&
            CanInteract())
        {
            currentPlayerInteraction
                .RegisterInteractable(this);
        }
    }

    private void CompleteReward()
    {
        if (rewardUsed)
        {
            return;
        }

        rewardUsed = true;
        rewardReady = false;
        selectionInProgress = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        Action callback =
            completedCallback;

        completedCallback = null;

        ClearPlayerReference(true);

        if (playBreakEffect)
        {
            PlayBreakEffect();
        }

        if (activeVisual != null)
        {
            activeVisual.SetActive(false);
        }

        if (callback != null)
        {
            callback.Invoke();
        }
    }

    private void PlayBreakEffect()
    {
        Bounds bounds =
            CalculateVisualBounds();

        Material sourceMaterial =
            FindSourceMaterial();

        float averageSize =
            (
                bounds.size.x +
                bounds.size.y +
                bounds.size.z
            ) / 3f;

        float baseSize =
            Mathf.Max(
                0.03f,
                averageSize *
                fragmentSizeRatio
            );

        for (int i = 0;
             i < fragmentCount;
             i++)
        {
            GameObject fragment =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );

            fragment.name =
                "BoonRewardFragment";

            Collider fragmentCollider =
                fragment.GetComponent<Collider>();

            if (fragmentCollider != null)
            {
                Destroy(fragmentCollider);
            }

            Vector3 randomOffset =
                new Vector3(
                    UnityEngine.Random.Range(
                        -bounds.extents.x,
                        bounds.extents.x
                    ),
                    UnityEngine.Random.Range(
                        -bounds.extents.y,
                        bounds.extents.y
                    ),
                    UnityEngine.Random.Range(
                        -bounds.extents.z,
                        bounds.extents.z
                    )
                );

            fragment.transform.position =
                bounds.center +
                randomOffset;

            fragment.transform.rotation =
                UnityEngine.Random.rotation;

            float randomSize =
                baseSize *
                UnityEngine.Random.Range(
                    0.65f,
                    1.35f
                );

            fragment.transform.localScale =
                Vector3.one *
                randomSize;

            Renderer renderer =
                fragment.GetComponent<Renderer>();

            if (renderer != null &&
                sourceMaterial != null)
            {
                renderer.sharedMaterial =
                    sourceMaterial;
            }

            Vector3 direction =
                fragment.transform.position -
                bounds.center;

            if (direction.sqrMagnitude <
                0.001f)
            {
                direction =
                    UnityEngine.Random.onUnitSphere;
            }

            direction.Normalize();

            Vector3 velocity =
                direction *
                fragmentForce +
                Vector3.up *
                fragmentUpwardForce +
                UnityEngine.Random.insideUnitSphere *
                fragmentForce *
                0.25f;

            RewardFragmentMotion motion =
                fragment.AddComponent<
                    RewardFragmentMotion>();

            motion.Initialize(
                velocity,
                fragmentLifetime,
                fragmentGravity
            );
        }
    }

    private Bounds CalculateVisualBounds()
    {
        bool hasBounds = false;

        Bounds combined =
            new Bounds(
                transform.position,
                Vector3.one
            );

        for (int i = 0;
             i < activeRenderers.Length;
             i++)
        {
            Renderer renderer =
                activeRenderers[i];

            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject
                    .activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                combined =
                    renderer.bounds;

                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(
                    renderer.bounds
                );
            }
        }

        if (!hasBounds &&
            triggerCollider != null)
        {
            combined =
                triggerCollider.bounds;
        }

        return combined;
    }

    private Material FindSourceMaterial()
    {
        for (int i = 0;
             i < activeRenderers.Length;
             i++)
        {
            Renderer renderer =
                activeRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            Material[] materials =
                renderer.sharedMaterials;

            for (int j = 0;
                 j < materials.Length;
                 j++)
            {
                if (materials[j] != null)
                {
                    return materials[j];
                }
            }
        }

        return null;
    }

    private void OnTriggerEnter(
        Collider other)
    {
        TryRegisterPlayer(other);
    }

    private void OnTriggerStay(
        Collider other)
    {
        TryRegisterPlayer(other);
    }

    private void TryRegisterPlayer(
        Collider other)
    {
        if (!rewardReady ||
            rewardUsed ||
            other == null)
        {
            return;
        }

        Player player =
            other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        if (currentPlayer != null &&
            currentPlayer != player)
        {
            return;
        }

        playerColliders.Add(other);

        if (currentPlayer == player &&
            currentBoonInfo != null &&
            currentPlayerInteraction != null)
        {
            return;
        }

        BoonInfo boonInfo =
            player.GetComponent<BoonInfo>();

        if (boonInfo == null)
        {
            boonInfo =
                player.GetComponentInChildren<
                    BoonInfo>(true);
        }

        PlayerInteraction interaction =
            player.GetComponent<
                PlayerInteraction>();

        if (interaction == null)
        {
            interaction =
                player.GetComponentInChildren<
                    PlayerInteraction>(true);
        }

        if (boonInfo == null ||
            interaction == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] Player에 " +
                "BoonInfo와 PlayerInteraction이 필요합니다.",
                player
            );

            return;
        }

        currentPlayer = player;
        currentBoonInfo = boonInfo;
        currentPlayerInteraction =
            interaction;

        currentPlayerInteraction
            .RegisterInteractable(this);
    }

    private void OnTriggerExit(
        Collider other)
    {
        Player player =
            other.GetComponentInParent<Player>();

        if (player == null ||
            player != currentPlayer)
        {
            return;
        }

        playerColliders.Remove(other);

        if (playerColliders.Count > 0)
        {
            return;
        }

        ClearPlayerReference(false);
    }

    private void ClearPlayerReference(
        bool unblockInteraction)
    {
        if (currentPlayerInteraction != null)
        {
            currentPlayerInteraction
                .UnregisterInteractable(this);

            if (unblockInteraction)
            {
                currentPlayerInteraction
                    .SetInteractionBlocked(false);
            }
        }

        playerColliders.Clear();

        currentPlayer = null;
        currentBoonInfo = null;
        currentPlayerInteraction = null;
    }

    private void UnblockInteraction()
    {
        if (currentPlayerInteraction != null)
        {
            currentPlayerInteraction
                .SetInteractionBlocked(false);
        }
    }

    /// <summary>
    /// RunFlowManager에 저장된 현재 방 타입으로 판정한다.
    /// </summary>
    private static bool IsCurrentRoomJakdu()
    {
        RunFlowManager manager =
            RunFlowManager.Instance;

        return
            manager != null &&
            manager.IsRunActive &&
            manager.CurrentRoomType ==
                RoomType.Jakdu;
    }

    private void EnsureLandingLight()
    {
        if (landingLight != null)
        {
            return;
        }

        Transform found =
            transform.Find(
                "RuntimeLandingLight"
            );

        GameObject lightObject;

        if (found != null)
        {
            lightObject =
                found.gameObject;
        }
        else
        {
            lightObject =
                new GameObject(
                    "RuntimeLandingLight"
                );

            lightObject.transform.SetParent(
                transform,
                false
            );

            lightObject.transform.localPosition =
                Vector3.up * 0.5f;
        }

        landingLight =
            lightObject.GetComponent<Light>();

        if (landingLight == null)
        {
            landingLight =
                lightObject.AddComponent<Light>();
        }

        landingLight.type =
            LightType.Point;

        landingLight.shadows =
            LightShadows.None;

        landingLight.intensity = 0f;
        landingLight.enabled = false;
    }

    private void ResolveUI()
    {
        if (boonRewardUI != null)
        {
            return;
        }

        boonRewardUI =
            FindFirstObjectByType<
                BoonRewardUI>(
                    FindObjectsInactive.Include
                );

        if (boonRewardUI == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] " +
                "BoonRewardUI를 찾지 못했습니다.",
                this
            );
        }
    }

    private void LoadBoonData()
    {
        allBoons.Clear();

        if (boonDatabase != null &&
            boonDatabase.Boons != null)
        {
            allBoons.AddRange(
                boonDatabase.Boons
            );
        }

        if (allBoons.Count > 0)
        {
            return;
        }

        BoonData[] loaded =
            Resources.LoadAll<BoonData>(
                resourcesPath
            );

        if (loaded != null &&
            loaded.Length > 0)
        {
            allBoons.AddRange(loaded);
        }

        if (allBoons.Count == 0)
        {
            Debug.LogError(
                "[BoonRewardInteractable] " +
                "득도 데이터가 없습니다.",
                this
            );
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

    private void OnDisable()
    {
        if (appearanceRoutine != null)
        {
            StopCoroutine(appearanceRoutine);
            appearanceRoutine = null;
        }

        rewardReady = false;
        selectionInProgress = false;
        completedCallback = null;

        if (landingLight != null)
        {
            landingLight.intensity = 0f;
            landingLight.enabled = false;
        }

        ClearPlayerReference(true);
    }
}

/// <summary>
/// 획득 시 생성된 작은 파편을 이동시키고 제거한다.
/// 원본 보상 오브젝트가 꺼져도 독립적으로 계속 실행된다.
/// </summary>
internal sealed class RewardFragmentMotion :
    MonoBehaviour
{
    private Vector3 velocity;
    private float lifetime;
    private float gravity;
    private float elapsed;

    private Vector3 initialScale;
    private Renderer targetRenderer;
    private Material runtimeMaterial;
    private string colorProperty;
    private Color initialColor;

    public void Initialize(
        Vector3 startVelocity,
        float duration,
        float gravityStrength)
    {
        velocity = startVelocity;
        lifetime =
            Mathf.Max(0.1f, duration);

        gravity =
            Mathf.Max(0f, gravityStrength);

        initialScale =
            transform.localScale;

        targetRenderer =
            GetComponent<Renderer>();

        PrepareFadeMaterial();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        velocity +=
            Vector3.down *
            gravity *
            Time.deltaTime;

        transform.position +=
            velocity *
            Time.deltaTime;

        transform.Rotate(
            280f * Time.deltaTime,
            360f * Time.deltaTime,
            220f * Time.deltaTime,
            Space.Self
        );

        float ratio =
            Mathf.Clamp01(
                elapsed / lifetime
            );

        float remaining =
            1f - ratio;

        transform.localScale =
            initialScale *
            Mathf.Max(
                0.001f,
                remaining
            );

        ApplyAlpha(remaining);

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void PrepareFadeMaterial()
    {
        if (targetRenderer == null ||
            targetRenderer.sharedMaterial == null)
        {
            return;
        }

        runtimeMaterial =
            new Material(
                targetRenderer.sharedMaterial
            );

        targetRenderer.sharedMaterial =
            runtimeMaterial;

        if (runtimeMaterial.HasProperty(
                "_BaseColor"))
        {
            colorProperty =
                "_BaseColor";
        }
        else if (runtimeMaterial.HasProperty(
                     "_Color"))
        {
            colorProperty =
                "_Color";
        }
        else
        {
            return;
        }

        initialColor =
            runtimeMaterial.GetColor(
                colorProperty
            );
    }

    private void ApplyAlpha(
        float alpha)
    {
        if (runtimeMaterial == null ||
            string.IsNullOrEmpty(
                colorProperty))
        {
            return;
        }

        Color color =
            initialColor;

        color.a *= alpha;

        runtimeMaterial.SetColor(
            colorProperty,
            color
        );
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}