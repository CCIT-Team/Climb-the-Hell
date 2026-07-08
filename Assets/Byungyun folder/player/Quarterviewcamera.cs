using UnityEngine;

public class QuarterViewCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("자동 타겟 찾기")]
    [SerializeField] private string playerTag = "Player";

    [Header("카메라 설정")]
    public float distance = 7f;
    public float horizontalAngle = 45f;
    public float verticalAngle = 50f;

    private Vector3 _offset;

    private void Awake()
    {
        CalculateOffset();
        TryFindPlayerTarget();
    }

    private void Start()
    {
        // 플레이어가 Awake 이후에 생성되는 경우 대비
        TryFindPlayerTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            TryFindPlayerTarget();

            if (target == null)
            {
                return;
            }
        }

        transform.position = target.position + _offset;

        // rotation을 Euler로 완전 고정
        // LookAt 사용 안 함
        transform.rotation = Quaternion.Euler(
            verticalAngle,
            horizontalAngle,
            0f
        );
    }

    private void CalculateOffset()
    {
        Quaternion rot = Quaternion.Euler(
            verticalAngle,
            horizontalAngle,
            0f
        );

        _offset = rot * new Vector3(
            0f,
            0f,
            -distance
        );
    }

    private void TryFindPlayerTarget()
    {
        // 이미 인스펙터에서 넣었으면 자동 변경하지 않음
        if (target != null)
        {
            return;
        }

        GameObject playerObject = null;

        try
        {
            playerObject = GameObject.FindGameObjectWithTag(playerTag);
        }
        catch
        {
            Debug.LogWarning(
                $"[QuarterViewCamera] '{playerTag}' 태그가 프로젝트에 없습니다. " +
                "Player 오브젝트에 태그를 만들고 지정하세요.",
                this
            );

            return;
        }

        if (playerObject == null)
        {
            Debug.LogWarning(
                $"[QuarterViewCamera] '{playerTag}' 태그를 가진 오브젝트를 찾지 못했습니다.",
                this
            );

            return;
        }

        target = playerObject.transform;

        Debug.Log(
            $"[QuarterViewCamera] 자동 타겟 설정 완료: {target.name}",
            target
        );
    }

    private void OnValidate()
    {
        CalculateOffset();
    }
}