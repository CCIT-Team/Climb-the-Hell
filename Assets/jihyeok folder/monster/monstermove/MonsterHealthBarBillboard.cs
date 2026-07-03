using UnityEngine;

/// <summary>
/// 월드 스페이스 HP바가 항상 플레이어 카메라를 바라보게 한다.
/// 위치는 부모인 DamageNumberPoint 또는 Head를 자동으로 따라간다.
/// </summary>
public class MonsterHealthBarBillboard : MonoBehaviour
{
    [Header("바라볼 카메라")]
    [Tooltip("비워두면 Main Camera를 자동으로 찾는다.")]
    [SerializeField]
    private Camera targetCamera;

    [Header("방향 보정")]
    [Tooltip("HP바가 뒤집혀 보이면 체크한다.")]
    [SerializeField]
    private bool reverseDirection;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        // 카메라와 같은 회전값을 사용해서 정면을 맞춘다.
        transform.rotation =
            targetCamera.transform.rotation;

        if (reverseDirection)
        {
            transform.rotation *=
                Quaternion.Euler(
                    0f,
                    180f,
                    0f
                );
        }
    }
}