using UnityEngine;
using Fusion;

/**
 * <summary>
 * NetworkBallController manages the shared soccer ball.
 *
 * Fusion Shared Mode authority model:
 *   The ball's NetworkObject is owned by the master client. ALL state changes
 *   route through RPCs targeting StateAuthority so writes always happen on the
 *   correct peer.
 *
 *   HasBall is not stored on the player. Each NetworkPlayer reads
 *   (ball.CurrentHolder == this) — consistent because CurrentHolder is
 *   [Networked] and readable by all clients.
 *
 * Physics model:
 *   The Rigidbody is permanently kinematic. Position is integrated manually
 *   using [Networked] BallVelocity. NetworkTransform handles interpolation
 *   on non-authority clients.
 *
 * Prefab requirements:
 *   Rigidbody (isKinematic=true, useGravity=false, freeze rotation),
 *   NetworkObject, NetworkTransform, SphereCollider.
 * </summary>
 */
[RequireComponent(typeof(Rigidbody))]
public class NetworkBallController : NetworkBehaviour
{
    [Header("Ball Physics")]
    [SerializeField] private float groundDrag = 3.5f;
    [SerializeField] private float airDrag = 0.3f;
    [SerializeField] private float maxSpeed = 28f;
    [SerializeField] private float gravityStrength = 12f;

    [Header("Possession")]
    [SerializeField] private float pickupCooldown = 0.4f;
    [SerializeField] private float holdForwardOffset = 0.9f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDist = 0.6f;
    [SerializeField] private float ballRadius = 0.25f;
    [SerializeField] private float minFloorY = 0.25f;

    #region Components & Networked State

    private Rigidbody rb;

    /** The player currently holding the ball, or null when free. */
    [Networked] public NetworkPlayer CurrentHolder { get; private set; }

    /** Ball velocity while free; integrated every tick by state authority. */
    [Networked] public Vector3 BallVelocity { get; private set; }

    [Networked] private TickTimer PickupCooldownTimer { get; set; }

    #endregion

    #region Derived Properties

    /**
     * <summary>True when the ball has no holder and the cooldown has expired.</summary>
     */
    public bool IsAvailable =>
        Object.IsValid &&
        CurrentHolder == null &&
        PickupCooldownTimer.ExpiredOrNotRunning(Runner);

    /**
     * <summary>True while any player holds the ball.</summary>
     */
    public bool IsHeld => Object.IsValid && CurrentHolder != null;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            transform.position = new Vector3(0f, 0.5f, 0f);
            BallVelocity = Vector3.zero;
            CurrentHolder = null;
        }
    }

    #endregion

    #region Fusion Tick

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (IsHeld)
            TickHeld();
        else
            TickFree();
    }

    /**
     * <summary>
     * Snaps the ball to the hold position in front of the current holder.
     * Computed from the holder's transform on the authority peer so no
     * cross-object [Networked] write is needed.
     * </summary>
     */
    private void TickHeld()
    {
        if (CurrentHolder == null) return;

        Vector3 pos = CurrentHolder.transform.position
            + CurrentHolder.transform.forward * holdForwardOffset;
        pos.y = CurrentHolder.transform.position.y + 0.1f;

        transform.position = pos;
        BallVelocity = Vector3.zero;
    }

    /**
     * <summary>
     * Manually integrates BallVelocity with drag and gravity.
     * Deterministic because it uses Runner.DeltaTime (fixed tick delta).
     * Uses a SphereCast so ground detection works even when the LayerMask is
     * not configured — falls back to a hard floor clamp at minFloorY.
     * </summary>
     */
    private void TickFree()
    {
        float dt = Runner.DeltaTime;

        // SphereCast downward from ball centre. Works even if groundLayer = 0
        // because the hard floor clamp below acts as a last-resort safety net.
        bool grounded = Physics.SphereCast(
            transform.position,
            ballRadius,
            Vector3.down,
            out _,
            groundCheckDist,
            groundLayer == 0 ? ~0 : (int)groundLayer,
            QueryTriggerInteraction.Ignore);

        bool hitHardFloor = transform.position.y <= minFloorY;
        grounded = grounded || hitHardFloor;

        if (!grounded)
        {
            BallVelocity += Vector3.down * gravityStrength * dt;
        }
        else
        {
            if (BallVelocity.y < 0f)
                BallVelocity = new Vector3(BallVelocity.x, 0f, BallVelocity.z);

            if (hitHardFloor)
            {
                Vector3 p = transform.position;
                p.y = minFloorY;
                transform.position = p;
            }
        }

        float drag = grounded ? groundDrag : airDrag;
        Vector3 hVel = new Vector3(BallVelocity.x, 0f, BallVelocity.z);
        hVel = Vector3.MoveTowards(hVel, Vector3.zero, drag * hVel.magnitude * dt);
        BallVelocity = new Vector3(hVel.x, BallVelocity.y, hVel.z);

        float hMag = new Vector2(BallVelocity.x, BallVelocity.z).magnitude;
        if (hMag > maxSpeed)
        {
            float s = maxSpeed / hMag;
            BallVelocity = new Vector3(BallVelocity.x * s, BallVelocity.y, BallVelocity.z * s);
        }

        transform.position += BallVelocity * dt;

        // Belt-and-suspenders floor clamp after position integration.
        if (transform.position.y < minFloorY)
        {
            Vector3 p = transform.position;
            p.y = minFloorY;
            transform.position = p;
            if (BallVelocity.y < 0f)
                BallVelocity = new Vector3(BallVelocity.x, 0f, BallVelocity.z);
        }
    }

    #endregion

    #region Authority Helpers

    private void AuthorityPickup(NetworkPlayer picker)
    {
        CurrentHolder = picker;
        BallVelocity = Vector3.zero;
        Debug.Log($"[Ball] Pickup by player {picker.Object.InputAuthority.PlayerId}");
    }

    private void AuthorityRelease(Vector3 direction, float speed)
    {
        CurrentHolder = null;
        BallVelocity = direction.normalized * speed;
        PickupCooldownTimer = TickTimer.CreateFromSeconds(Runner, pickupCooldown);
        Debug.Log($"[Ball] Released speed={speed:F1} dir={direction}");
    }

    /**
     * <summary>
     * Direct release — only valid when this client IS the state authority.
     * NetworkPlayer calls this path when ball.Object.HasStateAuthority is true.
     * </summary>
     * <param name="direction">Normalised world-space direction.</param>
     * <param name="speed">Launch speed in m/s.</param>
     */
    public void Release(Vector3 direction, float speed)
    {
        if (!Object.HasStateAuthority) return;
        AuthorityRelease(direction, speed);
    }

    #endregion

    #region RPCs

    /**
     * <summary>
     * Any client can request to pick up a free ball.
     * Passes PlayerRef (a blittable int) to avoid Fusion's prefab-lookup path.
     * </summary>
     * <param name="pickerRef">PlayerRef of the requesting player.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestPickup(PlayerRef pickerRef)
    {
        if (!IsAvailable) return;
        NetworkPlayer picker = ResolvePlayer(pickerRef);
        if (picker == null) return;
        AuthorityPickup(picker);
    }

    /**
     * <summary>
     * Any client can request to steal the ball from the current holder.
     * Passes PlayerRef to avoid the prefab-lookup crash when passing NetworkBehaviour refs.
     * </summary>
     * <param name="thiefRef">PlayerRef of the stealing player.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSteal(PlayerRef thiefRef)
    {
        if (CurrentHolder == null) return;
        NetworkPlayer thief = ResolvePlayer(thiefRef);
        if (thief == null) return;

        // Derive team from PlayerId (matches Spawned() assignment) rather than
        // reading the [Networked] Team property, which may not have replicated yet.
        int thiefTeam = thiefRef.PlayerId <= 2 ? 0 : 1;
        int holderTeam = CurrentHolder.Object.InputAuthority.PlayerId <= 2 ? 0 : 1;
        if (thiefTeam == holderTeam) return;

        Debug.Log($"[Ball] Steal: player {thiefRef.PlayerId} <- player {CurrentHolder.Object.InputAuthority.PlayerId}");
        AuthorityPickup(thief);
    }

    /**
     * <summary>
     * The holding player requests the ball be released (pass or shot).
     * Passes PlayerRef to avoid the prefab-lookup crash.
     * </summary>
     * <param name="releaserRef">PlayerRef of the releasing player.</param>
     * <param name="direction">Normalised world-space launch direction.</param>
     * <param name="speed">Launch speed in m/s.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestRelease(PlayerRef releaserRef, Vector3 direction, float speed)
    {
        NetworkPlayer releaser = ResolvePlayer(releaserRef);
        if (releaser == null || CurrentHolder != releaser) return;
        AuthorityRelease(direction, speed);
    }

    /**
     * <summary>
     * Finds the NetworkPlayer whose InputAuthority matches the given PlayerRef.
     * Only called on the state authority, so FindObjectsByType overhead is acceptable.
     * </summary>
     * <param name="playerRef">The PlayerRef to look up.</param>
     * <returns>The matching NetworkPlayer, or null if not found.</returns>
     */
    private NetworkPlayer ResolvePlayer(PlayerRef playerRef)
    {
        foreach (var p in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None))
        {
            if (p.Object != null && p.Object.InputAuthority == playerRef)
                return p;
        }
        return null;
    }

    #endregion

    #region Goal Handling

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("Goal"))
        {
            var goal = other.GetComponent<GoalTrigger>();
            if (goal != null) HandleGoal(goal.Team);
        }
    }

    private void HandleGoal(int scoringTeam)
    {
        Debug.Log($"[Ball] GOAL — team {scoringTeam} scored!");

        CurrentHolder = null;
        transform.position = new Vector3(0f, 0.5f, 0f);
        BallVelocity = Vector3.zero;
        PickupCooldownTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);

        GameManager.Instance?.OnGoalScored(scoringTeam);
    }

    #endregion

    #region Editor Helpers

    private void OnDrawGizmos()
    {
        if (Object == null || !Object.IsValid) return;

        Gizmos.color = IsHeld ? Color.yellow : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        if (IsHeld && CurrentHolder != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, CurrentHolder.transform.position);
        }
    }

    #endregion
}
