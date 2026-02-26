using UnityEngine;
using Fusion;

/**
 * <summary>
 * DashAbility propels the player forward at high speed for a short duration.
 *
 * Behaviour summary:
 *   - On activation the player's speed is overridden for dashDuration seconds.
 *   - Direction: current movement input direction, or last known direction if
 *     standing still.
 *   - During the dash the player can pick up a free ball or steal an opponent's
 *     ball by entering the steal zone at dash speed.
 *   - Cooldown starts when the dash finishes (not when it begins), so the player
 *     always knows they can use the ability for a full dashDuration after pressing it.
 *
 * Network model:
 *   - Execute() fires only on InputAuthority (gated by AbilityController).
 *   - DashTimer and DashDirection are held locally; NetworkPlayer reads
 *     IsDashing / DashSpeedMultiplier each tick to override movement speed.
 *   - No RPC needed: movement is already replicated via NetworkPlayer's
 *     deterministic FixedUpdateNetwork + GetInput() path.
 *
 * All tuning values are serialized and exposed in the inspector.
 * </summary>
 */
[System.Serializable]
public class DashAbility : AbilityBase
{
    [Header("Dash Settings")]
    [Tooltip("How long the dash lasts in seconds.")]
    [SerializeField] private float dashDuration = 0.35f;

    [Tooltip("Speed multiplier applied to the player's base move speed during the dash.")]
    [SerializeField] private float dashSpeedMultiplier = 3.5f;

    [Tooltip("If movement input is below this magnitude, the last known direction is used instead.")]
    [SerializeField] private float standingStillThreshold = 0.1f;

    // ──────────────────────────────────────────────────────────────────────────
    // Runtime state (non-networked; local to InputAuthority client)
    // ──────────────────────────────────────────────────────────────────────────

    private bool isDashing;
    private float dashEndTime;         // Fusion Runner.SimulationTime
    private Vector3 dashDirection;
    private Vector3 lastKnownMoveDir;

    // ──────────────────────────────────────────────────────────────────────────
    // Public accessors (read by NetworkPlayer each tick)
    // ──────────────────────────────────────────────────────────────────────────

    /** True while the dash is in progress. */
    public bool IsDashing => isDashing;

    /**
     * <summary>
     * Speed multiplier this tick. Returns dashSpeedMultiplier while dashing,
     * 1.0 otherwise.
     * </summary>
     */
    public float CurrentSpeedMultiplier => isDashing ? dashSpeedMultiplier : 1f;

    /**
     * <summary>
     * World-space direction of the active dash. Zero when not dashing.
     * NetworkPlayer uses this to override movement direction mid-dash.
     * </summary>
     */
    public Vector3 DashDirection => isDashing ? dashDirection : Vector3.zero;

    // ──────────────────────────────────────────────────────────────────────────
    // AbilityBase overrides
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Records the move direction when it is non-zero so we have a fallback
     * direction when the player is standing still at activation time.
     * </summary>
     * <param name="context">Runtime context.</param>
     */
    public override void TickAbility(AbilityContext context)
    {
        if (context.Player == null) return;

        // Keep last-known direction updated each tick for standing-still fallback.
        Vector3 moveDir = context.Player.CurrentMoveDirection;
        if (moveDir.magnitude > standingStillThreshold)
            lastKnownMoveDir = moveDir.normalized;

        // Expire the dash and start cooldown when time is up.
        if (isDashing && context.Runner.SimulationTime >= dashEndTime)
        {
            isDashing = false;
            StartCooldown();
            Debug.Log("[DashAbility] Dash finished, starting cooldown.");
        }
    }

    /**
     * <summary>
     * Begins the dash.  Captures direction and sets the end-time using
     * Fusion's deterministic SimulationTime.
     * </summary>
     * <param name="context">Runtime context.</param>
     */
    protected override void Execute(AbilityContext context)
    {
        if (context.Player == null) return;

        // Prefer current input; fall back to last known.
        Vector3 moveDir = context.Player.CurrentMoveDirection;
        dashDirection = (moveDir.magnitude > standingStillThreshold)
            ? moveDir.normalized
            : (lastKnownMoveDir.magnitude > 0.01f ? lastKnownMoveDir : context.Player.transform.forward);

        isDashing = true;
        dashEndTime = context.Runner.SimulationTime + dashDuration;

        Debug.Log($"[DashAbility] Dash started — dir={dashDirection}, duration={dashDuration}s");
    }

    public override void OnValidate()
    {
        base.OnValidate();
        dashDuration          = Mathf.Max(0.05f, dashDuration);
        dashSpeedMultiplier   = Mathf.Max(1f,    dashSpeedMultiplier);
        standingStillThreshold = Mathf.Clamp01(standingStillThreshold);
    }
}

