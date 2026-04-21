using UnityEngine;
using Fusion;

[System.Serializable]
public class TeleportAbility : AbilityBase
{
    [Header("Teleport Settings")]
    [Tooltip("Seconds the beacon remains before auto-expiring.")]
    [SerializeField] private float beaconLifetime = 10f;

    [Tooltip("Prefab for the beacon NetworkObject. Must have TeleportBeacon + NetworkObject components.")]
    [SerializeField] private GameObject beaconPrefab;

    /** The beacon prefab; read by AbilityController.RPC_SpawnBeacon(). */
    public TeleportBeacon GetBeaconPrefab() => beaconPrefab.GetComponent<TeleportBeacon>();

    /** Beacon auto-expiry duration; read by AbilityController.RPC_SpawnBeacon(). */
    public float BeaconLifetime => beaconLifetime;
    
    /** True when a beacon has been placed and is awaiting teleport. */
    private bool beaconPlaced;

    /** Cached reference to the spawned beacon (may be null if not yet resolved). */
    private TeleportBeacon activeBeacon;


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

        AbilityFxPlayer.Instance?.PlayFx(AbilityFxEvent.TeleportBeaconPlace, pos);

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

        AbilityFxPlayer.Instance?.PlayFx(AbilityFxEvent.TeleportArrive, target);

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


