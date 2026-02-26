using Fusion;
using UnityEngine;

/**
 * <summary>
 * NetworkInputData defines the input structure sent over the network every tick.
 * In Fusion, input is gathered on the client, sent to the server, and resimulated
 * on all clients for prediction and reconciliation.
 *
 * IMPORTANT: This struct must be blittable (no managed types) and implement INetworkInput.
 * Keep it small — it is sent at the simulation tick rate (~60 Hz).
 *
 * Shoot / pass design:
 *   - AimInput magnitude > deadzone means the player is currently aiming.
 *   - AimJustReleased == true means the aim stick was released THIS tick.
 *   - AimHoldDuration >= threshold means it was a held shot; otherwise a pass.
 *   - LastAimDirection holds the direction captured at the moment of release.
 * </summary>
 */
public struct NetworkInputData : INetworkInput
{
    /** Movement joystick (left stick) direction, normalised. */
    public Vector2 MovementInput;

    /** Aim joystick (right stick) direction, normalised. Non-zero while player is aiming. */
    public Vector2 AimInput;

    /** Seconds the aim stick has been held continuously this sequence. */
    public float AimHoldDuration;

    /**
     * True for exactly one tick — the tick on which the aim stick was released.
     * InputController guarantees this is set at most once per press-release cycle.
     */
    public NetworkBool AimJustReleased;

    /** The aim direction captured at the moment of release. */
    public Vector2 LastAimDirection;

    /** Packed button bitmask (ability, etc.). */
    public NetworkButtons Buttons;

    /**
     * World-space tap position captured when an ability is activated.
     * Used by ObstructionAbility to determine block placement.
     * Vector3.zero means "no tap position this tick."
     */
    public Vector3 AbilityTapPosition;
}

/**
 * <summary>
 * Enum defining all possible button inputs.
 * Values are packed into a bitmask by Fusion for wire efficiency.
 * </summary>
 */
public enum InputButton
{
    /** Single ability button — player equips one ability per match. */
    Ability1 = 0,
}

