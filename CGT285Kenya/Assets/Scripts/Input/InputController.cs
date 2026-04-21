using System;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    [Header("Mobile Input References")]
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private MobileJoystick aimJoystick;
    [SerializeField] private UIButtonClicker abilityButton;

    [Header("Keyboard Input Settings")]
    [SerializeField] private bool useKeyboardInput = true;

    [Header("Obstruction Targeting (Keyboard)")]
    [Tooltip("Camera used to ray-cast mouse clicks for obstruction placement.")]
    [SerializeField] private Camera gameCamera;

    #region Private State

    private Vector2 movementInput;
    private Vector2 rawAimInput;
    private bool ability1Pressed;

    private Vector3 pendingAbilityTapPosition;
    private bool hasPendingAbilityTap;

    private float aimHoldStartTime = -1f;
    private Vector2 lastNonZeroAimDir;

    private bool pendingAimRelease;
    private Vector2 pendingReleaseDirection;
    private float pendingReleaseDuration;

    private Vector2 prevAimInput;

    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
        
        abilityButton.OnClick += () => ability1Pressed = true;
    }

    private void OnDisable()
    {
        abilityButton.OnClick -= () => ability1Pressed = false;
    }

    private void Update()
    {
        ReadRawInput();
        TrackAimHold();
        ReadObstructionTap();
    }

    /**
     * <summary>
     * Reads raw device input every frame.
     * Mobile joystick takes priority over keyboard when active.
     * </summary>
     */
    private void ReadRawInput()
    {
        if (movementJoystick != null && movementJoystick.IsActive)
            movementInput = movementJoystick.Direction;
        else if (useKeyboardInput)
        {
            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.W)) v += 1f;
            if (Input.GetKey(KeyCode.S)) v -= 1f;
            if (Input.GetKey(KeyCode.A)) h -= 1f;
            if (Input.GetKey(KeyCode.D)) h += 1f;
            movementInput = new Vector2(h, v).normalized;
        }
        else
            movementInput = Vector2.zero;

        if (aimJoystick != null && aimJoystick.IsActive)
            rawAimInput = aimJoystick.Direction;
        else if (useKeyboardInput)
        {
            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.UpArrow))    v += 1f;
            if (Input.GetKey(KeyCode.DownArrow))  v -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow))  h -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) h += 1f;
            rawAimInput = new Vector2(h, v).normalized;
        }
        else
            rawAimInput = Vector2.zero;

        if (useKeyboardInput && Input.GetKeyDown(KeyCode.Q))
            ability1Pressed = true;
    }

    /**
     * <summary>
     * Tracks how long the aim stick has been held and latches a release event
     * when the stick returns to neutral.
     * </summary>
     */
    private void TrackAimHold()
    {
        const float deadzone = 0.1f;
        bool aimActive    = rawAimInput.magnitude > deadzone;
        bool prevWasActive = prevAimInput.magnitude > deadzone;

        if (aimActive)
        {
            if (aimHoldStartTime < 0f)
                aimHoldStartTime = Time.time;
            lastNonZeroAimDir = rawAimInput.normalized;
        }
        else if (prevWasActive && !aimActive && aimHoldStartTime >= 0f)
        {
            float duration = Time.time - aimHoldStartTime;
            pendingAimRelease       = true;
            pendingReleaseDirection = lastNonZeroAimDir;
            pendingReleaseDuration  = duration;
            aimHoldStartTime = -1f;
            Debug.Log($"[InputController] Aim released after {duration:F3}s, dir={lastNonZeroAimDir}");
        }

        prevAimInput = rawAimInput;
    }
    
    private void ReadObstructionTap()
    {
        if (!useKeyboardInput) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (gameCamera == null) return;

        // Ray-cast against an infinite horizontal plane at Y=0.
        // This avoids the range indicator disc geometry intercepting the ray.
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            pendingAbilityTapPosition = hitPoint;
            hasPendingAbilityTap      = true;
            Debug.Log($"[InputController] Obstruction tap at {hitPoint}");
        }
    }

    /**
     * <summary>
     * Packages current input into a NetworkInputData for Fusion.
     * Called by NetworkCallbackHandler.OnInput() at tick-rate.
     * Consumes latched aim-release and ability-tap atomically.
     * </summary>
     * <returns>The fully-populated network input struct for this tick.</returns>
     */
    public NetworkInputData GetNetworkInput()
    {
        var data = new NetworkInputData
        {
            MovementInput = movementInput,
            AimInput = rawAimInput,
            AimHoldDuration = pendingAimRelease ? pendingReleaseDuration
                            : (aimHoldStartTime >= 0f ? Time.time - aimHoldStartTime : 0f),
            AimJustReleased= pendingAimRelease,
            LastAimDirection = pendingAimRelease ? pendingReleaseDirection : lastNonZeroAimDir,
            AbilityTapPosition = hasPendingAbilityTap ? pendingAbilityTapPosition : Vector3.zero,
        };

        if (ability1Pressed)
            data.Buttons.Set(InputButton.Ability1, true);

        ability1Pressed = false;

        if (pendingAimRelease)
        {
            pendingAimRelease = false;
            pendingReleaseDirection = Vector2.zero;
            pendingReleaseDuration = 0f;
        }

        hasPendingAbilityTap = false;
        pendingAbilityTapPosition = Vector3.zero;

        return data;
    }

    /** <summary>Public API for mobile UI ability button.</summary>
     * <param name="pressed">Whether the button is pressed.</param>
     */
    public void SetAbility1Button(bool pressed) => ability1Pressed = pressed;

    /**
     * <summary>
     * Submit a world-space tap position for the Obstruction ability.
     * Called by a mobile UI touch handler when the player taps the field.
     * </summary>
     * <param name="worldPosition">World-space position of the tap.</param>
     */
    public void SetAbilityTapPosition(Vector3 worldPosition)
    {
        pendingAbilityTapPosition = worldPosition;
        hasPendingAbilityTap = true;
    }
}
