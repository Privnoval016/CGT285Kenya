using UnityEngine;
using Fusion;

/**
 * <summary>
 * ObstructionBlock is a short-lived networked obstacle placed by ObstructionAbility.
 *
 * Properties:
 *   - Square collider, slightly larger than a player.
 *   - Fixed rotation (always axis-aligned) — set at spawn time, never changes.
 *   - Blocks both players and ball movement via its BoxCollider.
 *   - Despawns when: the blockLifetime timer expires, or a goal is scored
 *     (GameManager calls DespawnAll() on all active blocks).
 *
 * Network model:
 *   - State authority is the master client (spawner).
 *   - All other clients receive the object through Fusion's standard replication.
 *   - No custom networked properties needed beyond transform (handled by NetworkTransform).
 * </summary>
 */
[RequireComponent(typeof(NetworkObject))]
public class ObstructionBlock : NetworkBehaviour
{
    [Header("Block Settings")]
    [Tooltip("Seconds before the block automatically despawns.")]
    [SerializeField] private float blockLifetime = 6f;

    [Tooltip("Visual object to show the block on all clients.")]
    [SerializeField] private GameObject visual;

    #region Networked State

    [Networked] private TickTimer LifetimeTimer { get; set; }

    /** Networked spawn position - ensures block appears at correct location on all clients. */
    [Networked] public Vector3 NetworkPosition { get; set; }

    #endregion

    #region Static Registry

    // Master list of all active blocks so GameManager can clear them on goal.
    private static readonly System.Collections.Generic.List<ObstructionBlock> activeBlocks
        = new System.Collections.Generic.List<ObstructionBlock>();

    /**
     * <summary>
     * Despawns all currently active obstruction blocks.
     * Called by GameManager.OnGoalScored() on the state authority.
     * </summary>
     * <param name="runner">The active NetworkRunner.</param>
     */
    public static void DespawnAll(NetworkRunner runner)
    {
        // Iterate a copy because Despawn modifies the list via Despawned().
        var copy = new System.Collections.Generic.List<ObstructionBlock>(activeBlocks);
        foreach (var block in copy)
        {
            if (block != null && block.Object != null && block.Object.IsValid)
                runner.Despawn(block.Object);
        }
        activeBlocks.Clear();
    }

    #endregion

    #region Fusion Lifecycle

    public override void Spawned()
    {
        activeBlocks.Add(this);

        if (Object.HasStateAuthority)
            LifetimeTimer = TickTimer.CreateFromSeconds(Runner, blockLifetime);

        if (visual != null) visual.SetActive(true);
        
        transform.position = NetworkPosition;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        activeBlocks.Remove(this);
    }

    public override void FixedUpdateNetwork()
    {
        transform.position = NetworkPosition;
        
        if (!Object.HasStateAuthority) return;

        if (LifetimeTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    #endregion

    #region Editor

    private void OnValidate()
    {
        blockLifetime = Mathf.Max(1f, blockLifetime);
    }

    #endregion
}

