using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Player 컴포넌트가 반드시 있어야 동작
[RequireComponent(typeof(Player))]

public class Attack : MonoBehaviour
{
    [SerializeField] private Weapon currentWeapon;

    private Player player;
    private Camera mainCamera; // ← 카메라 캐싱용 변수 추가

    private void Awake()
    {
        // Player 컴포넌트 가져오기
        player = GetComponent<Player>();

        // 카메라를 Awake에서 한 번만 찾아서 저장
        mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogError("MainCamera를 찾을 수 없습니다! 카메라에 'MainCamera' 태그가 있는지 확인하세요.");
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // 마우스 방향으로 플레이어 회전
        RotateBoxToMouse();

        // 좌클릭 시 공격
        if (Input.GetMouseButtonDown(0))
        {
            currentWeapon.Use();
        }
    }

    // 플레이어를 마우스 방향으로 회전
    private void RotateBoxToMouse()
    {
        // Camera.main 대신 캐싱된 mainCamera 사용
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Plane groundPlane = new Plane(
            Vector3.up,
            new Vector3(0f, transform.position.y, 0f)
        );

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(distance);
            Vector3 dir = mouseWorldPos - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(dir.normalized);
            }
        }
    }

}
