using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상점방 전용 득도 구매 오브젝트.
///
/// 기존 BoonRewardInteractable을 상점용으로 복사한 버전이다.
/// 차이점:
/// 1. 전투 클리어 조건 없이 Start에서 바로 보상 오브젝트를 준비한다.
/// 2. Interact 시 플레이어 골드를 먼저 검사한다.
/// 3. 골드가 부족하면 "골드가 부족합니다" 문구를 띄운다.
/// 4. 득도 선택을 실제로 완료했을 때만 골드를 차감한다.
/// 5. 기존 BoonRewardInteractable과 클래스명이 다르므로 같이 존재해도 된다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class ShopBoonRewardInteractable : InteractableBase
{
    [Header("상점 설정")]
    [Min(0)]
    [SerializeField] private int price = 100;

    [Tooltip("None이면 Attack, Defense, Mobility, Debuff 전체에서 후보를 뽑는다.")]
    [SerializeField] private BoonCategory shopRewardCategory = BoonCategory.None;

    [Tooltip("상점방 시작 시 바로 구매 가능한 상태로 준비")]
    [SerializeField] private bool prepareOnStart = true;

    [Tooltip("상점 오브젝트 위치에서 보상 위치를 살짝 보정")]
    [SerializeField] private Vector3 shopPositionOffset;

    [Header("골드 부족 표시")]
    [SerializeField] private string insufficientGoldMessage = "Not enough gold";

    [Tooltip("비워두면 런타임에 3D TMP 텍스트를 자동 생성한다.")]
    [SerializeField] private TextMeshPro warningText;

    [SerializeField] private Vector3 warningTextOffset = new Vector3(0f, 2.2f, 0f);

    [Min(0.1f)]
    [SerializeField] private float warningDuration = 1.2f;

    [Min(0.1f)]
    [SerializeField] private float warningMoveUpDistance = 0.45f;

    [Header("보상 선택")]
    [Range(1, 3)]
    [SerializeField] private int choiceCount = 3;

    [Header("보상 UI")]
    [SerializeField] private BoonRewardUI boonRewardUI;

    [Header("보상 UI 폰트")]
    [Tooltip("보상 UI가 열렸을 때 폰트를 적용할지 여부")]
    [SerializeField] private bool applyRewardUIFont = true;

    [Tooltip("decide1, decide2, decide3 또는 그 부모 오브젝트를 넣는다. 비워두면 BoonRewardUI 오브젝트 아래 전체에 적용한다.")]
    [SerializeField] private GameObject[] rewardFontTargets = new GameObject[0];

    [Tooltip("TextMeshPro용 폰트. TMP Font Asset을 넣어야 한다.")]
    [SerializeField] private TMP_FontAsset rewardTmpFont;

    [Tooltip("기본 UI Text용 폰트. TMP가 아니라 UnityEngine.UI.Text일 때만 사용한다.")]
    [SerializeField] private Font rewardLegacyFont;

    [Header("득도 데이터")]
    [SerializeField] private BoonDatabase boonDatabase;

    [Tooltip("BoonDatabase가 비어 있을 때 검색할 Resources 폴더")]
    [SerializeField] private string resourcesPath = "Boons";

    [Header("계열별 월드 프리팹")]
    [Tooltip("None 또는 기본 표시용 프리팹")]
    [SerializeField] private GameObject defaultRewardPrefab;

    [SerializeField] private GameObject attackRewardPrefab;
    [SerializeField] private GameObject defenseRewardPrefab;
    [SerializeField] private GameObject mobilityRewardPrefab;
    [SerializeField] private GameObject debuffRewardPrefab;

    [Tooltip("상점에서 Jakdu 계열을 팔 때만 사용")]
    [SerializeField] private GameObject jakduRewardPrefab;

    [Header("작두 프리팹 위치 보정")]
    [SerializeField] private Vector3 jakduVisualLocalPosition;
    [SerializeField] private Vector3 jakduVisualLocalRotation;
    [SerializeField] private Vector3 jakduVisualScaleMultiplier = Vector3.one;

    [Header("등장 연출")]
    [Tooltip("0이면 제자리에서 바로 등장한다.")]
    [Min(0f)]
    [SerializeField] private float dropHeight = 0f;

    [Min(0.01f)]
    [SerializeField] private float dropDuration = 0.28f;

    [Tooltip("등장 후 살짝 튀는 높이. 상점에서는 0이어도 된다.")]
    [Min(0f)]
    [SerializeField] private float landingBounceHeight = 0f;

    [Min(0.01f)]
    [SerializeField] private float landingBounceDuration = 0.1f;

    [Header("등장 발광")]
    [Tooltip("비어 있으면 런타임에 Point Light를 자동 생성")]
    [SerializeField] private Light landingLight;

    [Min(0f)]
    [SerializeField] private float flashIntensity = 6f;

    [Min(0f)]
    [SerializeField] private float flashRange = 4f;

    [Min(0.01f)]
    [SerializeField] private float flashDuration = 0.14f;

    [Min(1f)]
    [SerializeField] private float flashScaleMultiplier = 1.08f;

    [Header("구매 완료 파편 연출")]
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

    [Header("구매 후 처리")]
    [Tooltip("구매가 끝나면 오브젝트를 비활성화한다.")]
    [SerializeField] private bool hideAfterPurchase = true;

    [Header("디버그")]
    [SerializeField] private bool showLogs = true;

    private readonly List<BoonData> allBoons = new List<BoonData>();
    private readonly List<BoonData> availableBoons = new List<BoonData>(32);
    private readonly List<BoonData> selectedChoices = new List<BoonData>(3);
    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

    private Player currentPlayer;
    private BoonInfo currentBoonInfo;
    private PlayerInteraction currentPlayerInteraction;

    private BoxCollider triggerCollider;
    private GameObject activeVisual;
    private Renderer[] activeRenderers = Array.Empty<Renderer>();
    private Vector3 activeVisualBaseScale = Vector3.one;

    private Coroutine appearanceRoutine;
    private Coroutine warningRoutine;

    private bool rewardReady;
    private bool rewardUsed;
    private bool selectionInProgress;

    private void Awake()
    {
        NormalizeMessages();
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.enabled = false;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.detectCollisions = true;

        EnsureLandingLight();
        EnsureWarningText();
        ResolveUI();
        LoadBoonData();
    }

    private void Start()
    {
        if (prepareOnStart)
        {
            PrepareShopReward();
        }
    }

    private void OnValidate()
    {
        NormalizeMessages();
        BoxCollider box = GetComponent<BoxCollider>();

        if (box != null)
        {
            box.isTrigger = true;
        }

        Rigidbody body = GetComponent<Rigidbody>();

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
    /// 상점방 시작 시 또는 외부에서 다시 진열할 때 호출한다.
    /// 전투 클리어 조건을 전혀 사용하지 않는다.
    /// </summary>
    public void PrepareShopReward()
    {
        Vector3 shopPosition = transform.position + shopPositionOffset;
        PrepareAt(shopRewardCategory, shopPosition);
    }

    /// <summary>
    /// 상점용 득도 보상을 지정 위치에 준비한다.
    /// </summary>
    public void PrepareAt(BoonCategory category, Vector3 targetPosition)
    {
        if (appearanceRoutine != null)
        {
            StopCoroutine(appearanceRoutine);
            appearanceRoutine = null;
        }

        rewardReady = false;
        rewardUsed = false;
        selectionInProgress = false;

        ClearPlayerReference(true);
        BuildCategoryVisual(category);

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider>();
        }

        triggerCollider.enabled = false;
        triggerCollider.isTrigger = true;

        gameObject.SetActive(true);

        appearanceRoutine = StartCoroutine(AppearRoutine(targetPosition));

        if (showLogs)
        {
            Debug.Log(
                $"[ShopBoonRewardInteractable] 상점 득도 준비 / " +
                $"계열={category}, 가격={price}, 위치={targetPosition}",
                this
            );
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
            currentPlayer != null &&
            currentBoonInfo != null &&
            currentPlayerInteraction != null &&
            boonRewardUI != null &&
            !boonRewardUI.IsOpen;
    }

    public override void Interact(Player player)
    {
        if (!CanInteract() ||
            player == null ||
            player != currentPlayer)
        {
            return;
        }

        if (player.money == null ||
            !player.money.CanSpend(price))
        {
            ShowInsufficientGoldText();
            return;
        }

        CreateChoices();

        if (selectedChoices.Count == 0)
        {
            Debug.LogWarning(
                "[ShopBoonRewardInteractable] " +
                "현재 구매 가능한 득도 후보가 없습니다.",
                this
            );

            ShowWarningText("No boons available");
            return;
        }

        bool opened = boonRewardUI.Open(
            selectedChoices,
            HandleSelected,
            HandleCancelled
        );

        if (!opened)
        {
            return;
        }

        ApplyRewardUIFont();

        selectionInProgress = true;

        currentPlayerInteraction.SetInteractionBlocked(true);
    }

    private IEnumerator AppearRoutine(Vector3 targetPosition)
    {
        Vector3 startPosition = targetPosition + Vector3.up * dropHeight;

        transform.position = startPosition;

        if (activeVisual != null)
        {
            activeVisual.SetActive(true);
            activeVisual.transform.localScale = activeVisualBaseScale;
        }

        if (dropHeight > 0f)
        {
            float elapsed = 0f;

            while (elapsed < dropDuration)
            {
                elapsed += Time.deltaTime;

                float ratio = Mathf.Clamp01(elapsed / dropDuration);
                float fallRatio = ratio * ratio;

                transform.position = Vector3.LerpUnclamped(
                    startPosition,
                    targetPosition,
                    fallRatio
                );

                yield return null;
            }
        }

        transform.position = targetPosition;

        if (landingBounceHeight > 0f)
        {
            yield return BounceRoutine(targetPosition);
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
                "[ShopBoonRewardInteractable] 상점 득도 상호작용 가능",
                this
            );
        }
    }

    private IEnumerator BounceRoutine(Vector3 landingPosition)
    {
        float halfDuration = landingBounceDuration * 0.5f;

        if (halfDuration <= 0f)
        {
            yield break;
        }

        Vector3 bouncePosition = landingPosition + Vector3.up * landingBounceHeight;

        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / halfDuration);

            transform.position = Vector3.Lerp(
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
            float ratio = Mathf.Clamp01(elapsed / halfDuration);

            transform.position = Vector3.Lerp(
                bouncePosition,
                landingPosition,
                SmoothStep(ratio)
            );

            yield return null;
        }

        transform.position = landingPosition;
    }

    private IEnumerator FlashRoutine()
    {
        EnsureLandingLight();

        if (landingLight != null && flashIntensity > 0f)
        {
            landingLight.range = flashRange;
            landingLight.intensity = flashIntensity;
            landingLight.enabled = true;
        }

        Transform scaleTarget = activeVisual != null ? activeVisual.transform : transform;
        Vector3 baseScale = activeVisual != null ? activeVisualBaseScale : transform.localScale;
        Vector3 flashScale = baseScale * flashScaleMultiplier;

        if (scaleTarget != null)
        {
            scaleTarget.localScale = flashScale;
        }

        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float ratio = Mathf.Clamp01(elapsed / flashDuration);
            float smoothRatio = SmoothStep(ratio);

            if (landingLight != null)
            {
                landingLight.intensity = Mathf.Lerp(
                    flashIntensity,
                    0f,
                    smoothRatio
                );
            }

            if (scaleTarget != null)
            {
                scaleTarget.localScale = Vector3.Lerp(
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
            scaleTarget.localScale = baseScale;
        }
    }

    private void BuildCategoryVisual(BoonCategory category)
    {
        if (activeVisual != null)
        {
            Destroy(activeVisual);
            activeVisual = null;
        }

        GameObject selectedPrefab = GetRewardPrefab(category);

        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "[ShopBoonRewardInteractable] " +
                "보상 월드 프리팹이 비어 있습니다. " +
                "상호작용은 가능하지만 시각 오브젝트는 생성되지 않습니다.",
                this
            );

            activeRenderers = Array.Empty<Renderer>();
            return;
        }

        activeVisual = Instantiate(selectedPrefab, transform);
        activeVisual.name = $"{selectedPrefab.name}_ShopBoonReward";

        if (category == BoonCategory.Jakdu)
        {
            activeVisual.transform.localPosition = jakduVisualLocalPosition;
            activeVisual.transform.localRotation = Quaternion.Euler(jakduVisualLocalRotation);
            activeVisual.transform.localScale = Vector3.Scale(
                activeVisual.transform.localScale,
                jakduVisualScaleMultiplier
            );
        }
        else
        {
            activeVisual.transform.localPosition = Vector3.zero;
            activeVisual.transform.localRotation = Quaternion.identity;
        }

        activeVisualBaseScale = activeVisual.transform.localScale;
        activeVisual.SetActive(true);

        Collider[] childColliders = activeVisual.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < childColliders.Length; i++)
        {
            childColliders[i].enabled = false;
        }

        activeRenderers = activeVisual.GetComponentsInChildren<Renderer>(true);
    }

    private GameObject GetRewardPrefab(BoonCategory category)
    {
        switch (category)
        {
            case BoonCategory.Attack:
                return attackRewardPrefab != null ? attackRewardPrefab : defaultRewardPrefab;

            case BoonCategory.Defense:
                return defenseRewardPrefab != null ? defenseRewardPrefab : defaultRewardPrefab;

            case BoonCategory.Mobility:
                return mobilityRewardPrefab != null ? mobilityRewardPrefab : defaultRewardPrefab;

            case BoonCategory.Debuff:
                return debuffRewardPrefab != null ? debuffRewardPrefab : defaultRewardPrefab;

            case BoonCategory.Jakdu:
                return jakduRewardPrefab != null ? jakduRewardPrefab : defaultRewardPrefab;

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

        for (int i = 0; i < allBoons.Count; i++)
        {
            BoonData boon = allBoons[i];

            if (boon == null ||
                !IsAllowedShopCategory(boon.category) ||
                !currentBoonInfo.CanOffer(boon))
            {
                continue;
            }

            availableBoons.Add(boon);
        }

        int resultCount = Mathf.Min(choiceCount, availableBoons.Count);

        for (int i = 0; i < resultCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, availableBoons.Count);

            BoonData temporary = availableBoons[i];
            availableBoons[i] = availableBoons[randomIndex];
            availableBoons[randomIndex] = temporary;

            selectedChoices.Add(availableBoons[i]);
        }
    }

    private bool IsAllowedShopCategory(BoonCategory category)
    {
        if (shopRewardCategory != BoonCategory.None)
        {
            return category == shopRewardCategory;
        }

        return
            category == BoonCategory.Attack ||
            category == BoonCategory.Defense ||
            category == BoonCategory.Mobility ||
            category == BoonCategory.Debuff;
    }

    private void ApplyRewardUIFont()
    {
        if (!applyRewardUIFont)
        {
            return;
        }

        if (rewardFontTargets == null || rewardFontTargets.Length == 0)
        {
            if (boonRewardUI != null)
            {
                ApplyFontsToObject(boonRewardUI.gameObject);
            }

            return;
        }

        for (int i = 0; i < rewardFontTargets.Length; i++)
        {
            GameObject target = rewardFontTargets[i];

            if (target == null)
            {
                continue;
            }

            ApplyFontsToObject(target);
        }
    }

    private void ApplyFontsToObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (rewardTmpFont != null)
        {
            TMP_Text[] tmpTexts = target.GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < tmpTexts.Length; i++)
            {
                TMP_Text text = tmpTexts[i];

                if (text == null)
                {
                    continue;
                }

                text.font = rewardTmpFont;
                text.ForceMeshUpdate();
            }
        }

        if (rewardLegacyFont != null)
        {
            Text[] legacyTexts = target.GetComponentsInChildren<Text>(true);

            for (int i = 0; i < legacyTexts.Length; i++)
            {
                Text text = legacyTexts[i];

                if (text == null)
                {
                    continue;
                }

                text.font = rewardLegacyFont;
            }
        }
    }

    private void HandleSelected(BoonData selectedBoon)
    {
        selectionInProgress = false;

        if (selectedBoon == null ||
            currentPlayer == null ||
            currentBoonInfo == null)
        {
            UnblockInteraction();
            return;
        }

        if (currentPlayer.money == null ||
            !currentPlayer.money.TrySpend(price))
        {
            ShowInsufficientGoldText();
            UnblockInteraction();
            return;
        }

        if (!currentBoonInfo.TryAddBoon(selectedBoon))
        {
            // 득도 획득에 실패했다면 이미 차감한 골드를 되돌린다.
            currentPlayer.money.AddMoney(price);

            Debug.LogWarning(
                $"[ShopBoonRewardInteractable] {selectedBoon.displayName} 획득 실패 / 골드 환불",
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
            currentPlayerInteraction.RegisterInteractable(this);
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

        ClearPlayerReference(true);

        if (playBreakEffect)
        {
            PlayBreakEffect();
        }

        if (activeVisual != null)
        {
            activeVisual.SetActive(false);
        }

        if (hideAfterPurchase)
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayBreakEffect()
    {
        Bounds bounds = CalculateVisualBounds();
        Material sourceMaterial = FindSourceMaterial();

        float averageSize = (bounds.size.x + bounds.size.y + bounds.size.z) / 3f;
        float baseSize = Mathf.Max(0.03f, averageSize * fragmentSizeRatio);

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fragment.name = "ShopBoonRewardFragment";

            Collider fragmentCollider = fragment.GetComponent<Collider>();

            if (fragmentCollider != null)
            {
                Destroy(fragmentCollider);
            }

            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-bounds.extents.x, bounds.extents.x),
                UnityEngine.Random.Range(-bounds.extents.y, bounds.extents.y),
                UnityEngine.Random.Range(-bounds.extents.z, bounds.extents.z)
            );

            fragment.transform.position = bounds.center + randomOffset;
            fragment.transform.rotation = UnityEngine.Random.rotation;

            float randomSize = baseSize * UnityEngine.Random.Range(0.65f, 1.35f);
            fragment.transform.localScale = Vector3.one * randomSize;

            Renderer renderer = fragment.GetComponent<Renderer>();

            if (renderer != null && sourceMaterial != null)
            {
                renderer.sharedMaterial = sourceMaterial;
            }

            Vector3 direction = fragment.transform.position - bounds.center;

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = UnityEngine.Random.onUnitSphere;
            }

            direction.Normalize();

            Vector3 velocity =
                direction * fragmentForce +
                Vector3.up * fragmentUpwardForce +
                UnityEngine.Random.insideUnitSphere * fragmentForce * 0.25f;

            ShopBoonRewardFragmentMotion motion = fragment.AddComponent<ShopBoonRewardFragmentMotion>();
            motion.Initialize(velocity, fragmentLifetime, fragmentGravity);
        }
    }

    private Bounds CalculateVisualBounds()
    {
        bool hasBounds = false;
        Bounds combined = new Bounds(transform.position, Vector3.one);

        for (int i = 0; i < activeRenderers.Length; i++)
        {
            Renderer renderer = activeRenderers[i];

            if (renderer == null ||
                !renderer.enabled ||
                !renderer.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!hasBounds)
            {
                combined = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds && triggerCollider != null)
        {
            combined = triggerCollider.bounds;
        }

        return combined;
    }

    private Material FindSourceMaterial()
    {
        for (int i = 0; i < activeRenderers.Length; i++)
        {
            Renderer renderer = activeRenderers[i];

            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] != null)
                {
                    return materials[j];
                }
            }
        }

        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegisterPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryRegisterPlayer(other);
    }

    private void TryRegisterPlayer(Collider other)
    {
        if (!rewardReady ||
            rewardUsed ||
            other == null)
        {
            return;
        }

        Player player = other.GetComponentInParent<Player>();

        if (player == null)
        {
            return;
        }

        if (currentPlayer != null && currentPlayer != player)
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

        BoonInfo boonInfo = player.GetComponent<BoonInfo>();

        if (boonInfo == null)
        {
            boonInfo = player.GetComponentInChildren<BoonInfo>(true);
        }

        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();

        if (interaction == null)
        {
            interaction = player.GetComponentInChildren<PlayerInteraction>(true);
        }

        if (boonInfo == null || interaction == null)
        {
            Debug.LogError(
                "[ShopBoonRewardInteractable] Player에 BoonInfo와 PlayerInteraction이 필요합니다.",
                player
            );

            return;
        }

        currentPlayer = player;
        currentBoonInfo = boonInfo;
        currentPlayerInteraction = interaction;

        currentPlayerInteraction.RegisterInteractable(this);
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponentInParent<Player>();

        if (player == null || player != currentPlayer)
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

    private void ClearPlayerReference(bool unblockInteraction)
    {
        if (currentPlayerInteraction != null)
        {
            currentPlayerInteraction.UnregisterInteractable(this);

            if (unblockInteraction)
            {
                currentPlayerInteraction.SetInteractionBlocked(false);
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
            currentPlayerInteraction.SetInteractionBlocked(false);
        }
    }

    private void ResolveUI()
    {
        if (boonRewardUI != null)
        {
            return;
        }

        boonRewardUI = FindFirstObjectByType<BoonRewardUI>(FindObjectsInactive.Include);

        if (boonRewardUI == null)
        {
            Debug.LogError(
                "[ShopBoonRewardInteractable] BoonRewardUI를 찾지 못했습니다.",
                this
            );
        }
    }

    private void LoadBoonData()
    {
        allBoons.Clear();

        if (boonDatabase != null && boonDatabase.Boons != null)
        {
            allBoons.AddRange(boonDatabase.Boons);
        }

        if (allBoons.Count > 0)
        {
            return;
        }

        BoonData[] loaded = Resources.LoadAll<BoonData>(resourcesPath);

        if (loaded != null && loaded.Length > 0)
        {
            allBoons.AddRange(loaded);
        }

        if (allBoons.Count == 0)
        {
            Debug.LogError(
                "[ShopBoonRewardInteractable] 득도 데이터가 없습니다.",
                this
            );
        }
    }

    private void EnsureLandingLight()
    {
        if (landingLight != null)
        {
            return;
        }

        Transform found = transform.Find("RuntimeShopBoonLight");
        GameObject lightObject;

        if (found != null)
        {
            lightObject = found.gameObject;
        }
        else
        {
            lightObject = new GameObject("RuntimeShopBoonLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * 0.5f;
        }

        landingLight = lightObject.GetComponent<Light>();

        if (landingLight == null)
        {
            landingLight = lightObject.AddComponent<Light>();
        }

        landingLight.type = LightType.Point;
        landingLight.shadows = LightShadows.None;
        landingLight.intensity = 0f;
        landingLight.enabled = false;
    }

    private void EnsureWarningText()
    {
        if (warningText == null)
        {
            Transform found = transform.Find("RuntimeShopWarningText");

            GameObject textObject;

            if (found != null)
            {
                textObject = found.gameObject;
            }
            else
            {
                textObject = new GameObject("RuntimeShopWarningText");
                textObject.transform.SetParent(transform, false);
            }

            warningText = textObject.GetComponent<TextMeshPro>();

            if (warningText == null)
            {
                warningText = textObject.AddComponent<TextMeshPro>();
            }

            warningText.alignment = TextAlignmentOptions.Center;
            warningText.fontSize = 3f;
            warningText.text = string.Empty;
        }

        warningText.gameObject.SetActive(false);
    }

    private void ShowInsufficientGoldText()
    {
        ShowWarningText(insufficientGoldMessage);
    }

    private void NormalizeMessages()
    {
        insufficientGoldMessage =
            "Not enough gold";
    }

    private void ShowWarningText(string message)
    {
        EnsureWarningText();

        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
            warningRoutine = null;
        }

        warningRoutine = StartCoroutine(WarningTextRoutine(message));
    }

    private IEnumerator WarningTextRoutine(string message)
    {
        if (warningText == null)
        {
            yield break;
        }

        Transform textTransform = warningText.transform;
        Vector3 startLocalPosition = warningTextOffset;
        Vector3 endLocalPosition = warningTextOffset + Vector3.up * warningMoveUpDistance;

        warningText.text = message;
        warningText.alpha = 1f;
        warningText.gameObject.SetActive(true);
        textTransform.localPosition = startLocalPosition;

        Camera mainCamera = Camera.main;

        float elapsed = 0f;

        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;

            float ratio = Mathf.Clamp01(elapsed / warningDuration);
            float smoothRatio = SmoothStep(ratio);

            textTransform.localPosition = Vector3.Lerp(
                startLocalPosition,
                endLocalPosition,
                smoothRatio
            );

            warningText.alpha = Mathf.Lerp(1f, 0f, smoothRatio);

            if (mainCamera != null)
            {
                textTransform.rotation = mainCamera.transform.rotation;
            }

            yield return null;
        }

        warningText.gameObject.SetActive(false);
        warningText.alpha = 1f;
        warningRoutine = null;
    }

    private static float SmoothStep(float value)
    {
        value = Mathf.Clamp01(value);
        return value * value * (3f - 2f * value);
    }

    private void OnDisable()
    {
        if (appearanceRoutine != null)
        {
            StopCoroutine(appearanceRoutine);
            appearanceRoutine = null;
        }

        if (warningRoutine != null)
        {
            StopCoroutine(warningRoutine);
            warningRoutine = null;
        }

        rewardReady = false;
        selectionInProgress = false;

        if (landingLight != null)
        {
            landingLight.intensity = 0f;
            landingLight.enabled = false;
        }

        if (warningText != null)
        {
            warningText.gameObject.SetActive(false);
        }

        ClearPlayerReference(true);
    }
}

/// <summary>
/// 상점 득도 구매 완료 시 생성되는 작은 파편 이동 스크립트.
/// 기존 RewardFragmentMotion과 이름이 달라서 중복 클래스 오류가 나지 않는다.
/// </summary>
internal sealed class ShopBoonRewardFragmentMotion : MonoBehaviour
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

    public void Initialize(Vector3 startVelocity, float duration, float gravityStrength)
    {
        velocity = startVelocity;
        lifetime = Mathf.Max(0.1f, duration);
        gravity = Mathf.Max(0f, gravityStrength);

        initialScale = transform.localScale;
        targetRenderer = GetComponent<Renderer>();

        PrepareFadeMaterial();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        velocity += Vector3.down * gravity * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        transform.Rotate(
            280f * Time.deltaTime,
            360f * Time.deltaTime,
            220f * Time.deltaTime,
            Space.Self
        );

        float ratio = Mathf.Clamp01(elapsed / lifetime);
        float remaining = 1f - ratio;

        transform.localScale = initialScale * Mathf.Max(0.001f, remaining);

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

        runtimeMaterial = new Material(targetRenderer.sharedMaterial);
        targetRenderer.sharedMaterial = runtimeMaterial;

        if (runtimeMaterial.HasProperty("_BaseColor"))
        {
            colorProperty = "_BaseColor";
        }
        else if (runtimeMaterial.HasProperty("_Color"))
        {
            colorProperty = "_Color";
        }
        else
        {
            return;
        }

        initialColor = runtimeMaterial.GetColor(colorProperty);
    }

    private void ApplyAlpha(float alpha)
    {
        if (runtimeMaterial == null ||
            string.IsNullOrEmpty(colorProperty))
        {
            return;
        }

        Color color = initialColor;
        color.a *= alpha;

        runtimeMaterial.SetColor(colorProperty, color);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
