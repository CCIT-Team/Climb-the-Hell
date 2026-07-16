using UnityEngine;

/// <summary>
/// 쿼터뷰 카메라.
/// target이 비어 있으면 자동으로 Player를 찾는다.
/// PlayerSceneMover, GameManager, Player 태그, Player 컴포넌트, PlayerController 순서로 찾는다.
/// </summary>
public class QuarterViewCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    private Transform target;

    [Header("카메라 설정")]
    [SerializeField]
    private float distance = 7f;

    [SerializeField]
    private float horizontalAngle = 45f;

    [SerializeField]
    private float verticalAngle = 50f;

    [Header("자동 탐색")]
    [SerializeField]
    private bool autoFindPlayer = true;

    [SerializeField]
    private float retryInterval = 0.25f;

    [Header("디버그")]
    [SerializeField]
    private bool showWarning = true;

    private Vector3 offset;
    private float retryTimer;

    private void Awake()
    {
        CalculateOffset();
        TryFindPlayerTarget();
    }

    private void LateUpdate()
    {
        if (target == null && autoFindPlayer)
        {
            retryTimer -= Time.deltaTime;

            if (retryTimer <= 0f)
            {
                retryTimer = retryInterval;
                TryFindPlayerTarget();
            }
        }

        if (target == null)
        {
            return;
        }

        transform.position =
            target.position + offset;

        transform.rotation =
            Quaternion.Euler(
                verticalAngle,
                horizontalAngle,
                0f
            );
    }

    private void CalculateOffset()
    {
        Quaternion rotation =
            Quaternion.Euler(
                verticalAngle,
                horizontalAngle,
                0f
            );

        offset =
            rotation *
            new Vector3(
                0f,
                0f,
                -distance
            );
    }

    private void TryFindPlayerTarget()
    {
        if (target != null)
        {
            return;
        }

        if (PlayerSceneMover.Instance != null &&
            PlayerSceneMover.Instance.CurrentPlayer != null)
        {
            target =
                PlayerSceneMover.Instance
                    .CurrentPlayer
                    .transform;

            if (showWarning)
            {
                Debug.Log(
                    "[QuarterViewCamera] PlayerSceneMover.CurrentPlayer를 Target으로 설정했습니다.",
                    this
                );
            }

            return;
        }

        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentPlayer != null)
        {
            target =
                GameManager.Instance
                    .CurrentPlayer
                    .transform;

            if (showWarning)
            {
                Debug.Log(
                    "[QuarterViewCamera] GameManager.CurrentPlayer를 Target으로 설정했습니다.",
                    this
                );
            }

            return;
        }

        GameObject taggedPlayer = null;

        try
        {
            taggedPlayer =
                GameObject.FindGameObjectWithTag("Player");
        }
        catch
        {
            taggedPlayer = null;
        }

        if (taggedPlayer != null)
        {
            target = taggedPlayer.transform;

            if (showWarning)
            {
                Debug.Log(
                    "[QuarterViewCamera] Player 태그 오브젝트를 Target으로 설정했습니다.",
                    this
                );
            }

            return;
        }

        Player[] players =
            FindObjectsOfType<Player>(true);

        if (players != null &&
            players.Length > 0 &&
            players[0] != null)
        {
            target =
                players[0].transform;

            if (showWarning)
            {
                Debug.Log(
                    "[QuarterViewCamera] Player 컴포넌트를 Target으로 설정했습니다.",
                    this
                );
            }

            return;
        }

        PlayerController[] controllers =
            FindObjectsOfType<PlayerController>(true);

        if (controllers != null &&
            controllers.Length > 0 &&
            controllers[0] != null)
        {
            target =
                controllers[0].transform;

            if (showWarning)
            {
                Debug.Log(
                    "[QuarterViewCamera] PlayerController를 Target으로 설정했습니다.",
                    this
                );
            }

            return;
        }

        if (showWarning)
        {
            Debug.LogWarning(
                "[QuarterViewCamera] Player를 찾지 못했습니다.",
                this
            );
        }
    }

    private void OnValidate()
    {
        CalculateOffset();
    }
}
