using UnityEngine;
using Fusion;

/**
 * <summary>
 * TeleportBeacon is a short-lived networked object placed by TeleportAbility.
 *
 * Lifecycle:
 *   1. Spawned at the player's position when the ability is first activated.
 *   2. Persists until: the player activates the ability again (teleports to it)
 *      OR the expiry timer runs out.
 *   3. On expiry without use, the beacon despawns and the owning player's
 *      ability cooldown is triggered via RPC.
 *
 * All state is [Networked] so the visual can be rendered on all clients.
 * </summary>
 */
public class TeleportBeacon : NetworkBehaviour
{
    [Header("Beacon Visuals")]
    [Tooltip("Optional visual mesh/renderer shown to all clients while the beacon is active.")]
    [SerializeField] private GameObject visual;

    #region Networked State

    /** PlayerRef of the player who placed this beacon. */
    [Networked] public PlayerRef OwnerRef { get; set; }

    /** Tick-based expiry timer. Set by TeleportAbility when spawning. */
    [Networked] public TickTimer ExpiryTimer { get; set; }

    /** True once teleport has been used (beacon consumed). */
    [Networked] public NetworkBool WasUsed { get; set; }

    #endregion

    #region Fusion Lifecycle

    public override void Spawned()
    {
        if (visual != null) visual.SetActive(true);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (WasUsed) return;

        // Auto-expire beacon if timer runs out before player uses it.
        if (ExpiryTimer.Expired(Runner))
        {
            NotifyOwnerBeaconExpired();
            AbilityFxPlayer.Instance?.PlayFx(AbilityFxEvent.TeleportBeaconExpire, transform.position);
            Runner.Despawn(Object);
        }
    }

    #endregion

    #region Public API

    /**
     * <summary>
     * Consumes this beacon (called by TeleportAbility on teleport).
     * Plays the arrival effect and despawns the object.
     * Must be called on state authority.
     * </summary>
     */
    public void Consume()
    {
        if (!Object.HasStateAuthority) return;
        WasUsed = true;

        Runner.Despawn(Object);
    }

    #endregion

    #region Helpers

    /**
     * <summary>
     * Sends an RPC back to the owning player's AbilityController so the cooldown
     * starts even when the beacon expires without being used.
     * </summary>
     */
    private void NotifyOwnerBeaconExpired()
    {
        // Find the owning player and tell their AbilityController to start cooldown.
        foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (p.Object != null && p.Object.InputAuthority == OwnerRef)
            {
                var ac = p.GetComponent<AbilityController>();
                if (ac != null)
                    RPC_TriggerExpiryCooldown(OwnerRef);
                break;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerExpiryCooldown(PlayerRef ownerRef)
    {
        // On each client, find the owning player and start cooldown if it's the local player.
        foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (p.Object == null || p.Object.InputAuthority != ownerRef) continue;
            if (!p.Object.HasInputAuthority) continue;

            var ac = p.GetComponent<AbilityController>();
            if (ac != null)
                ac.SetCooldown(ac.ActiveAbility?.CooldownDuration ?? 8f);
            break;
        }
    }


    #endregion
}

