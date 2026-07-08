using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private UIBase[] uiList;

    public void Open(UIBase ui)
    {
        if (ui == null)
        {
            Debug.LogWarning("[UIManager] 열 UI가 없습니다.", this);
            return;
        }

        CloseAll();
        ui.Open();
    }

    public void CloseAll()
    {
        if (uiList == null)
        {
            return;
        }

        foreach (UIBase ui in uiList)
        {
            if (ui == null)
            {
                continue;
            }

            ui.Close();
        }
    }
}