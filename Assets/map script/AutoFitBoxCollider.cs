using UnityEngine;

public class AutoFitBoxCollider : MonoBehaviour
{
    [ContextMenu("Fit BoxCollider To Mesh")]
    public void FitBoxColliderToMesh()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("Renderer가 없음");
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider box = GetComponent<BoxCollider>();

        if (box == null)
            box = gameObject.AddComponent<BoxCollider>();

        box.center = transform.InverseTransformPoint(bounds.center);

        Vector3 localMin = transform.InverseTransformPoint(bounds.min);
        Vector3 localMax = transform.InverseTransformPoint(bounds.max);

        box.size = new Vector3(
            Mathf.Abs(localMax.x - localMin.x),
            Mathf.Abs(localMax.y - localMin.y),
            Mathf.Abs(localMax.z - localMin.z)
        );

        Debug.Log(gameObject.name + " BoxCollider 자동 조정 완료");
    }
}