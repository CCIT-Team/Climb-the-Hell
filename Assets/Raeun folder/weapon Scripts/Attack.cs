using UnityEngine;

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
        playerController =
            GetComponent<PlayerController>();

        FindCamera();

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
        HandleAttackInput();
    }

    private void FindCamera()
    {
        if (mainCamera != null)
        {
            return;
        }

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
        if (currentWeapon == null)
        {
            return;
        }

        // 마우스 왼쪽 버튼: 기본 공격
        if (Input.GetMouseButtonDown(0))
        {
            FaceMouseDirection();
            currentWeapon.Use();
        }

        // 마우스 오른쪽 버튼: 근접 무기 특수 공격
        if (Input.GetMouseButtonDown(1) &&
            currentWeapon is MeleeWeapon meleeWeapon)
        {
            FaceMouseDirection();
            meleeWeapon.SpecialUse();
        }
    }

    private void FaceMouseDirection()
    {
        if (mainCamera == null ||
            playerController == null)
        {
            return;
        }

        Ray mouseRay =
            mainCamera.ScreenPointToRay(
                Input.mousePosition
            );

        Plane groundPlane =
            new Plane(
                Vector3.up,
                transform.position
            );

        if (!groundPlane.Raycast(
            mouseRay,
            out float hitDistance
        ))
        {
            return;
        }

        Vector3 mouseWorldPosition =
            mouseRay.GetPoint(hitDistance);

        Vector3 attackDirection =
            mouseWorldPosition -
            transform.position;

        attackDirection.y = 0f;

        if (attackDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        playerController.FaceDirection(
            attackDirection
        );
    }

    public void SetWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;
    }

    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }
}