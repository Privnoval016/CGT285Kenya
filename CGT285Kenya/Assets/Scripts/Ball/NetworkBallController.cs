using UnityEngine;
using Fusion;

/**
 * NetworkBallController manages the shared soccer ball in the multiplayer game.
 * The ball is a single networked object that all players can interact with.
 * 
 * Key behaviors:
 * - Ball can be "held" by a player (follows them)
 * - Ball can be "free" (physics-based movement)
 * - Ball ownership/state is authoritative on the server
 * 
 * Fusion Patterns:
 * - Uses State Authority (server controls ball state)
 * - Requires NetworkTransform or NetworkRigidbody for position synchronization
 * - Uses TickTimer for time-based mechanics
 * 
 * IMPORTANT: Add NetworkTransform component in the Inspector!
 */
[RequireComponent(typeof(Rigidbody))]
public class NetworkBallController : NetworkBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float _groundDrag = 2f;
    [SerializeField] private float _airDrag = 0.5f;
    [SerializeField] private float _maxSpeed = 25f;
    [SerializeField] private float _pickupCooldown = 0.5f;
    
    [Header("Physics")]
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckDistance = 0.2f;
    
    // Components
    private Rigidbody _rigidbody;
    
    // Networked state
    [Networked] public NetworkPlayer CurrentHolder { get; set; }
    [Networked] private TickTimer PickupCooldownTimer { get; set; }
    [Networked] private Vector3 HoldPosition { get; set; }
    
    // Properties
    public bool IsAvailable => CurrentHolder == null && PickupCooldownTimer.ExpiredOrNotRunning(Runner);
    public bool IsHeld => CurrentHolder != null;
    
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void Spawned()
    {
        // Initialize ball at center of field
        if (Object.HasStateAuthority)
        {
            transform.position = new Vector3(0, 0.5f, 0);
            CurrentHolder = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Only the state authority (server/master client) controls ball physics
        if (!Object.HasStateAuthority)
            return;
            
        // Update ball behavior based on state
        if (IsHeld)
        {
            UpdateHeldBehavior();
        }
        else
        {
            UpdateFreeBehavior();
        }
    }

    /**
     * When held by a player, the ball follows them smoothly
     */
    private void UpdateHeldBehavior()
    {
        if (CurrentHolder == null) return;
        
        // Disable physics while held
        if (!_rigidbody.isKinematic)
        {
            // Reset velocities BEFORE making kinematic
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }
        
        // Directly set position to hold position (no lerp needed, NetworkTransform handles smoothing)
        transform.position = HoldPosition;
    }

    /**
     * When free, the ball uses physics and gradually slows down
     */
    private void UpdateFreeBehavior()
    {
        // Enable physics when free
        if (_rigidbody.isKinematic)
        {
            _rigidbody.isKinematic = false;
        }
        
        // Apply drag based on whether ball is grounded
        bool isGrounded = IsGrounded();
        _rigidbody.linearDamping = isGrounded ? _groundDrag : _airDrag;
        
        // Clamp velocity
        if (_rigidbody.linearVelocity.magnitude > _maxSpeed)
        {
            _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _maxSpeed;
        }
    }

    /// <summary>
    /// Sets the player who is holding the ball.
    /// Only callable by state authority (server).
    /// </summary>
    public void SetHolder(NetworkPlayer player)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("Only state authority can set ball holder!");
            return;
        }
        
        CurrentHolder = player;
        
        if (player != null)
        {
            Debug.Log($"Player {player.Object.InputAuthority.PlayerId} picked up the ball");
        }
    }

    /// <summary>
    /// Sets the position where the ball should be held (in front of player)
    /// </summary>
    public void SetHoldPosition(Vector3 position)
    {
        HoldPosition = position;
    }

    /// <summary>
    /// Releases the ball with velocity (pass or shot)
    /// </summary>
    public void Release(Vector3 direction, float speed)
    {
        if (!Object.HasStateAuthority)
        {
            Debug.LogWarning("Only state authority can release ball!");
            return;
        }
        
        CurrentHolder = null;
        
        // Start pickup cooldown to prevent immediate re-pickup
        PickupCooldownTimer = TickTimer.CreateFromSeconds(Runner, _pickupCooldown);
        
        // Apply velocity
        _rigidbody.linearVelocity = direction * speed;
        
        Debug.Log($"Ball released with speed {speed}");
    }

    /// <summary>
    /// Checks if ball is on the ground
    /// </summary>
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, _groundCheckDistance, _groundLayer);
    }

    /// <summary>
    /// Called when the ball enters a trigger (for goal detection)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        
        // Check for goal
        if (other.CompareTag("Goal"))
        {
            var goal = other.GetComponent<GoalTrigger>();
            if (goal != null)
            {
                HandleGoal(goal.Team);
            }
        }
    }

    private void HandleGoal(int scoringTeam)
    {
        Debug.Log($"GOAL! Team {scoringTeam} scored!");
        
        // Reset ball to center
        transform.position = new Vector3(0, 0.5f, 0);
        _rigidbody.linearVelocity = Vector3.zero;
        CurrentHolder = null;
        
        // Notify game manager
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGoalScored(scoringTeam);
        }
    }

    /**
 * Draws debug gizmos in the editor for visualization
 */
    private void OnDrawGizmos()
    {
        // Only access networked properties when the object is spawned
        if (Object == null || !Object.IsValid)
            return;
        
        // Draw pickup range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 1.5f);
    
        // Draw possession indicator
        if (IsHeld)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, CurrentHolder.transform.position);
        }
    }

}

