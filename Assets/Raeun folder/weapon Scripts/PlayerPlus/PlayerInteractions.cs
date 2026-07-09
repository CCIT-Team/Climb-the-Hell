using UnityEngine;

public class PlayerInteractions : MonoBehaviour
{
    [SerializeField] private TraitUI traitUI;

    private bool isInRange;
    private bool isOpen;

    private void Awake()
    {
        ResolveTraitUI();
    }

    private void Update()
    {
        if (!isInRange)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E pressed");
            ToggleTraitUI();
        }
    }

    private void ToggleTraitUI()
    {
        ResolveTraitUI();

        if (traitUI == null)
        {
            isOpen = false;

            Debug.LogWarning(
                "[PlayerInteractions] TraitUI를 찾지 못해 특성 UI를 열 수 없습니다.",
                this
            );

            return;
        }

        isOpen = !isOpen;

        if (isOpen)
        {
            traitUI.RefreshUI();
            traitUI.Open();
        }
        else
        {
            traitUI.Close();
        }
    }

    private void ResolveTraitUI()
    {
        if (traitUI != null)
        {
            return;
        }

        traitUI =
            FindObjectOfType<TraitUI>(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TraitNPC"))
        {
            isInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("TraitNPC"))
        {
            isInRange = false;
            isOpen = false;

            ResolveTraitUI();

            if (traitUI != null)
            {
                traitUI.Close();
            }
        }
    }

    private void OnDisable()
    {
        isInRange = false;
        isOpen = false;
    }
}
