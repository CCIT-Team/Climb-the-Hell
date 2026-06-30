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

    [Header("Reflect Plane")]
    [Tooltip("플레이어 로컬 좌표 기준 반사 면 중심")]
    public Vector3 reflectCenter =
        new Vector3(0f, 1f, 0.7f);

    [Tooltip("반사 면의 가로 길이")]
    public float reflectWidth = 2.4f;

    [Tooltip("반사 면의 세로 길이")]
    public float reflectHeight = 2.4f;

    [Header("Debug")]
    public bool showDebugLog = true;
    public bool drawGizmo = true;

    public bool IsReflecting
    {
        get;
        private set;
    }

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
        }

        reflectCoroutine =
            StartCoroutine(ReflectRoutine());
    }

    private IEnumerator ReflectRoutine()
    {
        IsReflecting = true;

        if (showDebugLog)
        {
            Debug.Log("반사 판정 시작");
        }

        yield return new WaitForSeconds(
            reflectDuration
        );

        IsReflecting = false;

        if (showDebugLog)
        {
            Debug.Log("반사 판정 종료");
        }

        reflectCoroutine = null;
    }

    /*
     * 반사 사각형의 월드 좌표 꼭짓점 반환
     *
     * v3 -------- v2
     * |         / |
     * |      /    |
     * |   /       |
     * v0 -------- v1
     *
     * 삼각형 1: v0, v1, v2
     * 삼각형 2: v0, v2, v3
     */
    public void GetReflectVertices(
        out Vector3 v0,
        out Vector3 v1,
        out Vector3 v2,
        out Vector3 v3
    )
    {
        Vector3 center =
            transform.TransformPoint(
                reflectCenter
            );

        Vector3 right =
            transform.right.normalized;

        Vector3 up =
            transform.up.normalized;

        Vector3 scale =
            transform.lossyScale;

        float halfWidth =
            reflectWidth *
            Mathf.Abs(scale.x) *
            0.5f;

        float halfHeight =
            reflectHeight *
            Mathf.Abs(scale.y) *
            0.5f;

        v0 =
            center -
            right * halfWidth -
            up * halfHeight;

        v1 =
            center +
            right * halfWidth -
            up * halfHeight;

        v2 =
            center +
            right * halfWidth +
            up * halfHeight;

        v3 =
            center -
            right * halfWidth +
            up * halfHeight;
    }

    private void OnDisable()
    {
        IsReflecting = false;

        if (reflectCoroutine != null)
        {
            StopCoroutine(reflectCoroutine);
            reflectCoroutine = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo)
        {
            return;
        }

        GetReflectVertices(
            out Vector3 v0,
            out Vector3 v1,
            out Vector3 v2,
            out Vector3 v3
        );

        Gizmos.color =
            IsReflecting
                ? Color.green
                : Color.cyan;

        Gizmos.DrawLine(v0, v1);
        Gizmos.DrawLine(v1, v2);
        Gizmos.DrawLine(v2, v3);
        Gizmos.DrawLine(v3, v0);

        // 사각형을 삼각형 두 개로 나누는 선
        Gizmos.DrawLine(v0, v2);
    }
}