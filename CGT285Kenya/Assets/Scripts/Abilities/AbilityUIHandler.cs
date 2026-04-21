using UnityEngine;
using Fusion;

/**
 * <summary>
 * AbilityUIHandler manages ability selection via UI button clicks.
 * Stores THIS client's selected ability so it persists across scene transitions.
 * </summary>
 */
public class AbilityUIHandler : MonoBehaviour
{
    [SerializeField] private UI uiComponents;

    private static int selectedAbilityIndex = 0;

    private void OnEnable()
    {
        if (uiComponents == null)
            uiComponents = GetComponent<UI>();

        if (uiComponents != null)
        {
            if (uiComponents.dash != null) uiComponents.dash.clicked += () => SelectAbility(0);
            if (uiComponents.teleport != null) uiComponents.teleport.clicked += () => SelectAbility(1);
            if (uiComponents.enlarge != null) uiComponents.enlarge.clicked += () => SelectAbility(2);
            if (uiComponents.obstruction != null) uiComponents.obstruction.clicked += () => SelectAbility(3);
        }
    }

    private void Start()
    {
        int randomAbility = Random.Range(0, 4);
        SelectAbility(randomAbility);
    }

    private void SelectAbility(int abilityIndex)
    {
        selectedAbilityIndex = abilityIndex;
        Debug.Log($"[AbilityUIHandler] Selected ability {abilityIndex}");
    }

    public static int GetSelectedAbility()
    {
        return selectedAbilityIndex;
    }

}




