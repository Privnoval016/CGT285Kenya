using UnityEngine;
using Fusion;

/**
 * <summary>
 * TeleportAbility implements a two-stage teleport mechanic.
 *
 * Stage 1 (beacon placement):
 *   First activation places a TeleportBeacon at the player's current position.
 *   The beacon persists for beaconLifetime seconds.  No cooldown starts here.
 *
 * Stage 2 (teleport):
 *   Second activation teleports the player to the beacon and despawns it.
 *   Cooldown starts after teleporting.
 *
 * Beacon expiry:
 *   If beaconLifetime expires before the player teleports, the beacon despawns
 *   automatically and cooldown starts via TeleportBeacon.RPC_TriggerExpiryCooldown().
 *
 * Network model:
 *   - Stage transitions and beacon spawn/despawn happen via RPCs routed to
 *     StateAuthority so the shared simulation stays consistent.
 *   - The ability tracks Stage locally (InputAuthority) and via a [Networked]
 *     property on AbilityController (AbilityIndex is already synced; the Stage
 *     is communicated implicitly by whether a beacon exists).
 *   - Teleport position is applied by setting the player's CharacterController
 *     warp position, which is then propagated by NetworkTransform.
 * </summary>
 */
[System.Serializable]
public class TeleportAbility : AbilityBase
{
    [Header("Teleport Settings")]
    [Tooltip("Seconds the beacon remains before auto-expiring.")]
    [SerializeField] private float beaconLifetime = 10f;

    [Tooltip("Prefab for the beacon NetworkObject. Must have TeleportBeacon + NetworkObject components.")]
    [SerializeField] private GameObject beaconPrefab;

    // ──────────────────────────────────────────────────────────────────────────
    // Public accessors (used by AbilityController RPCs)
    // ──────────────────────────────────────────────────────────────────────────

    /** The beacon prefab; read by AbilityController.RPC_SpawnBeacon(). */
    public TeleportBeacon GetBeaconPrefab() => beaconPrefab.GetComponent<TeleportBeacon>();

    /** Beacon auto-expiry duration; read by AbilityController.RPC_SpawnBeacon(). */
    public float BeaconLifetime => beaconLifetime;

    // ──────────────────────────────────────────────────────────────────────────
    // Local runtime state (InputAuthority client only)
    // ──────────────────────────────────────────────────────────────────────────

    /** True when a beacon has been placed and is awaiting teleport. */
    private bool beaconPlaced;

    /** Cached reference to the spawned beacon (may be null if not yet resolved). */
    private TeleportBeacon activeBeacon;

    // ──────────────────────────────────────────────────────────────────────────
    // AbilityBase overrides
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Each tick: checks whether the active beacon has been despawned externally
     * (expired) so the local stage state stays consistent.
     * </summary>
     * <param name="context">Runtime context.</param>
     */
    public override void TickAbility(AbilityContext context)
    {
        if (!beaconPlaced) return;

        // Beacon was despawned (expired or consumed by another path).
        if (activeBeacon == null || !activeBeacon.Object.IsValid)
        {
            beaconPlaced = false;
            activeBeacon = null;
        }
    }

    /**
     * <summary>
     * Stage dispatch: first call places the beacon, second call teleports.
     * Called only on InputAuthority client via AbilityController.TryUseAbility().
     * </summary>
     * <param name="context">Runtime context.</param>
     */
    protected override void Execute(AbilityContext context)
    {
        if (!beaconPlaced)
            PlaceBeacon(context);
        else
            DoTeleport(context);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    private void PlaceBeacon(AbilityContext context)
    {
        if (beaconPrefab == null)
        {
            Debug.LogWarning("[TeleportAbility] beaconPrefab is not assigned!");
            return;
        }

        Vector3 pos = context.Player.transform.position;
        Quaternion rot = Quaternion.identity;

        // Spawn via RPC so the master client owns the NetworkObject.
        context.Player.GetComponent<AbilityController>()
            .RPC_SpawnBeacon(pos, rot, context.Player.Object.InputAuthority);

        beaconPlaced = true;
        Debug.Log($"[TeleportAbility] Beacon placement requested at {pos}");
    }

    private void DoTeleport(AbilityContext context)
    {
        if (activeBeacon == null || !activeBeacon.Object.IsValid)
        {
            // Beacon is gone — reset stage without cooldown.
            beaconPlaced = false;
            return;
        }

        Vector3 target = activeBeacon.transform.position;

        // Warp the CharacterController to the beacon position.
        var cc = context.Player.GetComponent<UnityEngine.CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            context.Player.transform.position = target;
            cc.enabled = true;
        }
        else
        {
            context.Player.transform.position = target;
        }

        // Tell state authority to consume (despawn) the beacon.
        context.Player.GetComponent<AbilityController>()
            .RPC_ConsumeBeacon(context.Player.Object.InputAuthority);

        beaconPlaced = false;
        activeBeacon = null;

        StartCooldown();
        Debug.Log($"[TeleportAbility] Teleported to {target}");
    }

    /**
     * <summary>
     * Called by AbilityController.RPC_NotifyBeaconSpawned() after the beacon
     * is spawned on the master client so the InputAuthority peer can cache it.
     * </summary>
     * <param name="beacon">The newly spawned beacon.</param>
     */
    public void RegisterBeacon(TeleportBeacon beacon)
    {
        activeBeacon = beacon;
    }

    public override void OnValidate()
    {
        base.OnValidate();
        beaconLifetime = Mathf.Max(1f, beaconLifetime);
    }
}


