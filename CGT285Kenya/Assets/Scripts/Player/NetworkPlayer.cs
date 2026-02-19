using UnityEngine;
using Fusion;

/**
 * <summary>
 * NetworkPlayer is the main networked player controller.
 *
 * Fusion Shared Mode:
 *   Each player's NetworkObject is owned by that player's client.
 *   GetInput() delivers input only on the owning client (HasInputAuthority).
 *   [Networked] property writes on the player only work on its own client.
 *
 * Possession model:
 *   HasBall is a derived property — returns (ball.CurrentHolder == this).
 *   The ball's [Networked] CurrentHolder is the single source of truth.
 *
 * Shoot / pass:
 *   Right-stick tap (hold less than shotChargeThreshold) → pass at passSpeed.
 *   Right-stick hold then release (hold >= shotChargeThreshold) → shot at shotSpeed.
 * </summary>
 */
[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = 20f;

    [Header("Ball Interaction")]
    [SerializeField] private float ballPickupRange = 1.5f;
    [SerializeField] private float ballStealRange = 1.3f;
    [SerializeField] private float stealDotThreshold = 0.65f;

    [Header("Shooting")]
    [SerializeField] private float passSpeed = 12f;
    [SerializeField] private float shotSpeed = 22f;
    [SerializeField] private float shotChargeThreshold = 0.25f;

    [Header("Team Visuals")]
    [SerializeField] private Color team0Color = Color.blue;
    [SerializeField] private Color team1Color = Color.red;
    [SerializeField] private MeshRenderer visualMesh;

    #region Components & State

    private CharacterController cc;
    private NetworkBallController ball;
    private AbilityController abilityController;
    private Material materialInstance;

    [Networked] public int Team { get; set; }

    /**
     * <summary>
     * True when this player holds the ball.
     * Derived from ball.CurrentHolder to avoid cross-object authority writes.
     * </summary>
     */
    public bool HasBall => Ball != null && Ball.Object.IsValid && Ball.CurrentHolder == this;

    // Lazy accessor — re-searches the scene if the cached reference is null.
    // The ball is spawned by the master client and may not exist yet when
    // this player's Spawned() fires on a late-joining client.
    private NetworkBallController Ball
    {
        get
        {
            if (ball == null)
                ball = FindFirstObjectByType<NetworkBallController>();
            return ball;
        }
    }

    private Vector3 moveDir;
    private float verticalVelocity;
    private int prevTeam = -1;

    // Throttle RPC sends — only re-send pickup/steal every few ticks.
    private int lastPickupRpcTick = -100;
    private int lastStealRpcTick = -100;
    private const int RpcResendInterval = 6;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        abilityController = GetComponent<AbilityController>();
    }

    #endregion

    #region Fusion Lifecycle

    /**
     * <summary>
     * Called once when the networked object is spawned on this client.
     * Assigns team and sets up visuals.
     * </summary>
     */
    public override void Spawned()
    {
        // Eagerly cache; Ball property will re-search lazily if this returns null.
        ball = FindFirstObjectByType<NetworkBallController>();

        if (Object.HasStateAuthority)
        {
            Team = (Object.InputAuthority.PlayerId <= 2) ? 0 : 1;
            Debug.Log($"[Player] player {Object.InputAuthority.PlayerId} → team {Team}");
        }

        prevTeam = Team;
        SetupTeamColor();
    }

    /**
     * <summary>
     * Called every render frame. Used for cosmetic reactions to networked
     * property changes (team color) without touching simulation state.
     * </summary>
     */
    public override void Render()
    {
        if (Team != prevTeam)
        {
            prevTeam = Team;
            SetupTeamColor();
        }
    }

    /**
     * <summary>
     * Deterministic simulation tick. Input is rewound and replayed by Fusion
     * for client-side prediction.
     * </summary>
     */
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        ProcessMovement(input);
        ProcessRotation(input);
        ProcessBallInteractions(input);
        ProcessAbility(input);
    }

    #endregion

    #region Movement

    private void ProcessMovement(NetworkInputData input)
    {
        moveDir = new Vector3(input.MovementInput.x, 0f, input.MovementInput.y).normalized;

        Vector3 move = moveDir * moveSpeed * Runner.DeltaTime;

        if (cc.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity -= gravity * Runner.DeltaTime;

        move.y = verticalVelocity * Runner.DeltaTime;
        cc.Move(move);
    }

    private void ProcessRotation(NetworkInputData input)
    {
        Vector3 lookDir;

        if (input.AimInput.magnitude > 0.1f)
            lookDir = new Vector3(input.AimInput.x, 0f, input.AimInput.y);
        else if (moveDir.magnitude > 0.1f)
            lookDir = moveDir;
        else
            return;

        Quaternion target = Quaternion.LookRotation(lookDir.normalized);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, target, rotationSpeed * Runner.DeltaTime);
    }

    #endregion

    #region Ball Interactions

    private void ProcessBallInteractions(NetworkInputData input)
    {
        NetworkBallController b = Ball;
        if (b == null || !b.Object.IsValid) return;

        if (!HasBall && b.IsAvailable)
        {
            float dist = Vector3.Distance(transform.position, b.transform.position);
            int tick = Runner.Tick;
            if (dist <= ballPickupRange && tick - lastPickupRpcTick > RpcResendInterval)
            {
                lastPickupRpcTick = tick;
                b.RPC_RequestPickup(Object.InputAuthority);
            }
        }

        // Steal: fires when this player enters the space in front of the holder
        // (the steal zone is defined from the holder's perspective, not the thief's).
        if (!HasBall && b.IsHeld &&
            b.CurrentHolder != null && b.CurrentHolder != this &&
            b.CurrentHolder.Team != Team)
        {
            Vector3 holderToThief = transform.position - b.CurrentHolder.transform.position;
            float dist = holderToThief.magnitude;
            float dot = Vector3.Dot(b.CurrentHolder.transform.forward, holderToThief.normalized);
            int tick = Runner.Tick;

            if (dist <= ballStealRange && dot >= stealDotThreshold &&
                tick - lastStealRpcTick > RpcResendInterval)
            {
                lastStealRpcTick = tick;
                Debug.Log($"[Player] Attempting steal — dist={dist:F2} dot={dot:F2}");
                b.RPC_RequestSteal(Object.InputAuthority);
            }
        }

        if (HasBall && input.AimJustReleased && input.LastAimDirection.magnitude > 0.1f)
        {
            bool isShot = input.AimHoldDuration >= shotChargeThreshold;
            float speed = isShot ? shotSpeed : passSpeed;
            Vector3 dir = new Vector3(
                input.LastAimDirection.x, 0f, input.LastAimDirection.y).normalized;

            ReleaseBall(dir, speed);

            Debug.Log($"[Player] {(isShot ? "SHOT" : "PASS")} " +
                $"holdDuration={input.AimHoldDuration:F3}s speed={speed:F1}");
        }
    }

    /**
     * <summary>
     * Routes a ball release to the correct path.
     * Direct call if this client is the ball's state authority; RPC otherwise.
     * </summary>
     * <param name="direction">Normalised world-space launch direction.</param>
     * <param name="speed">Launch speed in m/s.</param>
     */
    private void ReleaseBall(Vector3 direction, float speed)
    {
        NetworkBallController b = Ball;
        if (b == null) return;

        if (b.Object.HasStateAuthority)
            b.Release(direction, speed);
        else
            b.RPC_RequestRelease(Object.InputAuthority, direction, speed);
    }

    #endregion

    #region Ability

    private void ProcessAbility(NetworkInputData input)
    {
        if (abilityController == null) return;

        if (input.Buttons.IsSet(InputButton.Ability1))
            abilityController.ExecuteAbility(0);
    }

    #endregion

    #region Team Color

    /**
     * <summary>
     * Creates a per-instance material copy and applies the team color.
     * Called on Spawned() and when the Team property changes.
     * </summary>
     */
    private void SetupTeamColor()
    {
        if (visualMesh == null)
        {
            Transform child = transform.Find("Visual");
            if (child != null) visualMesh = child.GetComponent<MeshRenderer>();
            if (visualMesh == null) visualMesh = GetComponentInChildren<MeshRenderer>();
        }

        if (visualMesh == null)
        {
            Debug.LogWarning($"[NetworkPlayer] No MeshRenderer on player {Object.InputAuthority.PlayerId}");
            return;
        }

        if (visualMesh.sharedMaterial == null)
        {
            Debug.LogWarning($"[NetworkPlayer] No shared material on player {Object.InputAuthority.PlayerId}");
            return;
        }

        if (materialInstance == null)
            materialInstance = new Material(visualMesh.sharedMaterial);

        materialInstance.color = Team == 0 ? team0Color : team1Color;
        visualMesh.material = materialInstance;
    }

    #endregion
}
