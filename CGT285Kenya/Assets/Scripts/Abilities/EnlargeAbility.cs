using UnityEngine;
using Fusion;

/**
 * <summary>
 * EnlargeAbility increases the player's physical size for a limited duration.
 *
 * Effects while enlarged:
 *   - Visual scale multiplied by enlargeScaleMultiplier.
 *   - Ball steal/intercept radius multiplied by enlargeInterceptMultiplier
 *     (applied via NetworkPlayer.AdditionalInterceptRadius).
 *   - Shot charge speed multiplied by enlargeShotChargeMultiplier
 *     (applied via NetworkPlayer.ShotChargeSpeedMultiplier).
 *
 * Network model:
 *   - The enlarge state (IsEnlarged + remaining time) is replicated via
 *     [Networked] EnlargeTimer on the AbilityController is NOT used here since
 *     AbilityController's CooldownTimer is for the cooldown.  Instead we store
 *     a separate TickTimer as a [Networked] property on the player via
 *     NetworkPlayer.EnlargeTimer, updated through RPC.
 *   - Visual scale change is applied locally by NetworkPlayer.Render() reading
 *     the timer, so it appears on all clients.
 *   - Stat multipliers are read by NetworkPlayer.FixedUpdateNetwork() each tick.
 * </summary>
 */
[System.Serializable]
public class EnlargeAbility : AbilityBase
{
    [Header("Enlarge Settings")]
    [Tooltip("How many seconds the enlarged state lasts.")]
    [SerializeField] private float enlargeDuration = 5f;

    [Tooltip("Uniform scale multiplier applied to the player's transform while enlarged.")]
    [SerializeField] private float enlargeScaleMultiplier = 1.8f;

    [Tooltip("Multiplier applied to the ball intercept/steal radius while enlarged.")]
    [SerializeField] private float enlargeInterceptMultiplier = 1.6f;

    [Tooltip("Multiplier applied to shot charge speed while enlarged (shot fires sooner on hold).")]
    [SerializeField] private float enlargeShotChargeMultiplier = 1.75f;

    // ──────────────────────────────────────────────────────────────────────────
    // Public accessors (read by NetworkPlayer)
    // ──────────────────────────────────────────────────────────────────────────

    /** Uniform scale multiplier while enlarged, 1 otherwise. */
    public float ScaleMultiplier       => enlargeScaleMultiplier;

    /** Intercept radius multiplier while enlarged, 1 otherwise. */
    public float InterceptMultiplier   => enlargeInterceptMultiplier;

    /** Shot charge speed multiplier while enlarged, 1 otherwise. */
    public float ShotChargeMultiplier  => enlargeShotChargeMultiplier;

    // ──────────────────────────────────────────────────────────────────────────
    // AbilityBase overrides
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Activates the enlarged state by writing the timer on the NetworkPlayer
     * via RPC so all peers replicate it.
     * </summary>
     * <param name="context">Runtime context.</param>
     */
    protected override void Execute(AbilityContext context)
    {
        if (context.Player == null) return;

        // Ask state authority to set the EnlargeTimer on the player.
        context.Player.RPC_SetEnlarged(enlargeDuration);

        AbilityFxPlayer.Instance?.PlayFx(AbilityFxEvent.EnlargeActivate, context.Player.transform.position);

        StartCooldown();
        Debug.Log($"[EnlargeAbility] Enlarged for {enlargeDuration}s");
    }

    public override void OnValidate()
    {
        base.OnValidate();
        enlargeDuration               = Mathf.Max(0.5f,   enlargeDuration);
        enlargeScaleMultiplier        = Mathf.Max(1f,      enlargeScaleMultiplier);
        enlargeInterceptMultiplier    = Mathf.Max(1f,      enlargeInterceptMultiplier);
        enlargeShotChargeMultiplier   = Mathf.Max(1f,      enlargeShotChargeMultiplier);
    }
}

