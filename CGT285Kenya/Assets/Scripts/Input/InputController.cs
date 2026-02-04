using UnityEngine;

/**
 * InputController is a singleton that manages local player input.
 * It supports both mobile touch controls (virtual joysticks) and keyboard input.
 * This class reads input and packages it into NetworkInputData for Fusion.
 * 
 * Pattern: Singleton for easy access from anywhere
 */
public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    [Header("Mobile Input References")]
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private MobileJoystick aimJoystick;
    
    [Header("Keyboard Input Settings")]
    [SerializeField] private bool useKeyboardInput = true;
    
    [Header("Input Smoothing")]
    [SerializeField] private float inputSmoothing = 0.1f;
    
    private Vector2 movementInput;
    private Vector2 aimInput;
    private Vector2 previousAimInput;
    private Vector2 lastAimDirection;
    private float aimHoldStartTime = -1f;
    private float aimReleaseDuration = -1f;
    private bool aimWasReleased;
    
    private bool actionButtonPressed;
    private bool ability1ButtonPressed;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        ReadInput();
        UpdateAimHoldDuration();
    }

    /**
     * Reads input from both mobile joysticks and keyboard.
     * Mobile takes priority if joysticks are active.
     */
    private void ReadInput()
    {
        // Movement Input
        if (movementJoystick != null && movementJoystick.IsActive)
        {
            movementInput = movementJoystick.Direction;
        }
        else if (useKeyboardInput)
        {
            float horizontal = 0f;
            float vertical = 0f;
            
            if (Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(KeyCode.S)) vertical -= 1f;
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;
            
            movementInput = new Vector2(horizontal, vertical).normalized;
        }
        
        // Aim Input
        if (aimJoystick != null && aimJoystick.IsActive)
        {
            aimInput = aimJoystick.Direction;
        }
        else if (useKeyboardInput)
        {
            float horizontal = 0f;
            float vertical = 0f;
            
            if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            
            aimInput = new Vector2(horizontal, vertical).normalized;
        }
        
        // Button Input
        if (useKeyboardInput)
        {
            if (Input.GetKeyDown(KeyCode.Space)) actionButtonPressed = true;
            if (Input.GetKeyDown(KeyCode.Q)) ability1ButtonPressed = true;
        }
    }

    /**
     * <summary>
     * Tracks how long the aim joystick has been held.
     * This determines pass vs shot behavior.
     * </summary>
     */
    private void UpdateAimHoldDuration()
    {
        if (aimInput.magnitude > 0.1f)
        {
            // Aim is being held
            if (aimHoldStartTime < 0)
            {
                aimHoldStartTime = Time.time;
            }
            lastAimDirection = aimInput; // Store direction while holding
            aimWasReleased = false;
        }
        else if (previousAimInput.magnitude > 0.1f)
        {
            // Aim was just released this frame
            if (aimHoldStartTime >= 0)
            {
                aimReleaseDuration = Time.time - aimHoldStartTime;
                aimWasReleased = true;
                aimHoldStartTime = -1f;
                Debug.Log($"[InputController] Aim released after {aimReleaseDuration}s in direction {lastAimDirection}");
            }
        }
        
        previousAimInput = aimInput;
    }

    /**
     * <summary>
     * Packages current input state into NetworkInputData for Fusion.
     * This is called by NetworkCallbackHandler.OnInput().
     * </summary>
     * <returns>The network input data structure</returns>
     */
    public NetworkInputData GetNetworkInput()
    {
        var input = new NetworkInputData
        {
            MovementInput = movementInput,
            AimInput = aimInput,
            AimHoldDuration = aimHoldStartTime >= 0 ? Time.time - aimHoldStartTime : 0f,
            AimJustReleased = aimWasReleased,
            LastAimDirection = lastAimDirection
        };
        
        if (actionButtonPressed)
            input.Buttons.Set(InputButton.Action, true);
        if (ability1ButtonPressed)
            input.Buttons.Set(InputButton.Ability1, true);
        
        // Reset button states after reading
        actionButtonPressed = false;
        ability1ButtonPressed = false;
        
        // Consume aim release after sending once
        if (aimWasReleased)
        {
            aimWasReleased = false;
            aimReleaseDuration = -1f;
        }
        
        return input;
    }

    /**
     * Public API for mobile UI buttons to set button states
     */
    public void SetActionButton(bool pressed) => actionButtonPressed = pressed;
    public void SetAbility1Button(bool pressed) => ability1ButtonPressed = pressed;
}

