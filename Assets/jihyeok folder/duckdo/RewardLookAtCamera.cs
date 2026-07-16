using UnityEngine;

/// <summary>
/// 보상 또는 문 위 목적지 프리팹을
/// 카메라 화면과 평행한 방향으로 유지한다.
///
/// 위치는 변경하지 않고 회전만 변경한다.
/// </summary>
public class RewardLookAtCamera : MonoBehaviour
{
    [Header("기준 카메라")]
    [SerializeField] private Camera targetCamera;

    [Header("모델 방향 보정")]
    [SerializeField] private Vector3 rotationOffset =
        new Vector3(90f, 0f, 0f);

    [Tooltip("앞뒤가 반대일 때 체크")]
    [SerializeField] private bool reverseDirection;

    [Tooltip("카메라 회전이 바뀌었을 때만 갱신")]
    [SerializeField] private bool followCameraRotation = true;

    private Quaternion previousCameraRotation;

    private void Awake()
    {
        ResolveCamera();
    }

    private void Start()
    {
        ApplyRotation();
    }

    private void LateUpdate()
    {
        if (!followCameraRotation)
        {
            return;
        }

        ResolveCamera();

        if (targetCamera == null)
        {
            return;
        }

        if (Quaternion.Angle(
                previousCameraRotation,
                targetCamera.transform.rotation
            ) < 0.01f)
        {
            return;
        }

        ApplyRotation();
    }

    private void ResolveCamera()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void ApplyRotation()
    {
        ResolveCamera();

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

        transform.rotation =
            screenFacingRotation *
            Quaternion.Euler(
                rotationOffset
            );

        previousCameraRotation =
            targetCamera.transform.rotation;
    }
}
