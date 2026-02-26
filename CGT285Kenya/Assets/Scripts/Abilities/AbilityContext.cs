using UnityEngine;
using Fusion;

/**
 * <summary>
 * AbilityContext is a data container passed to every ability on Execute().
 * It gives abilities access to all necessary runtime systems without creating
 * direct coupling between the ability and the player or runner.
 *
 * Design note: struct (not class) to avoid heap allocations per tick.
 * All references are read-only from the ability's perspective.
 * </summary>
 */
public readonly struct AbilityContext
{
    /** The player who owns this ability. */
    public readonly NetworkPlayer Player;

    /** The Fusion NetworkRunner for this session. */
    public readonly NetworkRunner Runner;

    /** The shared ball controller; may be null before ball is spawned. */
    public readonly NetworkBallController Ball;

    /**
     * <summary>
     * Constructs a fully-populated AbilityContext.
     * </summary>
     * <param name="player">The owning player.</param>
     * <param name="runner">The active NetworkRunner.</param>
     * <param name="ball">The ball in the scene (may be null).</param>
     */
    public AbilityContext(NetworkPlayer player, NetworkRunner runner, NetworkBallController ball)
    {
        Player = player;
        Runner = runner;
        Ball   = ball;
    }
}

