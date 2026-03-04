using UnityEngine;
using Fusion;

/**
 * <summary>
 * AbilityBase is the abstract base class for all player abilities.
 *
 * Design Patterns:
 *   Strategy – each concrete subclass is a swappable strategy.
 *   Template Method – TryExecute() calls the abstract Execute() hook.
 *
 * Network safety:
 *   Cooldown is tracked via a Fusion TickTimer stored on the owning
 *   AbilityController (a NetworkBehaviour), so it is replicated and
 *   deterministic across all peers. AbilityBase itself is a plain C# class
 *   and holds no [Networked] state of its own.
 *
 *   Execute() is only ever called on the client that has InputAuthority,
 *   so side-effects (RPCs, spawns) are triggered exactly once.
 * </summary>
 */
[System.Serializable]
public abstract class AbilityBase
{
    [Header("Base Ability Settings")]
    [SerializeField] protected string abilityName    = "Unnamed Ability";
    [SerializeField] protected float  cooldownDuration = 5f;
    [SerializeField] protected Sprite abilityIcon;

    // ──────────────────────────────────────────────────────────────────────────
    // Runtime state — injected by AbilityController.Initialize()
    // ──────────────────────────────────────────────────────────────────────────

    /** The controller that owns this ability; used to read/write the TickTimer. */
    protected AbilityController Controller;

    // ──────────────────────────────────────────────────────────────────────────
    // Public properties
    // ──────────────────────────────────────────────────────────────────────────

    /** Display name shown in the UI. */
    public string AbilityName      => abilityName;

    /** Total cooldown length in seconds. */
    public float  CooldownDuration => cooldownDuration;

    /** Optional icon for the HUD. */
    public Sprite Icon             => abilityIcon;

    /**
     * <summary>
     * True when the ability cannot be used yet.
     * Reads the TickTimer stored on the AbilityController so the value is
     * always consistent with the networked simulation.
     * </summary>
     */
    public bool IsOnCooldown
    {
        get
        {
            if (Controller == null || Controller.Runner == null) return false;
            return !Controller.CooldownTimer.ExpiredOrNotRunning(Controller.Runner);
        }
    }

    /**
     * <summary>Remaining cooldown time in seconds (0 when ready).</summary>
     */
    public float CooldownRemaining
    {
        get
        {
            if (Controller == null || Controller.Runner == null) return 0f;
            return Controller.CooldownTimer.RemainingTime(Controller.Runner) ?? 0f;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Binds this ability to its owning AbilityController.
     * Called by AbilityController.Spawned().
     * </summary>
     * <param name="controller">The NetworkBehaviour that owns this ability.</param>
     */
    public virtual void Initialize(AbilityController controller)
    {
        Controller = controller;
    }

    /**
     * <summary>
     * Attempts to execute the ability.
     * Guards against cooldown before forwarding to Execute().
     * </summary>
     * <param name="context">Full runtime context for the ability.</param>
     * <returns>True if execution succeeded.</returns>
     */
    public bool TryExecute(AbilityContext context)
    {
        if (IsOnCooldown)
        {
            Debug.Log($"[Ability] {abilityName} is on cooldown ({CooldownRemaining:F1}s)");
            return false;
        }

        Execute(context);
        return true;
    }

    /**
     * <summary>
     * Called when the ability button is pressed with a world-space position
     * (used by Obstruction for targeting; all other abilities can ignore pos).
     * The default forwards to the parameterless Execute overload.
     * </summary>
     * <param name="context">Full runtime context.</param>
     * <param name="worldPosition">World-space position of the input tap (may be Vector3.zero).</param>
     * <returns>True if execution succeeded.</returns>
     */
    public virtual bool TryExecuteAtPosition(AbilityContext context, Vector3 worldPosition)
    {
        return TryExecute(context);
    }

    /**
     * <summary>
     * Starts the cooldown timer.  Called by concrete abilities after their
     * effect has been committed so that multi-stage abilities can start the
     * cooldown at the correct moment.
     * </summary>
     */
    protected void StartCooldown()
    {
        if (Controller != null && Controller.Runner != null)
            Controller.SetCooldown(cooldownDuration);
    }

    /**
     * <summary>
     * Per-tick update hook. Called by AbilityController.FixedUpdateNetwork().
     * Override to drive time-limited effects (dash movement, enlarge duration, etc.).
     * </summary>
     * <param name="context">Full runtime context.</param>
     */
    public virtual void TickAbility(AbilityContext context) { }

    /**
     * <summary>
     * Validates serialized parameters inside the Unity editor.
     * </summary>
     */
    public virtual void OnValidate()
    {
        cooldownDuration = Mathf.Max(0f, cooldownDuration);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Abstract hooks
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Core ability logic implemented by each concrete subclass.
     * Only called on the peer with InputAuthority.
     * </summary>
     * <param name="context">Full runtime context.</param>
     */
    protected abstract void Execute(AbilityContext context);
}
