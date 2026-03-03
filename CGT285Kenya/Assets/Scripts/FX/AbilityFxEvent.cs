/**
 * <summary>
 * AbilityFxEvent enumerates every moment at which an ability triggers
 * audio or visual feedback. Each value maps to one entry in
 * AbilityFxLibrary, making it trivial for a designer to locate and
 * iterate on any specific effect.
 * </summary>
 */
public enum AbilityFxEvent
{
    // ── Dash ──────────────────────────────────────────────────────────────────
    /** Fired the instant the dash begins. */
    DashActivate,

    // ── Teleport ──────────────────────────────────────────────────────────────
    /** Fired when the teleport beacon is placed on the field. */
    TeleportBeaconPlace,

    /** Fired at the destination the moment the player teleports. */
    TeleportArrive,

    /** Fired at the beacon position when the beacon expires without use. */
    TeleportBeaconExpire,

    // ── Enlarge ───────────────────────────────────────────────────────────────
    /** Fired when the player grows. */
    EnlargeActivate,

    /** Fired when the enlarged state expires and the player returns to normal size. */
    EnlargeDeactivate,

    // ── Obstruction ───────────────────────────────────────────────────────────
    /** Fired at the block position when the obstruction block is placed. */
    ObstructionPlace,
}

