using UnityEngine;

public class PlayerInteractions : MonoBehaviour
{
    [SerializeField] private TraitUI traitUI;

    private bool isInRange;
    private bool isOpen;

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
        isOpen = !isOpen;

        if (isOpen)
        {
            traitUI.Open();
        }
        else
        {
            traitUI.Close();
        }
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
            traitUI.Close();
        }
    }
}