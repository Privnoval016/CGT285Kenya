using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Soccer/Ability Assignment Config", fileName = "AbilityAssignmentConfig")]
public class AbilityAssignmentConfig : ScriptableObject
{
    [Header("Ordered Ability List")]
    [Tooltip("Player 1 gets index 0, player 2 gets index 1, etc. Wraps if fewer entries than players.")]
    [SerializeReference] private List<AbilityBase> abilities = new List<AbilityBase>();

    /** Read-only view of the ability list for editor tooling. */
    public IReadOnlyList<AbilityBase> Abilities => abilities;

    /**
     * <summary>
     * Returns the ability template for the given index.
     * Wraps around if index exceeds the list length.
     * Returns null if the list is empty or the index is negative.
     * </summary>
     * <param name="index">Zero-based player join order index.</param>
     * <returns>The AbilityBase template, or null.</returns>
     */
    public AbilityBase GetAbility(int index)
    {
        if (abilities == null || abilities.Count == 0) return null;
        if (index < 0) return null;
        return abilities[index % abilities.Count];
    }

    /**
     * <summary>
     * Returns the ability index to assign to the Nth player who joined.
     * Wraps if joinOrder exceeds list length.
     * </summary>
     * <param name="joinOrder">Zero-based join order (0 = first player).</param>
     * <returns>Index into the abilities list.</returns>
     */
    public int GetAbilityIndex(int joinOrder)
    {
        if (abilities == null || abilities.Count == 0) return 0;
        return joinOrder % abilities.Count;
    }
}

