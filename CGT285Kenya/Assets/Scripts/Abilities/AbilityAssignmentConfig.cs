using System.Collections.Generic;
using UnityEngine;

/**
 * <summary>
 * AbilityAssignmentConfig is a ScriptableObject that defines the ordered list
 * of abilities assigned to players as they join.
 *
 * Assignment model:
 *   Player 1 gets Abilities[0], player 2 gets Abilities[1], etc.
 *   If there are more players than abilities the list wraps (modulo).
 *   You can reorder the list in the inspector to change who gets what.
 *
 * Usage:
 *   Create via Assets > Create > Soccer > Ability Assignment Config.
 *   Drag the ScriptableObject into AbilityController.assignmentConfig on the
 *   player prefab and into NetworkCallbackHandler.abilityConfig.
 * </summary>
 */
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
        if (abilities == null || abilities.Count == 0) return -1;
        return joinOrder % abilities.Count;
    }
}

