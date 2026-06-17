using UnityEngine;

public class PerformanceDisplay : MonoBehaviour
{
    private float currentFPS;
    private float averageFPS;

    private float timer;
    private int frameCount;

    private float totalFPS;
    private int fpsSampleCount;

    public float refreshRate = 0.5f;

    void Start()
    {
        // FPS 제한 해제
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
    }

    void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= refreshRate)
        {
            currentFPS =
                frameCount / timer;

            totalFPS += currentFPS;
            fpsSampleCount++;

            averageFPS =
                totalFPS / fpsSampleCount;

            frameCount = 0;
            timer = 0f;
        }
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();

        style.fontSize = 30;
        style.normal.textColor = Color.white;

        GUI.Box(
            new Rect(10, 10, 300, 120),
            "");

        GUI.Label(
            new Rect(25, 25, 260, 100),

            "Current FPS : " +
            Mathf.RoundToInt(currentFPS) +

            "\nAverage FPS : " +
            Mathf.RoundToInt(averageFPS),

            style);
    }
}