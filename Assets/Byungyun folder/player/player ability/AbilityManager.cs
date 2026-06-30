using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    [SerializeField]
    private List<AbilityData> selectedAbilities = new();

    public IReadOnlyList<AbilityData> SelectedAbilities =>
        selectedAbilities;

    public event System.Action OnAbilityChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddAbility(AbilityData ability)
    {
        if (ability == null)
        {
            return;
        }

        selectedAbilities.Add(ability);

        OnAbilityChanged?.Invoke();
    }

    public void RemoveAbility(AbilityData ability)
    {
        if (selectedAbilities.Remove(ability))
        {
            OnAbilityChanged?.Invoke();
        }
    }

    public int GetCategoryCount(AbilityCategory category)
    {
        int count = 0;

        foreach (AbilityData ability in selectedAbilities)
        {
            if (ability.category == category)
            {
                count++;
            }
        }

        return count;
    }

    public int GetTagCount(AbilityTag tag)
    {
        int count = 0;

        foreach (AbilityData ability in selectedAbilities)
        {
            if (ability.tags.Contains(tag))
            {
                count++;
            }
        }

        return count;
    }
}