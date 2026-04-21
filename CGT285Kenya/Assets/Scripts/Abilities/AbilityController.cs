using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkPlayer))]
public class AbilityController : NetworkBehaviour
{
    [SerializeField] private AbilityAssignmentConfig abilityConfig;

    #region Networked State

    /**
     * <summary>
     * Replicated cooldown timer.  Written by SetCooldown(); read by AbilityBase.IsOnCooldown
     * and by the HUD on all clients.
     * </summary>
     */
    [Networked] public TickTimer CooldownTimer { get; set; }

    /**
     * <summary>
     * Index representing the active ability (0=Dash, 1=Teleport, 2=Enlarge, 3=Obstruction).
     * Replicated so all clients can display the correct ability name/icon.
     * </summary>
     */
    [Networked] public int AbilityIndex { get; set; }

    #endregion

    #region Private State

    private NetworkPlayer player;
    private AbilityBase activeAbility;

    // Track the last index we materialised so Render() rebuilds when it changes.
    private int lastBuiltIndex = -2; // -2 = "never built"

    #endregion

    #region Public Accessors

    /** The currently equipped ability; null if none assigned. */
    public AbilityBase ActiveAbility => activeAbility;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        player = GetComponent<NetworkPlayer>();
    }

    #endregion

    #region Fusion Lifecycle

    public override void Spawned()
    {
        RebuildAbilityInstance();
    }

    public override void Render()
    {
        // Rebuild whenever the replicated AbilityIndex changes on any client.
        if (AbilityIndex != lastBuiltIndex)
            RebuildAbilityInstance();
    }

    public override void FixedUpdateNetwork()
    {
        if (activeAbility == null) return;
        if (!Object.HasInputAuthority) return;

        AbilityContext ctx = BuildContext();
        activeAbility.TickAbility(ctx);
    }

    #endregion

    #region Public API

    /**
     * <summary>
     * Assigns an ability by index.
     * Must be called on the object's state authority (the owning client in Shared mode).
     * Safe to call immediately after runner.Spawn() returns — will not be overwritten
     * by Spawned() because Spawned() only calls RebuildAbilityInstance().
     * </summary>
     * <param name="index">Ability index (0=Dash, 1=Teleport, 2=Enlarge, 3=Obstruction).</param>
     */
    public void AssignAbility(int index)
    {
        if (!Object.HasStateAuthority) return;
        AbilityIndex = index;
        RebuildAbilityInstance();
    }

    /**
     * <summary>
     * RPC for UI-based ability assignment. Routes the request to state authority
     * which then updates AbilityIndex on all clients via Render().
     * </summary>
     * <param name="abilityType">Ability type index (0=Dash, 1=Teleport, 2=Enlarge, 3=Obstruction).</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AssignAbilityByType(int abilityType)
    {
        AbilityIndex = abilityType;
    }

    /**
     * <summary>
     * Attempts to use the active ability.
     * Must be called from inside FixedUpdateNetwork() (after GetInput()).
     * </summary>
     * <param name="worldTapPosition">World-space tap position (used by Obstruction).</param>
     * <returns>True if the ability fired.</returns>
     */
    public bool TryUseAbility(Vector3 worldTapPosition)
    {
        if (activeAbility == null) return false;

        AbilityContext ctx = BuildContext();
        return activeAbility.TryExecuteAtPosition(ctx, worldTapPosition);
    }

    /**
     * <summary>
     * Writes the cooldown timer.  Called by AbilityBase.StartCooldown() and by
     * abilities that start the cooldown at a non-standard moment.
     * </summary>
     * <param name="durationSeconds">Cooldown duration in seconds.</param>
     */
    public void SetCooldown(float durationSeconds)
    {
        if (Runner == null) return;
        CooldownTimer = TickTimer.CreateFromSeconds(Runner, durationSeconds);
    }

    /**
     * <summary>
     * Remaining cooldown in seconds for HUD display.
     * Safe to call from any client.
     * </summary>
     * <returns>Seconds remaining, or 0 if ready.</returns>
     */
    public float GetCooldownRemaining()
    {
        if (Runner == null) return 0f;
        return CooldownTimer.RemainingTime(Runner) ?? 0f;
    }

    /**
     * <summary>
     * Whether the ability is currently ready (not on cooldown).
     * </summary>
     * <returns>True when the ability can be used.</returns>
     */
    public bool IsAbilityReady() =>
        Runner != null && CooldownTimer.ExpiredOrNotRunning(Runner);

    #endregion

    #region Spawn RPCs

    /**
     * <summary>
     * Master-client RPC: spawns a TeleportBeacon at the given position and
     * notifies the owning player's TeleportAbility via a follow-up RPC.
     * </summary>
     * <param name="position">World-space position for the beacon.</param>
     * <param name="rotation">Rotation for the beacon.</param>
     * <param name="ownerRef">PlayerRef of the requesting player.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnBeacon(Vector3 position, Quaternion rotation, PlayerRef ownerRef)
    {
        var beaconAbility = activeAbility as TeleportAbility;
        if (beaconAbility == null) return;

        TeleportBeacon prefab = beaconAbility.GetBeaconPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[AbilityController] TeleportAbility.beaconPrefab is not assigned!");
            return;
        }

        TeleportBeacon beacon = Runner.Spawn(prefab, position, rotation);
        if (beacon == null) return;

        beacon.OwnerRef = ownerRef;
        beacon.NetworkPosition = position;
        beacon.ExpiryTimer = TickTimer.CreateFromSeconds(Runner, beaconAbility.BeaconLifetime);

        RPC_NotifyBeaconSpawned(beacon.Object, ownerRef);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyBeaconSpawned(NetworkObject beaconObj, PlayerRef ownerRef)
    {
        if (beaconObj == null || !beaconObj.IsValid) return;
        if (!Object.HasInputAuthority) return;
        if (Object.InputAuthority != ownerRef) return;

        var beacon = beaconObj.GetComponent<TeleportBeacon>();
        if (beacon == null) return;

        var teleportAbility = activeAbility as TeleportAbility;
        teleportAbility?.RegisterBeacon(beacon);
    }

    /**
     * <summary>
     * Master-client RPC: despawns the active beacon for a given owner.
     * </summary>
     * <param name="ownerRef">PlayerRef of the player whose beacon to consume.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ConsumeBeacon(PlayerRef ownerRef)
    {
        foreach (var beacon in FindObjectsByType<TeleportBeacon>(FindObjectsSortMode.None))
        {
            if (beacon.OwnerRef == ownerRef && beacon.Object.IsValid)
            {
                beacon.Consume();
                return;
            }
        }
    }

    /**
     * <summary>
     * Master-client RPC: spawns an ObstructionBlock at the given position.
     * Block orientation is always Quaternion.identity (spec: fixed angle).
     * </summary>
     * <param name="position">World-space position for the block.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SpawnObstructionBlock(Vector3 position)
    {
        var obstructionAbility = activeAbility as ObstructionAbility;
        if (obstructionAbility == null) return;

        ObstructionBlock prefab = obstructionAbility.GetBlockPrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[AbilityController] ObstructionAbility.blockPrefab is not assigned!");
            return;
        }

        ObstructionBlock block = Runner.Spawn(prefab, position, Quaternion.identity);
        if (block == null) return;
        
        block.NetworkPosition = position;
    }

    #endregion

    #region Helpers

    /**
     * <summary>
     * Rebuilds the in-memory ability instance from AbilityIndex.
     * Creates a fresh instance with default serialized values.
     * Records the built index so Render() can detect future changes.
     * </summary>
     */
    private void RebuildAbilityInstance()
    {
        int idx = (Object != null && Object.IsValid) ? AbilityIndex : -1;
        lastBuiltIndex = idx;
        activeAbility  = null;

        AbilityBase template = GetAbilityByIndex(idx);
        if (template == null) return;

        activeAbility = template;
        activeAbility.Initialize(this);
        Debug.Log($"[AbilityController] Player {(Object != null ? Object.InputAuthority.PlayerId.ToString() : "?")} equipped {activeAbility.AbilityName} (index {idx})");
    }

    /**
     * <summary>
     * Direct ability lookup by type index (used by UI-based assignment).
     * Returns the ability from config or creates a default instance.
     * </summary>
     * <param name="index">Ability type index (0=Dash, 1=Teleport, 2=Enlarge, 3=Obstruction).</param>
     * <returns>The AbilityBase template.</returns>
     */
    private AbilityBase GetAbilityByIndex(int index)
    {
        if (abilityConfig != null)
        {
            return abilityConfig.GetAbility(index);
        }

        /* Fallback to default instances if no config assigned */
        return index switch
        {
            0 => new DashAbility(),
            1 => new TeleportAbility(),
            2 => new EnlargeAbility(),
            3 => new ObstructionAbility(),
            _ => null
        };
    }

    /**
     * <summary>
     * Builds a fresh AbilityContext snapshot each tick.
     * Ball is looked up lazily to handle late-spawned balls.
     * </summary>
     * <returns>Current AbilityContext.</returns>
     */
    private AbilityContext BuildContext()
    {
        var ball = FindFirstObjectByType<NetworkBallController>();
        return new AbilityContext(player, Runner, ball);
    }

    #endregion
}
