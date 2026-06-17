using UnityEngine;

public class MapUIController : MonoBehaviour
{
    public GameObject mapPanel;

    private void Start()
    {
        if (mapPanel != null)
            mapPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (mapPanel != null)
                mapPanel.SetActive(!mapPanel.activeSelf);
        }
    }
}