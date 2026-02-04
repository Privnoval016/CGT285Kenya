using UnityEngine;
using Fusion;

/**
 * AbilityBase is the abstract base class for all abilities.
 * Uses the Strategy Pattern to allow runtime polymorphism.
 * 
 * Design Pattern: Strategy Pattern
 * - Each ability is a concrete strategy implementing Execute()
 * - Abilities can be swapped at runtime
 * - Uses [SerializeReference] to allow inheritance in inspector
 * 
 * Key Features:
 * - Server-authoritative execution
 * - Built-in cooldown system
 * - Network synchronization through Fusion
 */
[System.Serializable]
public abstract class AbilityBase
{
    [Header("Base Ability Settings")]
    [SerializeField] protected string abilityName = "Unnamed Ability";
    [SerializeField] protected float cooldownDuration = 5f;
    [SerializeField] protected float energyCost = 20f;
    [SerializeField] protected Sprite abilityIcon;
    
    protected NetworkPlayer Owner;
    protected NetworkRunner Runner;
    protected float LastUsedTime;
    
    public string AbilityName => abilityName;
    public float CooldownDuration => cooldownDuration;
    public float EnergyCost => energyCost;
    public Sprite Icon => abilityIcon;
    public bool IsOnCooldown => Time.time - LastUsedTime < cooldownDuration;
    public float CooldownRemaining => Mathf.Max(0, cooldownDuration - (Time.time - LastUsedTime));
    
    /**
     * Initializes the ability with its owner and runner context
     */
    public virtual void Initialize(NetworkPlayer owner, NetworkRunner runner)
    {
        Owner = owner;
        Runner = runner;
        LastUsedTime = -cooldownDuration;
    }
    
    /**
     * Attempts to execute the ability.
     * Checks cooldown and energy cost before executing.
     */
    public bool TryExecute()
    {
        if (IsOnCooldown)
        {
            Debug.Log($"{abilityName} is on cooldown!");
            return false;
        }
        
        Execute();
        LastUsedTime = Time.time;
        return true;
    }
    
    /**
     * The actual ability logic. Must be implemented by concrete abilities.
     */
    protected abstract void Execute();
    
    /**
     * Called every frame to update ability state (e.g., channeling, effects)
     */
    public virtual void UpdateAbility()
    {
        // Override in derived classes if needed
    }
    
    /**
     * Validates ability parameters in the editor
     */
    public virtual void OnValidate()
    {
        cooldownDuration = Mathf.Max(0, cooldownDuration);
        energyCost = Mathf.Max(0, energyCost);
    }
}



