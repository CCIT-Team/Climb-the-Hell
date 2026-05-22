using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("카메라 설정")]
    public float distance = 7f;
    public float horizontalAngle = 45f;
    public float verticalAngle = 50f;

    private Vector3 _offset;

    void Start()
    {
        CalculateOffset();
    }

    void LateUpdate()
    {
        if (target == null) return;

        transform.position = target.position + _offset;

        // rotation을 Euler로 완전 고정 (LookAt 절대 사용 안 함)
        transform.rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
    }

    void CalculateOffset()
    {
        // 카메라 rotation 기준으로 뒤+위 방향 오프셋 계산
        Quaternion rot = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
        _offset = rot * new Vector3(0f, 0f, -distance);
    }

    void OnValidate()
    {
        CalculateOffset();
    }
}