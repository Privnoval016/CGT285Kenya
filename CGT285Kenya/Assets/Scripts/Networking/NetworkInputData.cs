using Fusion;
using UnityEngine;

/// <summary>
/// NetworkInputData defines the input structure that gets sent over the network.
/// In Fusion, input is gathered on the client, sent to the server, and then
/// resimulated on all clients for prediction and reconciliation.
/// 
/// This struct MUST be blittable (no managed types) and implement INetworkInput.
/// Size should be kept small as it's sent every tick (~60Hz).
/// </summary>
public struct NetworkInputData : INetworkInput
{
    /** Movement joystick (left stick) */
    public Vector2 MovementInput;
    
    /** Aim joystick (right stick) - used for passing/shooting direction */
    public Vector2 AimInput;
    
    /** How long the aim joystick has been held */
    public float AimHoldDuration;
    
    /** True on the frame the aim stick was released (for shooting) */
    public NetworkBool AimJustReleased;
    
    /** Last aim direction before release (for shooting) */
    public Vector2 LastAimDirection;
    
    /** Action buttons */
    public NetworkButtons Buttons;
}

/**
 * Enum defining all possible button inputs.
 * These get packed into a bitmask by Fusion for efficiency.
 */
public enum InputButton
{
    /** Primary action button - context sensitive (pickup/pass/shoot) */
    Action = 0,
    
    /** Single ability button - player equips one ability per match */
    Ability1 = 1,
}

