using UnityEngine;
using UnityEngine.EventSystems; // UI 위에 마우스가 있는지 확인하기 위해 사용

[RequireComponent(typeof(PlayerController))]
public class Attack : MonoBehaviour
{
    [Header("무기")]
    [SerializeField] private Weapon currentWeapon;

    [Header("카메라")]
    [SerializeField] private Camera mainCamera;

    private PlayerController playerController;

    private void Awake()
    {
        // 같은 오브젝트의 PlayerController 가져오기
        playerController = GetComponent<PlayerController>();

        // 메인 카메라 찾기
        FindCamera();

        // 무기가 연결되지 않았으면 경고 출력
        if (currentWeapon == null)
        {
            Debug.LogError(
                "[Attack] Current Weapon에 무기를 연결하세요.",
                this
            );
        }
    }

    private void Update()
    {
        // 매 프레임 공격 입력 확인
        HandleAttackInput();
    }

    private void FindCamera()
    {
        // 이미 연결되어 있으면 다시 찾지 않음
        if (mainCamera != null)
        {
            return;
        }

        // MainCamera 태그가 붙은 카메라 찾기
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError(
                "[Attack] MainCamera를 찾을 수 없습니다.",
                this
            );
        }
    }

    private void HandleAttackInput()
    {
        // 무기가 없으면 공격 불가
        if (currentWeapon == null)
        {
            return;
        }

        // 마우스 왼쪽 클릭
        if (Input.GetMouseButtonDown(0))
        {
            // 마우스가 UI 위에 있다면 공격하지 않음
            if (EventSystem.current != null &&
                EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            // 공격 전에 플레이어를 마우스 방향으로 회전
            FaceMouseDirection();

            // 기본 공격 실행
            currentWeapon.Use();
        }

        // 마우스 오른쪽 클릭 + 현재 무기가 근접 무기인 경우
        if (Input.GetMouseButtonDown(1) &&
            currentWeapon is MeleeWeapon meleeWeapon)
        {
            // 플레이어를 마우스 방향으로 회전
            FaceMouseDirection();

            // 근접 무기의 특수 공격 실행
            meleeWeapon.SpecialUse();
        }
    }

    private void FaceMouseDirection()
    {
        // 카메라나 플레이어 컨트롤러가 없으면 종료
        if (mainCamera == null)
        {
            FindCamera();
        }

        if (mainCamera == null ||
            playerController == null)
        {
            return;
        }

        // 화면상의 마우스 위치에서 Ray 생성
        Ray mouseRay =
            mainCamera.ScreenPointToRay(Input.mousePosition);

        // 플레이어 높이에 해당하는 바닥 평면 생성
        Plane groundPlane =
            new Plane(Vector3.up, transform.position);

        // Ray와 바닥이 만나는 지점 계산
        if (!groundPlane.Raycast(
            mouseRay,
            out float hitDistance))
        {
            return;
        }

        // 마우스의 월드 좌표
        Vector3 mouseWorldPosition =
            mouseRay.GetPoint(hitDistance);

        // 플레이어 → 마우스 방향 계산
        Vector3 attackDirection =
            mouseWorldPosition - transform.position;

        // 수평 방향만 사용
        attackDirection.y = 0f;

        // 방향이 너무 짧으면 회전하지 않음
        if (attackDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        // 플레이어를 해당 방향으로 회전
        playerController.FaceDirection(
            attackDirection
        );
    }

    // 무기 교체
    public void SetWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;
    }

    // 현재 장착한 무기 반환
    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }
}
