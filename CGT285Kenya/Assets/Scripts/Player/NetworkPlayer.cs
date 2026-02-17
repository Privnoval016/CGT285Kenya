using UnityEngine;
using Fusion;

/**
 * NetworkPlayer is the main networked player controller.
 * It handles movement, ball possession, and ability execution.
 * 
 * Key Fusion Concepts:
 * - Inherits from NetworkBehaviour (similar to MonoBehaviour but networked)
 * - Uses [Networked] attribute for synchronized properties
 * - GetInput() retrieves input from the network for this player
 * - FixedUpdateNetwork() is Fusion's equivalent of FixedUpdate, runs on all clients for prediction
 * 
 * IMPORTANT: Add NetworkTransform component in Inspector for smooth remote player movement!
 */
[RequireComponent(typeof(CharacterController))]
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float gravity = 20f;
    
    [Header("Ball Interaction")]
    [SerializeField] private Transform ballHoldPosition;
    [SerializeField] private float ballPickupRange = 1.5f;
    [SerializeField] private float ballStealRange = 1.2f;
    
    [Header("Shooting Settings")]
    [SerializeField] private float passSpeed = 10f;
    [SerializeField] private float shotSpeed = 20f;
    [SerializeField] private float shotChargeThreshold = 0.3f;
    
    [Header("Team Visuals")]
    [SerializeField] private Material playerMaterial;
    [SerializeField] private Color team0Color = Color.blue;
    [SerializeField] private Color team1Color = Color.red;
    [SerializeField] private MeshRenderer visualMesh;
    
    // Components
    private CharacterController characterController;
    private NetworkBallController ball;
    private AbilityController abilityController;
    private Material materialInstance;
    
    // Networked properties - these are automatically synchronized by Fusion
    [Networked] public bool HasBall { get; set; }
    [Networked] public int Team { get; set; }
    [Networked] private Vector3 NetworkedVelocity { get; set; }
    
    // Local cached values
    private Vector3 moveDirection;
    private Vector3 lookDirection;
    private float verticalVelocity;
    private int lastTeam = -1; // Track team changes locally

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        abilityController = GetComponent<AbilityController>();
    }

    /**
     * <summary>
     * Called when the object is spawned on the network.
     * This is Fusion's version of Start() for networked objects.
     * </summary>
     */
    public override void Spawned()
    {
        // Find the ball in the scene
        ball = FindFirstObjectByType<NetworkBallController>();
        
        // Assign team based on player ID (only state authority sets this)
        if (Object.HasStateAuthority)
        {
            Team = Object.InputAuthority.PlayerId < 3 ? 0 : 1;
            Debug.Log($"[NetworkPlayer] State authority assigned Player {Object.InputAuthority.PlayerId} to Team {Team}");
        }
        
        // Setup team colors
        lastTeam = Team;
        SetupTeamColor();
        
        // Setup ball hold position if not set
        if (ballHoldPosition == null)
        {
            var holdPos = new GameObject("BallHoldPosition");
            holdPos.transform.SetParent(transform);
            holdPos.transform.localPosition = new Vector3(0, 0, 0.8f);
            ballHoldPosition = holdPos.transform;
        }
    }
    
    /**
     * <summary>
     * Called every frame for rendering updates.
     * Check if team changed and update colors.
     * </summary>
     */
    public override void Render()
    {
        // Check if team has changed since last frame
        if (Team != lastTeam)
        {
            lastTeam = Team;
            SetupTeamColor();
            Debug.Log($"[NetworkPlayer] Team changed for Player {Object.InputAuthority.PlayerId} to Team {Team}");
        }
    }
    
    /**
     * <summary>
     * Sets up the team color for this player based on their team assignment.
     * Creates a material instance to avoid affecting the prefab material.
     * </summary>
     */
    private void SetupTeamColor()
    {
        // Find the visual mesh if not assigned
        if (visualMesh == null)
        {
            // Look for a child named "Visual" or just get the first MeshRenderer
            Transform visualChild = transform.Find("Visual");
            if (visualChild != null)
            {
                visualMesh = visualChild.GetComponent<MeshRenderer>();
            }
            
            if (visualMesh == null)
            {
                visualMesh = GetComponentInChildren<MeshRenderer>();
            }
        }
        
        if (visualMesh != null)
        {
            // Use existing material if playerMaterial not assigned
            if (playerMaterial == null)
            {
                playerMaterial = visualMesh.sharedMaterial;
            }
            
            if (playerMaterial != null)
            {
                // Create material instance
                materialInstance = new Material(playerMaterial);
                visualMesh.material = materialInstance;
                
                // Set color based on team
                Color teamColor = Team == 0 ? team0Color : team1Color;
                materialInstance.color = teamColor;
                
                Debug.Log($"[NetworkPlayer] Player {Object.InputAuthority.PlayerId} set to Team {Team} with color {teamColor}");
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayer] No material found for Player {Object.InputAuthority.PlayerId}");
            }
        }
        else
        {
            Debug.LogWarning($"[NetworkPlayer] No MeshRenderer found in children for Player {Object.InputAuthority.PlayerId}");
        }
    }

    /// <summary>
    /// FixedUpdateNetwork is called for every network tick (default 60Hz).
    /// This runs on all clients for client-side prediction and on server for authority.
    /// Input is automatically rewound and replayed for proper lag compensation.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // Only process input for this player if we have input authority
        if (GetInput(out NetworkInputData input))
        {
            // Process movement
            ProcessMovement(input);
            
            // Process rotation
            ProcessRotation(input);
            
            // Process ball interactions
            ProcessBallInteractions(input);
            
            // Process ability (single ability only)
            ProcessAbility(input);
        }
        
        // Update ball position if we're holding it
        if (HasBall && ball != null)
        {
            UpdateBallPosition();
        }
    }

    private void ProcessMovement(NetworkInputData input)
    {
        // Calculate movement direction
        Vector3 inputDirection = new Vector3(input.MovementInput.x, 0, input.MovementInput.y);
        
        // Apply camera-relative movement (top-down camera)
        moveDirection = inputDirection.normalized;
        
        // Calculate horizontal movement
        Vector3 movement = moveDirection * moveSpeed * Runner.DeltaTime;
        
        // Handle gravity properly
        if (characterController.isGrounded)
        {
            // Reset vertical velocity when grounded
            verticalVelocity = -2f; // Small downward force to keep grounded
        }
        else
        {
            // Apply gravity when in air
            verticalVelocity -= gravity * Runner.DeltaTime;
        }
        
        // Apply vertical velocity
        movement.y = verticalVelocity * Runner.DeltaTime;
        
        // Move the character
        characterController.Move(movement);
    }

    private void ProcessRotation(NetworkInputData input)
    {
        // Determine look direction based on input priority:
        // 1. Aim joystick (if active)
        // 2. Movement direction (if moving)
        Vector3 lookDir = Vector3.zero;
        
        if (input.AimInput.magnitude > 0.1f)
        {
            lookDir = new Vector3(input.AimInput.x, 0, input.AimInput.y);
        }
        else if (moveDirection.magnitude > 0.1f)
        {
            lookDir = moveDirection;
        }
        
        if (lookDir.magnitude > 0.1f)
        {
            lookDirection = lookDir.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Runner.DeltaTime
            );
        }
    }

    private void ProcessBallInteractions(NetworkInputData input)
    {
        if (ball == null) return;
        
        // Check for ball pickup
        if (!HasBall && ball.IsAvailable)
        {
            float distanceToBall = Vector3.Distance(transform.position, ball.transform.position);
            
            if (distanceToBall <= ballPickupRange)
            {
                // Automatically pick up ball when close
                PickupBall();
            }
        }
        
        // Check for ball steal from opponent
        if (!HasBall && ball.CurrentHolder != null && ball.CurrentHolder != this)
        {
            // Only steal from opposite team
            if (ball.CurrentHolder.Team == this.Team)
                return;
            
            // Check if we're facing the opponent and within steal range
            float distanceToHolder = Vector3.Distance(transform.position, ball.CurrentHolder.transform.position);
            Vector3 directionToHolder = (ball.CurrentHolder.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, directionToHolder);
            
            // Steal if we're facing them (dot > 0.7) and close enough
            if (distanceToHolder <= ballStealRange && dot > 0.7f)
            {
                Debug.Log($"[NetworkPlayer] Player {Object.InputAuthority.PlayerId} stealing ball from Player {ball.CurrentHolder.Object.InputAuthority.PlayerId}");
                StealBall();
            }
        }
        
        // Handle shooting/passing when aim is released
        if (HasBall && input.AimJustReleased && input.LastAimDirection.magnitude > 0.1f)
        {
            // Determine if it's a pass or shot based on hold duration
            bool isShot = input.AimHoldDuration >= shotChargeThreshold;
            
            if (isShot)
            {
                // Long hold = shot
                ShootBall(input.LastAimDirection, shotSpeed);
            }
            else
            {
                // Quick tap = pass
                PassBall(input.LastAimDirection);
            }
        }
    }

    private void ProcessAbility(NetworkInputData input)
    {
        // Only execute ability if we have the ability controller
        if (abilityController == null) return;
        
        // Single ability execution
        if (input.Buttons.IsSet(InputButton.Ability1))
        {
            abilityController.ExecuteAbility(0);
        }
    }

    private void UpdateBallPosition()
    {
        if (ballHoldPosition != null)
        {
            ball.SetHoldPosition(ballHoldPosition.position);
        }
    }

    private void PickupBall()
    {
        if (ball != null && Object.HasStateAuthority)
        {
            ball.SetHolder(this);
            HasBall = true;
        }
    }

    private void StealBall()
    {
        if (ball != null && Object.HasStateAuthority)
        {
            var previousHolder = ball.CurrentHolder;
            if (previousHolder != null)
            {
                previousHolder.HasBall = false;
            }
            
            ball.SetHolder(this);
            HasBall = true;
        }
    }

    private void PassBall(Vector2 direction)
    {
        if (ball != null && Object.HasStateAuthority)
        {
            Vector3 passDirection = new Vector3(direction.x, 0, direction.y).normalized;
            ball.Release(passDirection, passSpeed);
            HasBall = false;
        }
    }

    private void ShootBall(Vector2 direction, float speed)
    {
        if (ball != null && Object.HasStateAuthority)
        {
            Vector3 shootDirection = new Vector3(direction.x, 0, direction.y).normalized;
            ball.Release(shootDirection, speed);
            HasBall = false;
        }
    }
}

