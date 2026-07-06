using UnityEngine;

public class WarningCircle : MonoBehaviour
{
    public float duration = 1f;

    private Renderer rend;
    private float timer;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        Debug.Log("WarningCircle created");

        if (rend == null)
        {
            Debug.LogError("WarningCircle: Renderer 없음! 프리팹 확인해라");
        }
    }

    public void Init(float time)
    {
        duration = time;
        timer = 0f;
    }

    private void Update()
    {
        if (rend == null) return; // 🔥 핵심 방어

        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);

        rend.material.color = Color.Lerp(Color.green, Color.red, t);
    }
}