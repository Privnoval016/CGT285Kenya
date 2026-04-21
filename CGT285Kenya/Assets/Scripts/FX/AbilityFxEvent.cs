
public enum AbilityFxEvent
{
    /** Fired the instant the dash begins. */
    DashActivate,

    /** Fired when the teleport beacon is placed on the field. */
    TeleportBeaconPlace,

    /** Fired at the destination the moment the player teleports. */
    TeleportArrive,

    /** Fired at the beacon position when the beacon expires without use. */
    TeleportBeaconExpire,

    /** Fired when the player grows. */
    EnlargeActivate,

    /** Fired when the enlarged state expires and the player returns to normal size. */
    EnlargeDeactivate,

    /** Fired at the block position when the obstruction block is placed. */
    ObstructionPlace,
}

