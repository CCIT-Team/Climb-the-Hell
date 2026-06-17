using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerAttackReflect : MonoBehaviour
{
    [Header("Attack Input")]
    [Tooltip("기존 플레이어 공격과 같은 키")]
    public KeyCode attackKey = KeyCode.Mouse0;

    [Header("Reflect Timing")]
    [Tooltip("공격 버튼을 누른 뒤 반사 판정 유지 시간")]
    public float reflectDuration = 0.25f;

    [Header("Temporary Reflect Collider")]
    [Tooltip("플레이어 기준 반사 범위 위치")]
    public Vector3 colliderCenter = new Vector3(0f, 1f, 0f);

    [Tooltip("자동 생성 SphereCollider 반지름")]
    public float colliderRadius = 1.2f;

    [Tooltip("Reflect 레이어 이름")]
    public string reflectLayerName = "Reflect";

    [Header("Debug")]
    public bool showDebugLog = true;
    public bool drawGizmo = true;

    public bool IsReflecting { get; private set; }

    private GameObject reflectObject;
    private SphereCollider reflectCollider;
    private Coroutine reflectCoroutine;

    private void Update()
    {
        bool attackPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame
        )
        {
            attackPressed = true;
        }
#else
        if (Input.GetKeyDown(attackKey))
        {
            attackPressed = true;
        }
#endif

        if (attackPressed)
        {
            StartReflect();
        }
    }

    private void StartReflect()
    {
        if (reflectCoroutine != null)
        {
            StopCoroutine(reflectCoroutine);
            RemoveReflectCollider();
        }

        reflectCoroutine = StartCoroutine(
            ReflectRoutine()
        );
    }

    private IEnumerator ReflectRoutine()
    {
        CreateReflectCollider();

        IsReflecting = true;

        if (showDebugLog)
        {
            Debug.Log("반사 판정 시작");
        }

        yield return new WaitForSeconds(
            reflectDuration
        );

        IsReflecting = false;

        RemoveReflectCollider();

        if (showDebugLog)
        {
            Debug.Log("반사 판정 종료");
        }

        reflectCoroutine = null;
    }

    private void CreateReflectCollider()
    {
        RemoveReflectCollider();

        reflectObject = new GameObject(
            "TemporaryReflectCollider"
        );

        reflectObject.transform.SetParent(
            transform
        );

        reflectObject.transform.localPosition =
            colliderCenter;

        reflectObject.transform.localRotation =
            Quaternion.identity;

        reflectObject.transform.localScale =
            Vector3.one;

        int reflectLayer = LayerMask.NameToLayer(
            reflectLayerName
        );

        if (reflectLayer >= 0)
        {
            reflectObject.layer = reflectLayer;
        }
        else
        {
            reflectObject.layer = gameObject.layer;

            Debug.LogWarning(
                $"'{reflectLayerName}' 레이어가 없습니다."
            );
        }

        reflectCollider =
            reflectObject.AddComponent<SphereCollider>();

        reflectCollider.center = Vector3.zero;
        reflectCollider.radius = colliderRadius;
        reflectCollider.isTrigger = true;
    }

    private void RemoveReflectCollider()
    {
        if (reflectObject != null)
        {
            Destroy(reflectObject);
        }

        reflectObject = null;
        reflectCollider = null;
    }

    public bool IsReflectCollider(
        Collider targetCollider
    )
    {
        return
            IsReflecting &&
            reflectCollider != null &&
            targetCollider == reflectCollider;
    }

    private void OnDisable()
    {
        IsReflecting = false;

        RemoveReflectCollider();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.TransformPoint(
                colliderCenter
            ),
            colliderRadius
        );
    }
}