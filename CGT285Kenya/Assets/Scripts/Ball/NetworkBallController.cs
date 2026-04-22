using UnityEngine;
using Fusion;

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
    private SphereCollider sphereCollider;

    /** The player currently holding the ball, or null when free. */
    [Networked] public NetworkPlayer CurrentHolder { get; private set; }

    /** Ball velocity while free; integrated every tick by state authority. */
    [Networked] public Vector3 BallVelocity { get; private set; }

    [Networked] private TickTimer PickupCooldownTimer { get; set; }

    // Replicated simulation position (written by state authority each tick).
    // Non-authority clients use this as the base for extrapolation.
    [Networked] private Vector3 SimPosition { get; set; }

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
        rb.isKinematic  = true;
        rb.useGravity   = false;
        rb.freezeRotation = true;

        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
            sphereCollider.isTrigger = true;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            Vector3 start = new Vector3(0f, minFloorY, 0f);
            transform.position = start;
            SimPosition    = start;
            BallVelocity   = Vector3.zero;
            CurrentHolder  = null;
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

        // Write the authoritative position into the replicated SimPosition so
        // non-authority clients can use it as an extrapolation base.
        SimPosition = transform.position;
    }
    
    public override void Render()
    {
        if (Object.HasStateAuthority) return;

        if (IsHeld && CurrentHolder != null)
        {
            Vector3 target = CurrentHolder.transform.position
                + CurrentHolder.transform.forward * holdForwardOffset;
            target.y = CurrentHolder.transform.position.y + 0.1f;
            transform.position = target;
        }
        else
        {
            float alpha = Mathf.Clamp01(Time.deltaTime / Runner.DeltaTime);
            Vector3 extrapolated = SimPosition + BallVelocity * (Runner.DeltaTime * alpha);

            if (extrapolated.y < minFloorY)
                extrapolated.y = minFloorY;

            transform.position = extrapolated;
        }
    }

    /**
     * <summary>
     * Snaps the ball to the hold position in front of the current holder.
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
     * </summary>
     */
    private void TickFree()
    {
        float dt = Runner.DeltaTime;

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
        BallVelocity  = Vector3.zero;
        Debug.Log($"[Ball] Pickup by player {picker.Object.InputAuthority.PlayerId}");
    }

    private void AuthorityRelease(Vector3 direction, float speed)
    {
        CurrentHolder = null;
        BallVelocity  = direction.normalized * speed;
        PickupCooldownTimer = TickTimer.CreateFromSeconds(Runner, pickupCooldown);
        Debug.Log($"[Ball] Released speed={speed:F1} dir={direction}");
    }

    /**
     * <summary>
     * Direct release — only valid when this client IS the state authority.
     * </summary>
     * <param name="direction">Normalised world-space direction.</param>
     * <param name="speed">Launch speed in m/s.</param>
     */
    public void Release(Vector3 direction, float speed)
    {
        if (!Object.HasStateAuthority) return;
        AuthorityRelease(direction, speed);
    }

    /**
     * <summary>
     * Resets the ball to a spawn position with zero velocity.
     * Called during field resets after scoring.
     * Only valid on state authority.
     * </summary>
     * <param name="spawnPosition">The position to reset the ball to</param>
     */
     public void ResetToSpawnPosition(Vector3 spawnPosition)
    {
        if (!Object.HasStateAuthority) return;

        CurrentHolder = null;
        BallVelocity = Vector3.zero;
        SimPosition = spawnPosition;
        PickupCooldownTimer = TickTimer.None;
        
        transform.position = spawnPosition;
        Debug.Log($"[NetworkBall] Reset to spawn position: {spawnPosition}");
    }

    /**
     * <summary>
     * RPC to reset the ball to a spawn position.
     * Can be called from any client; will execute on the ball's state authority.
     * </summary>
     * <param name="spawnPosition">The position to reset the ball to</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ResetToSpawnPosition(Vector3 spawnPosition)
    {
        ResetToSpawnPosition(spawnPosition);
    }

    #endregion

    #region RPCs

    /**
     * <summary>
     * Any client can request to pick up a free ball.
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
     * </summary>
     * <param name="thiefRef">PlayerRef of the stealing player.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestSteal(PlayerRef thiefRef)
    {
        if (CurrentHolder == null) return;
        NetworkPlayer thief = ResolvePlayer(thiefRef);
        if (thief == null) return;

        int thiefTeam  = thiefRef.PlayerId <= 2 ? 0 : 1;
        int holderTeam = CurrentHolder.Object.InputAuthority.PlayerId <= 2 ? 0 : 1;
        if (thiefTeam == holderTeam) return;

        Debug.Log($"[Ball] Steal: player {thiefRef.PlayerId} <- player {CurrentHolder.Object.InputAuthority.PlayerId}");
        AuthorityPickup(thief);
    }

    /**
     * <summary>
     * The holding player requests the ball be released (pass or shot).
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
        Vector3 center = new Vector3(0f, minFloorY, 0f);
        transform.position = center;
        SimPosition    = center;
        BallVelocity   = Vector3.zero;
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
