using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("입력")]
    [SerializeField] private Key interactKey = Key.E;

    [Header("상호작용 안내 UI (선택)")]
    [SerializeField] private GameObject interactionGuide;
    [SerializeField] private TextMeshProUGUI interactionGuideText;

    private readonly List<InteractableBase> nearbyInteractables =
        new List<InteractableBase>(8);

    private Player player;
    private InputAction interactAction;
    private InteractableBase currentInteractable;
    private bool interactionBlocked;

    private void Awake()
    {
        player = GetComponent<Player>();

        interactAction = new InputAction(
            "Interact",
            InputActionType.Button,
            $"<Keyboard>/{interactKey.ToString().ToLowerInvariant()}"
        );

        SetGuideVisible(false);
    }

    private void OnEnable()
    {
        interactAction.performed += HandleInteract;
        interactAction.Enable();
    }

    private void OnDisable()
    {
        interactAction.performed -= HandleInteract;
        interactAction.Disable();

        nearbyInteractables.Clear();
        currentInteractable = null;
        interactionBlocked = false;
        SetGuideVisible(false);
    }

    private void OnDestroy()
    {
        interactAction?.Dispose();
    }

    private void HandleInteract(InputAction.CallbackContext context)
    {
        if (interactionBlocked)
        {
            return;
        }

        SelectBestInteractable();

        if (currentInteractable == null ||
            !currentInteractable.CanInteract())
        {
            return;
        }

        currentInteractable.Interact(player);
        SelectBestInteractable();
    }

    public void RegisterInteractable(InteractableBase interactable)
    {
        if (interactable == null ||
            nearbyInteractables.Contains(interactable))
        {
            return;
        }

        nearbyInteractables.Add(interactable);
        SelectBestInteractable();
    }

    public void UnregisterInteractable(InteractableBase interactable)
    {
        if (interactable == null)
        {
            return;
        }

        nearbyInteractables.Remove(interactable);

        if (currentInteractable == interactable)
        {
            currentInteractable = null;
        }

        SelectBestInteractable();
    }

    public void SetInteractionBlocked(bool blocked)
    {
        interactionBlocked = blocked;

        if (blocked)
        {
            currentInteractable = null;
            SetGuideVisible(false);
            return;
        }

        SelectBestInteractable();
    }

    private void SelectBestInteractable()
    {
        RemoveInvalidInteractables();

        if (interactionBlocked)
        {
            currentInteractable = null;
            SetGuideVisible(false);
            return;
        }

        InteractableBase best = null;
        int bestPriority = int.MinValue;
        float bestDistanceSqr = float.MaxValue;
        Vector3 playerPosition = transform.position;

        for (int i = 0; i < nearbyInteractables.Count; i++)
        {
            InteractableBase candidate = nearbyInteractables[i];

            if (candidate == null ||
                !candidate.isActiveAndEnabled ||
                !candidate.CanInteract())
            {
                continue;
            }

            Transform target = candidate.InteractionTransform;

            if (target == null)
            {
                continue;
            }

            int priority = candidate.InteractionPriority;
            float distanceSqr =
                (target.position - playerPosition).sqrMagnitude;

            if (priority > bestPriority ||
                (priority == bestPriority &&
                 distanceSqr < bestDistanceSqr))
            {
                best = candidate;
                bestPriority = priority;
                bestDistanceSqr = distanceSqr;
            }
        }

        currentInteractable = best;
        UpdateGuide();
    }

    private void RemoveInvalidInteractables()
    {
        for (int i = nearbyInteractables.Count - 1; i >= 0; i--)
        {
            InteractableBase target = nearbyInteractables[i];

            if (target == null || !target.isActiveAndEnabled)
            {
                nearbyInteractables.RemoveAt(i);
            }
        }
    }

    private void UpdateGuide()
    {
        bool visible = currentInteractable != null;
        SetGuideVisible(visible);

        if (visible && interactionGuideText != null)
        {
            interactionGuideText.text = currentInteractable.InteractionText;
        }
    }

    private void SetGuideVisible(bool visible)
    {
        if (interactionGuide != null)
        {
            interactionGuide.SetActive(visible);
        }
    }
}
