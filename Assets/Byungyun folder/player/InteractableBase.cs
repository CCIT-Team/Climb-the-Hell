using UnityEngine;

public abstract class InteractableBase :
    MonoBehaviour
{
    [Header("상호작용 기본 설정")]
    [Tooltip("겹치는 상호작용 대상 중 높은 값이 우선")]
    [SerializeField]
    private int interactionPriority = 0;

    [SerializeField]
    private string interactionText =
        "E키로 상호작용";

    public int InteractionPriority =>
        interactionPriority;

    public string InteractionText =>
        interactionText;

    public virtual Transform
        InteractionTransform =>
            transform;

    public abstract bool CanInteract();

    public abstract void Interact(
        Player player
    );
}