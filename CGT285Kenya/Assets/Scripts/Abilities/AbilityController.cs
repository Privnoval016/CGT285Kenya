using UnityEngine;
using Fusion;
using System.Collections.Generic;

/**
 * <summary>
 * AbilityController manages a player's equipped abilities.
 * It handles ability initialization, execution, and state management.
 * Uses [SerializeReference] to allow different ability types to be assigned in inspector.
 * This is Unity's way of supporting polymorphism in the inspector.
 * Note: Players can now only equip ONE ability per match.
 * </summary>
 */
public class AbilityController : NetworkBehaviour
{
    [Header("Equipped Ability")]
    [SerializeReference] private AbilityBase equippedAbility;
    
    private NetworkPlayer player;
    
    public AbilityBase EquippedAbility => equippedAbility;
    
    private void Awake()
    {
        player = GetComponent<NetworkPlayer>();
    }

    public override void Spawned()
    {
        if (equippedAbility != null)
        {
            equippedAbility.Initialize(player, Runner);
        }
    }

    public override void FixedUpdateNetwork()
    {
        equippedAbility?.UpdateAbility();
    }

    /**
     * <summary>
     * Executes the equipped ability.
     * </summary>
     * <param name="index">Ability index (always 0 for single ability)</param>
     * <returns>True if ability was successfully executed</returns>
     */
    public bool ExecuteAbility(int index)
    {
        if (equippedAbility == null)
        {
            Debug.LogWarning("[AbilityController] No ability equipped");
            return false;
        }
        
        return equippedAbility.TryExecute();
    }

    /**
     * <summary>
     * Sets the equipped ability for this player.
     * </summary>
     * <param name="ability">The ability to equip</param>
     * <returns>True if ability was successfully equipped</returns>
     */
    public bool EquipAbility(AbilityBase ability)
    {
        equippedAbility = ability;
        if (ability != null && Runner != null)
        {
            ability.Initialize(player, Runner);
        }
        return true;
    }

    /**
     * <summary>
     * Removes the currently equipped ability.
     * </summary>
     */
    public void RemoveAbility()
    {
        equippedAbility = null;
    }

    /**
     * <summary>
     * Gets cooldown info for UI.
     * </summary>
     * <param name="index">Ability index (always 0 for single ability)</param>
     * <returns>Remaining cooldown time in seconds</returns>
     */
    public float GetAbilityCooldown(int index)
    {
        if (equippedAbility != null)
        {
            return equippedAbility.CooldownRemaining;
        }
        return 0f;
    }

    /**
     * <summary>
     * Checks if the ability is ready to use.
     * </summary>
     * <param name="index">Ability index (always 0 for single ability)</param>
     * <returns>True if ability is off cooldown</returns>
     */
    public bool IsAbilityReady(int index)
    {
        if (equippedAbility != null)
        {
            return !equippedAbility.IsOnCooldown;
        }
        return false;
    }
}

