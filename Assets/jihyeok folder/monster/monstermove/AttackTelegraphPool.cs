using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 모든 몬스터가 공유하는 공격 범위 표시 풀.
///
/// 예고 상태:
/// 반투명 빨간색으로 깜빡인다.
///
/// 실제 공격 상태:
/// 진한 빨간색으로 변경된다.
///
/// 공용 Mesh 2개와 공용 Material 2개만 사용한다.
/// 몬스터마다 Material이나 범위 오브젝트를 만들지 않는다.
/// </summary>
public sealed class AttackTelegraphPool : MonoBehaviour
{
    public sealed class Handle
    {
        internal AttackTelegraphPool owner;
        internal GameObject gameObject;
        internal MeshFilter meshFilter;
        internal MeshRenderer meshRenderer;

        internal bool isRented;
        internal bool isPreview;

        public bool IsValid
        {
            get
            {
                return owner != null &&
                       gameObject != null &&
                       isRented;
            }
        }
    }

    private static AttackTelegraphPool instance;

    public static AttackTelegraphPool Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject poolObject =
                    new GameObject("[AttackTelegraphPool]");

                instance =
                    poolObject.AddComponent<AttackTelegraphPool>();

                DontDestroyOnLoad(poolObject);
            }

            return instance;
        }
    }

    [Header("풀 설정")]
    [Tooltip("동시에 공격을 예고할 것으로 예상되는 최대 몬스터 수")]
    [Min(1)]
    [SerializeField]
    private int initialPoolSize = 16;

    [Header("원형 범위 품질")]
    [Range(12, 96)]
    [SerializeField]
    private int circleSegments = 40;

    [Header("공격 예고 색상")]
    [SerializeField]
    private Color previewColor =
        new Color(1f, 0f, 0f, 0.5f);

    [Range(0f, 1f)]
    [SerializeField]
    private float previewMinimumAlpha = 0.22f;

    [Range(0f, 1f)]
    [SerializeField]
    private float previewMaximumAlpha = 0.55f;

    [Min(0f)]
    [SerializeField]
    private float previewPulseSpeed = 7f;

    [Header("실제 공격 색상")]
    [SerializeField]
    private Color impactColor =
        new Color(0.9f, 0f, 0f, 0.92f);

    private readonly Queue<Handle> availableHandles =
        new Queue<Handle>();

    private readonly List<Handle> allHandles =
        new List<Handle>();

    private Material previewMaterial;
    private Material impactMaterial;

    private Mesh circleMesh;
    private Mesh boxMesh;

    private int activeCount;
    private int previewCount;

    private bool isInitialized;

    private static readonly int BaseColorId =
        Shader.PropertyToID("_BaseColor");

    private static readonly int ColorId =
        Shader.PropertyToID("_Color");

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Update()
    {
        if (previewCount <= 0 ||
            previewMaterial == null)
        {
            return;
        }

        float pulse =
            (Mathf.Sin(
                Time.unscaledTime * previewPulseSpeed
            ) + 1f) * 0.5f;

        float alpha =
            Mathf.Lerp(
                previewMinimumAlpha,
                previewMaximumAlpha,
                pulse
            );

        Color color = previewColor;
        color.a = alpha;

        SetMaterialColor(
            previewMaterial,
            color
        );
    }

    public Handle RentCircle(
        Vector3 center,
        float radius
    )
    {
        Initialize();

        Handle handle = RentHandle();

        handle.meshFilter.sharedMesh =
            circleMesh;

        handle.gameObject.transform
            .SetPositionAndRotation(
                center,
                Quaternion.identity
            );

        handle.gameObject.transform.localScale =
            new Vector3(
                radius,
                1f,
                radius
            );

        return handle;
    }

    public Handle RentBox(
        Vector3 startPosition,
        Vector3 direction,
        float width,
        float length
    )
    {
        Initialize();

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = Vector3.forward;
        }

        direction.Normalize();

        Vector3 center =
            startPosition +
            direction * (length * 0.5f);

        Handle handle = RentHandle();

        handle.meshFilter.sharedMesh =
            boxMesh;

        handle.gameObject.transform
            .SetPositionAndRotation(
                center,
                Quaternion.LookRotation(direction)
            );

        handle.gameObject.transform.localScale =
            new Vector3(
                width,
                1f,
                length
            );

        return handle;
    }

    public void ShowImpact(Handle handle)
    {
        if (handle == null ||
            handle.owner != this ||
            !handle.isRented)
        {
            return;
        }

        if (handle.isPreview)
        {
            handle.isPreview = false;

            previewCount =
                Mathf.Max(0, previewCount - 1);
        }

        if (handle.meshRenderer != null)
        {
            handle.meshRenderer.sharedMaterial =
                impactMaterial;
        }
    }

    public void Release(Handle handle)
    {
        if (handle == null ||
            handle.owner != this ||
            !handle.isRented)
        {
            return;
        }

        if (handle.isPreview)
        {
            previewCount =
                Mathf.Max(0, previewCount - 1);
        }

        handle.isPreview = false;
        handle.isRented = false;

        if (handle.gameObject != null)
        {
            handle.gameObject.SetActive(false);
            handle.gameObject.transform.SetParent(
                transform
            );
        }

        activeCount =
            Mathf.Max(0, activeCount - 1);

        availableHandles.Enqueue(handle);
    }

    private void Initialize()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;

        circleMesh =
            CreateCircleMesh(circleSegments);

        boxMesh =
            CreateBoxMesh();

        previewMaterial =
            CreateTransparentMaterial(
                "Shared_AttackPreview_Material",
                previewColor
            );

        impactMaterial =
            CreateTransparentMaterial(
                "Shared_AttackImpact_Material",
                impactColor
            );

        SetMaterialColor(
            impactMaterial,
            impactColor
        );

        for (int i = 0;
             i < initialPoolSize;
             i++)
        {
            Handle handle = CreateHandle();

            availableHandles.Enqueue(handle);
        }
    }

    private Handle RentHandle()
    {
        Handle handle;

        if (availableHandles.Count > 0)
        {
            handle =
                availableHandles.Dequeue();
        }
        else
        {
            handle = CreateHandle();
        }

        handle.isRented = true;
        handle.isPreview = true;

        handle.meshRenderer.sharedMaterial =
            previewMaterial;

        handle.gameObject.SetActive(true);

        activeCount++;
        previewCount++;

        return handle;
    }

    private Handle CreateHandle()
    {
        GameObject visualObject =
            new GameObject(
                $"AttackTelegraph_{allHandles.Count}"
            );

        visualObject.transform.SetParent(
            transform
        );

        MeshFilter meshFilter =
            visualObject.AddComponent<MeshFilter>();

        MeshRenderer meshRenderer =
            visualObject.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterial =
            previewMaterial;

        meshRenderer.shadowCastingMode =
            ShadowCastingMode.Off;

        meshRenderer.receiveShadows = false;

        meshRenderer.lightProbeUsage =
            LightProbeUsage.Off;

        meshRenderer.reflectionProbeUsage =
            ReflectionProbeUsage.Off;

        visualObject.SetActive(false);

        Handle handle =
            new Handle
            {
                owner = this,
                gameObject = visualObject,
                meshFilter = meshFilter,
                meshRenderer = meshRenderer,
                isRented = false,
                isPreview = false
            };

        allHandles.Add(handle);

        return handle;
    }

    private static Material CreateTransparentMaterial(
        string materialName,
        Color color
    )
    {
        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader == null)
        {
            shader =
                Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader =
                Shader.Find("Standard");
        }

        if (shader == null)
        {
            Debug.LogError(
                "[AttackTelegraphPool] " +
                "사용 가능한 Shader를 찾지 못했습니다."
            );

            return null;
        }

        Material material =
            new Material(shader);

        material.name = materialName;

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);

            material.SetFloat(
                "_SrcBlend",
                (float)BlendMode.SrcAlpha
            );

            material.SetFloat(
                "_DstBlend",
                (float)BlendMode.OneMinusSrcAlpha
            );

            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);

            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );

            material.DisableKeyword(
                "_ALPHATEST_ON"
            );

            material.renderQueue =
                (int)RenderQueue.Transparent;
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);

            material.SetInt(
                "_SrcBlend",
                (int)BlendMode.SrcAlpha
            );

            material.SetInt(
                "_DstBlend",
                (int)BlendMode.OneMinusSrcAlpha
            );

            material.SetInt("_ZWrite", 0);

            material.DisableKeyword(
                "_ALPHATEST_ON"
            );

            material.EnableKeyword(
                "_ALPHABLEND_ON"
            );

            material.DisableKeyword(
                "_ALPHAPREMULTIPLY_ON"
            );

            material.renderQueue =
                (int)RenderQueue.Transparent;
        }

        SetMaterialColor(
            material,
            color
        );

        return material;
    }

    private static Mesh CreateCircleMesh(
        int segments
    )
    {
        Mesh mesh =
            new Mesh
            {
                name =
                    "Shared_AttackTelegraph_Circle"
            };

        Vector3[] vertices =
            new Vector3[segments + 1];

        Vector2[] uv =
            new Vector2[segments + 1];

        int[] triangles =
            new int[segments * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0;
             i < segments;
             i++)
        {
            float angle =
                Mathf.PI * 2f * i /
                segments;

            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);

            vertices[i + 1] =
                new Vector3(x, 0f, z);

            uv[i + 1] =
                new Vector2(
                    x * 0.5f + 0.5f,
                    z * 0.5f + 0.5f
                );
        }

        for (int i = 0;
             i < segments;
             i++)
        {
            int triangleIndex = i * 3;

            int current = i + 1;
            int next =
                ((i + 1) % segments) + 1;

            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] =
                next;
            triangles[triangleIndex + 2] =
                current;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static Mesh CreateBoxMesh()
    {
        Mesh mesh =
            new Mesh
            {
                name =
                    "Shared_AttackTelegraph_Box"
            };

        mesh.vertices =
            new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3( 0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f,  0.5f),
                new Vector3( 0.5f, 0f,  0.5f)
            };

        mesh.uv =
            new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };

        mesh.triangles =
            new[]
            {
                0, 2, 1,
                1, 2, 3
            };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private static void SetMaterialColor(
        Material material,
        Color color
    )
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty(BaseColorId))
        {
            material.SetColor(
                BaseColorId,
                color
            );
        }

        if (material.HasProperty(ColorId))
        {
            material.SetColor(
                ColorId,
                color
            );
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (circleMesh != null)
        {
            Destroy(circleMesh);
        }

        if (boxMesh != null)
        {
            Destroy(boxMesh);
        }

        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
        }

        if (impactMaterial != null)
        {
            Destroy(impactMaterial);
        }
    }
}
