using UnityEngine;

/// <summary>
/// 보상 오브젝트를 화면과 평행한 방향으로 유지한다.
///
/// 카메라 위치는 사용하지 않고 카메라 회전만 사용하므로,
/// 보상이 화면 좌우에 있어도 제각각 꺾이지 않는다.
///
/// 위치는 전혀 변경하지 않는다.
/// </summary>
public class RewardLookAtCamera : MonoBehaviour
{
    [Header("기준 카메라")]
    [SerializeField] private Camera targetCamera;

    [Header("원판 방향 보정")]
    [Tooltip("Unity 기본 Cylinder의 둥근 면을 카메라 쪽으로 돌리는 값")]
    [SerializeField] private Vector3 rotationOffset =
        new Vector3(90f, 0f, 0f);

    [Tooltip("앞뒤가 반대일 때 체크")]
    [SerializeField] private bool reverseDirection;

    [Header("카메라 회전 대응")]
    [Tooltip("카메라 각도가 바뀔 때만 보상 각도도 갱신")]
    [SerializeField] private bool followCameraRotation = true;

    private Quaternion previousCameraRotation;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        ApplyRotation();
    }

    private void LateUpdate()
    {
        if (!followCameraRotation || targetCamera == null)
        {
            return;
        }

        // 카메라 위치 이동은 무시하고,
        // 카메라 회전이 바뀐 경우에만 갱신한다.
        if (Quaternion.Angle(
                previousCameraRotation,
                targetCamera.transform.rotation
            ) < 0.01f)
        {
            return;
        }

        ApplyRotation();
    }

    private void ApplyRotation()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 facingDirection =
            reverseDirection
                ? targetCamera.transform.forward
                : -targetCamera.transform.forward;

        Quaternion screenFacingRotation =
            Quaternion.LookRotation(
                facingDirection,
                targetCamera.transform.up
            );

        // 위치는 건드리지 않고 회전값만 적용
        transform.rotation =
            screenFacingRotation *
            Quaternion.Euler(rotationOffset);

        previousCameraRotation =
            targetCamera.transform.rotation;
    }
}