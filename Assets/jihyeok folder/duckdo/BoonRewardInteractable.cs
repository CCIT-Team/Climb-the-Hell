using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 전투 클리어 후 CombatRoomFlow가 오브젝트를 켜면
/// 플레이어가 범위 안에서 E키로 득도 보상을 선택한다.
/// 보상 획득 시 원본 모델을 숨기고 조각 파괴/Fade 연출을 실행한다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Rigidbody))]
public class BoonRewardInteractable : InteractableBase
{
    [Header("보상 설정")]
    [Range(1, 3)]
    [SerializeField] private int choiceCount = 3;

    [Header("UI")]
    [SerializeField] private BoonRewardUI boonRewardUI;

    [Header("득도 데이터")]
    [SerializeField] private BoonDatabase boonDatabase;

    [Tooltip("BoonDatabase가 비어 있을 때 검색할 Resources 폴더")]
    [SerializeField] private string resourcesPath = "Boons";

    [Header("보상 파괴 연출")]
    [SerializeField] private bool playBreakEffect = true;

    [Range(4, 40)]
    [SerializeField] private int fragmentCount = 18;

    [Min(0.1f)]
    [SerializeField] private float fragmentLifetime = 1.2f;

    [Range(0f, 0.95f)]
    [Tooltip("전체 수명 중 투명해지기 시작하는 시점")]
    [SerializeField] private float fragmentFadeStartRatio = 0.3f;

    [Min(0f)]
    [SerializeField] private float fragmentForce = 3.5f;

    [Min(0f)]
    [SerializeField] private float fragmentUpwardForce = 2f;

    [Min(0f)]
    [SerializeField] private float fragmentGravity = 4f;

    [Min(0f)]
    [SerializeField] private float fragmentMoveDamping = 1.2f;

    [Min(0f)]
    [SerializeField] private float fragmentAngularSpeed = 420f;

    [Range(0.03f, 0.5f)]
    [SerializeField] private float fragmentSizeRatio = 0.11f;

    [Tooltip("비워두면 보상 오브젝트의 첫 번째 머티리얼을 사용")]
    [SerializeField] private Material fragmentMaterialOverride;

    [Header("디버그")]
    [SerializeField] private bool showLogs = true;

    private readonly List<BoonData> allBoons = new List<BoonData>();
    private readonly List<BoonData> availableBoons = new List<BoonData>(32);
    private readonly List<BoonData> selectedChoices = new List<BoonData>(3);
    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();

    private Player currentPlayer;
    private BoonInfo currentBoonInfo;
    private PlayerInteraction currentPlayerInteraction;

    private BoonCategory targetCategory = BoonCategory.None;
    private Action completedCallback;

    private bool rewardReady;
    private bool rewardUsed;
    private bool selectionInProgress;

    private BoxCollider triggerCollider;
    private Renderer[] rewardRenderers;
    private bool[] initialRendererStates;

    private void Awake()
    {
        triggerCollider = GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;

        Rigidbody body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.detectCollisions = true;

        CacheRewardRenderers();
        ResolveUI();
        LoadBoonData();
    }

    private void OnValidate()
    {
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        Rigidbody body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.isKinematic = true;
            body.useGravity = false;
        }
    }

    /// <summary>
    /// CombatRoomFlow가 보상 오브젝트를 켠 직후 호출한다.
    /// </summary>
    public void Prepare(BoonCategory category, Action onCompleted)
    {
        targetCategory = category;
        completedCallback = onCompleted;

        rewardReady = true;
        rewardUsed = false;
        selectionInProgress = false;

        ClearPlayerReference(true);
        RestoreRewardRenderers();

        if (triggerCollider == null)
        {
            triggerCollider = GetComponent<BoxCollider>();
        }

        triggerCollider.enabled = true;
        triggerCollider.isTrigger = true;

        if (showLogs)
        {
            Debug.Log(
                $"[BoonRewardInteractable] 보상 준비 완료 / 계열: {category}",
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

        return rewardReady &&
               !rewardUsed &&
               !selectionInProgress &&
               targetCategory != BoonCategory.None &&
               currentPlayer != null &&
               currentBoonInfo != null &&
               currentPlayerInteraction != null &&
               boonRewardUI != null &&
               !boonRewardUI.IsOpen;
    }

    public override void Interact(Player player)
    {
        if (showLogs)
        {
            Debug.Log("[BoonRewardInteractable] E 상호작용 호출", this);
        }

        if (!CanInteract())
        {
            Debug.LogWarning(
                "[BoonRewardInteractable] 현재 상호작용할 수 없습니다. " +
                $"Ready={rewardReady}, Used={rewardUsed}, " +
                $"Selecting={selectionInProgress}, Category={targetCategory}, " +
                $"Player={currentPlayer != null}, " +
                $"BoonInfo={currentBoonInfo != null}, " +
                $"PlayerInteraction={currentPlayerInteraction != null}, " +
                $"UI={boonRewardUI != null}",
                this
            );

            return;
        }

        if (player == null || player != currentPlayer)
        {
            return;
        }

        CreateChoices();

        if (selectedChoices.Count == 0)
        {
            Debug.LogWarning(
                $"[BoonRewardInteractable] {targetCategory} 계열에서 " +
                "획득 가능한 득도가 없어 문을 엽니다.",
                this
            );

            CompleteReward();
            return;
        }

        bool opened = boonRewardUI.Open(
            selectedChoices,
            HandleSelected,
            HandleCancelled
        );

        if (!opened)
        {
            Debug.LogWarning(
                "[BoonRewardInteractable] BoonRewardUI.Open이 false를 반환했습니다.",
                this
            );

            return;
        }

        selectionInProgress = true;
        currentPlayerInteraction.SetInteractionBlocked(true);
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
                boon.category != targetCategory ||
                !currentBoonInfo.CanOffer(boon))
            {
                continue;
            }

            availableBoons.Add(boon);
        }

        int resultCount = Mathf.Min(choiceCount, availableBoons.Count);

        // 필요한 수만큼 부분 Fisher-Yates 셔플.
        for (int i = 0; i < resultCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, availableBoons.Count);

            BoonData temp = availableBoons[i];
            availableBoons[i] = availableBoons[randomIndex];
            availableBoons[randomIndex] = temp;

            selectedChoices.Add(availableBoons[i]);
        }
    }

    private void HandleSelected(BoonData selectedBoon)
    {
        selectionInProgress = false;

        if (selectedBoon == null || currentBoonInfo == null)
        {
            UnblockInteraction();
            return;
        }

        if (!currentBoonInfo.TryAddBoon(selectedBoon))
        {
            Debug.LogWarning(
                $"[BoonRewardInteractable] {selectedBoon.displayName} 획득 실패",
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

        if (showLogs)
        {
            Debug.Log("[BoonRewardInteractable] 보상 획득 완료", this);
        }

        Action callback = completedCallback;
        completedCallback = null;

        ClearPlayerReference(true);

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        /*
         * 콜백이 원본 보상 오브젝트를 즉시 꺼도 연출이 중단되지 않도록
         * 조각은 원본의 자식이 아닌 독립 오브젝트로 생성한다.
         */
        if (playBreakEffect)
        {
            PlayBreakEffect();
        }

        callback?.Invoke();
    }

    private void PlayBreakEffect()
    {
        Bounds rewardBounds = CalculateRewardBounds();
        Material sourceMaterial = FindFragmentSourceMaterial();

        SetRewardRenderersEnabled(false);

        float averageSize =
            (rewardBounds.size.x + rewardBounds.size.y + rewardBounds.size.z) / 3f;

        float baseFragmentSize =
            Mathf.Max(0.02f, averageSize * fragmentSizeRatio);

        for (int i = 0; i < fragmentCount; i++)
        {
            CreateFragment(
                rewardBounds,
                sourceMaterial,
                baseFragmentSize
            );
        }
    }

    private void CreateFragment(
        Bounds rewardBounds,
        Material sourceMaterial,
        float baseFragmentSize)
    {
        GameObject fragment =
            GameObject.CreatePrimitive(PrimitiveType.Cube);

        fragment.name = "BoonRewardFragment";
        fragment.layer = gameObject.layer;

        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(
                -rewardBounds.extents.x,
                rewardBounds.extents.x
            ),
            UnityEngine.Random.Range(
                -rewardBounds.extents.y,
                rewardBounds.extents.y
            ),
            UnityEngine.Random.Range(
                -rewardBounds.extents.z,
                rewardBounds.extents.z
            )
        );

        fragment.transform.position = rewardBounds.center + randomOffset;
        fragment.transform.rotation = UnityEngine.Random.rotation;

        float randomSize =
            baseFragmentSize *
            UnityEngine.Random.Range(0.65f, 1.35f);

        fragment.transform.localScale = new Vector3(
            randomSize * UnityEngine.Random.Range(0.45f, 1.25f),
            randomSize * UnityEngine.Random.Range(0.45f, 1.45f),
            randomSize * UnityEngine.Random.Range(0.45f, 1.25f)
        );

        Renderer fragmentRenderer = fragment.GetComponent<Renderer>();

        if (sourceMaterial != null)
        {
            fragmentRenderer.sharedMaterial = sourceMaterial;
        }

        Collider fragmentCollider = fragment.GetComponent<Collider>();
        if (fragmentCollider != null)
        {
            Destroy(fragmentCollider);
        }

        Vector3 outwardDirection =
            fragment.transform.position - rewardBounds.center;

        if (outwardDirection.sqrMagnitude < 0.001f)
        {
            outwardDirection = UnityEngine.Random.onUnitSphere;
        }
        else
        {
            outwardDirection.Normalize();
        }

        Vector3 velocity =
            outwardDirection * fragmentForce +
            Vector3.up * fragmentUpwardForce +
            UnityEngine.Random.insideUnitSphere *
            fragmentForce * 0.35f;

        Vector3 angularVelocity =
            UnityEngine.Random.insideUnitSphere *
            fragmentAngularSpeed;

        BoonRewardFragment fragmentEffect =
            fragment.AddComponent<BoonRewardFragment>();

        fragmentEffect.Initialize(
            velocity,
            angularVelocity,
            fragmentLifetime,
            fragmentFadeStartRatio,
            fragmentGravity,
            fragmentMoveDamping
        );
    }

    private Bounds CalculateRewardBounds()
    {
        bool hasBounds = false;
        Bounds combinedBounds = new Bounds(transform.position, Vector3.one);

        if (rewardRenderers != null)
        {
            for (int i = 0; i < rewardRenderers.Length; i++)
            {
                Renderer targetRenderer = rewardRenderers[i];

                if (targetRenderer == null ||
                    !targetRenderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(targetRenderer.bounds);
                }
            }
        }

        if (!hasBounds && triggerCollider != null)
        {
            combinedBounds = triggerCollider.bounds;
        }

        return combinedBounds;
    }

    private Material FindFragmentSourceMaterial()
    {
        if (fragmentMaterialOverride != null)
        {
            return fragmentMaterialOverride;
        }

        if (rewardRenderers == null)
        {
            return null;
        }

        for (int i = 0; i < rewardRenderers.Length; i++)
        {
            Renderer targetRenderer = rewardRenderers[i];

            if (targetRenderer == null)
            {
                continue;
            }

            Material[] materials = targetRenderer.sharedMaterials;

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

    private void CacheRewardRenderers()
    {
        rewardRenderers = GetComponentsInChildren<Renderer>(true);
        initialRendererStates = new bool[rewardRenderers.Length];

        for (int i = 0; i < rewardRenderers.Length; i++)
        {
            initialRendererStates[i] =
                rewardRenderers[i] != null &&
                rewardRenderers[i].enabled;
        }
    }

    private void RestoreRewardRenderers()
    {
        if (rewardRenderers == null ||
            initialRendererStates == null ||
            rewardRenderers.Length != initialRendererStates.Length)
        {
            CacheRewardRenderers();
        }

        for (int i = 0; i < rewardRenderers.Length; i++)
        {
            if (rewardRenderers[i] != null)
            {
                rewardRenderers[i].enabled = initialRendererStates[i];
            }
        }
    }

    private void SetRewardRenderersEnabled(bool value)
    {
        if (rewardRenderers == null)
        {
            return;
        }

        for (int i = 0; i < rewardRenderers.Length; i++)
        {
            if (rewardRenderers[i] != null)
            {
                rewardRenderers[i].enabled = value;
            }
        }
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
        if (!rewardReady || rewardUsed || other == null)
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

        PlayerInteraction interaction =
            player.GetComponent<PlayerInteraction>();

        if (interaction == null)
        {
            interaction =
                player.GetComponentInChildren<PlayerInteraction>(true);
        }

        if (boonInfo == null || interaction == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] Player에 BoonInfo와 " +
                "PlayerInteraction이 필요합니다.",
                player
            );

            return;
        }

        currentPlayer = player;
        currentBoonInfo = boonInfo;
        currentPlayerInteraction = interaction;

        currentPlayerInteraction.RegisterInteractable(this);

        if (showLogs)
        {
            Debug.Log(
                "[BoonRewardInteractable] 플레이어 등록 완료 / E키 상호작용 가능",
                this
            );
        }
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

        boonRewardUI = FindFirstObjectByType<BoonRewardUI>(
            FindObjectsInactive.Include
        );

        if (boonRewardUI == null)
        {
            Debug.LogError(
                "[BoonRewardInteractable] BoonRewardUI를 찾지 못했습니다.",
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

        BoonData[] loaded =
            Resources.LoadAll<BoonData>(resourcesPath);

        if (loaded != null && loaded.Length > 0)
        {
            allBoons.AddRange(loaded);
        }

        if (allBoons.Count == 0)
        {
            Debug.LogError(
                "[BoonRewardInteractable] BoonDatabase 또는 " +
                $"Resources/{resourcesPath}에 득도 데이터가 없습니다.",
                this
            );
        }
    }

    private void OnDisable()
    {
        rewardReady = false;
        selectionInProgress = false;
        completedCallback = null;

        ClearPlayerReference(true);
    }
}

/// <summary>
/// 코드로 생성된 조각 하나를 이동시키고 점점 사라지게 한다.
/// 원본 보상 오브젝트와 분리되어 있으므로 원본이 꺼져도 계속 실행된다.
/// </summary>
internal sealed class BoonRewardFragment : MonoBehaviour
{
    private Vector3 velocity;
    private Vector3 angularVelocity;
    private Vector3 originalScale;

    private float lifetime;
    private float fadeStartRatio;
    private float gravity;
    private float moveDamping;

    private Material runtimeMaterial;
    private string colorProperty;
    private Color originalColor;

    public void Initialize(
        Vector3 startVelocity,
        Vector3 startAngularVelocity,
        float effectLifetime,
        float effectFadeStartRatio,
        float effectGravity,
        float effectMoveDamping)
    {
        velocity = startVelocity;
        angularVelocity = startAngularVelocity;
        originalScale = transform.localScale;

        lifetime = Mathf.Max(0.05f, effectLifetime);
        fadeStartRatio = Mathf.Clamp(effectFadeStartRatio, 0f, 0.95f);
        gravity = Mathf.Max(0f, effectGravity);
        moveDamping = Mathf.Max(0f, effectMoveDamping);

        PrepareMaterial();

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < lifetime)
        {
            float deltaTime = Time.unscaledDeltaTime;
            elapsedTime += deltaTime;

            velocity += Vector3.down * gravity * deltaTime;
            velocity *= Mathf.Exp(-moveDamping * deltaTime);

            transform.position += velocity * deltaTime;
            transform.Rotate(
                angularVelocity * deltaTime,
                Space.Self
            );

            float lifeProgress =
                Mathf.Clamp01(elapsedTime / lifetime);

            float fadeProgress =
                Mathf.InverseLerp(
                    fadeStartRatio,
                    1f,
                    lifeProgress
                );

            float smoothFade =
                Mathf.SmoothStep(0f, 1f, fadeProgress);

            transform.localScale =
                originalScale *
                Mathf.Lerp(1f, 0.05f, smoothFade);

            SetAlpha(1f - smoothFade);

            yield return null;
        }

        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }

        Destroy(gameObject);
    }

    private void PrepareMaterial()
    {
        Renderer targetRenderer = GetComponent<Renderer>();

        if (targetRenderer == null)
        {
            return;
        }

        /*
         * renderer.material을 사용해 조각 전용 머티리얼 인스턴스를 만든다.
         * 원본 보상 머티리얼의 투명도가 변경되는 것을 막는다.
         */
        runtimeMaterial = targetRenderer.material;

        if (runtimeMaterial.HasProperty("_BaseColor"))
        {
            colorProperty = "_BaseColor";
        }
        else if (runtimeMaterial.HasProperty("_Color"))
        {
            colorProperty = "_Color";
        }

        if (!string.IsNullOrEmpty(colorProperty))
        {
            originalColor =
                runtimeMaterial.GetColor(colorProperty);
        }

        ConfigureTransparentMaterial(runtimeMaterial);
    }

    private void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        // URP Lit
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        }

        // Built-in Standard
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
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
            material.SetFloat("_ZWrite", 0f);
        }

        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetShaderPassEnabled("ShadowCaster", false);
    }

    private void SetAlpha(float alpha)
    {
        if (runtimeMaterial == null ||
            string.IsNullOrEmpty(colorProperty))
        {
            return;
        }

        Color color = originalColor;
        color.a = originalColor.a * Mathf.Clamp01(alpha);

        runtimeMaterial.SetColor(colorProperty, color);
    }
}
