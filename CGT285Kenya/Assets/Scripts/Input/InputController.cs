using UnityEngine;

/**
 * <summary>
 * InputController is a singleton that manages local player input.
 * It supports both mobile touch controls (virtual joysticks) and keyboard input,
 * packaging everything into a NetworkInputData struct for Fusion.
 *
 * Aim / shoot design:
 *   ReadRawInput() accumulates raw aim state every Update().
 *   GetNetworkInput() is called by Fusion's OnInput callback at tick-rate and
 *   performs release detection atomically, so AimJustReleased appears in
 *   exactly one network tick and is never dropped.
 * </summary>
 */
public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    [Header("Mobile Input References")]
    [SerializeField] private MobileJoystick movementJoystick;
    [SerializeField] private MobileJoystick aimJoystick;

    [Header("Keyboard Input Settings")]
    [SerializeField] private bool useKeyboardInput = true;

    #region Private State

    private Vector2 movementInput;
    private Vector2 rawAimInput;
    private bool ability1Pressed;

    private float aimHoldStartTime = -1f;
    private Vector2 lastNonZeroAimDir;

    // Latched release — persists until GetNetworkInput consumes it so the event
    // is never lost between an Update() and the next OnInput() tick.
    private bool pendingAimRelease;
    private Vector2 pendingReleaseDirection;
    private float pendingReleaseDuration; // captured at release time; aimHoldStartTime is -1 by then

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

    private void Update()
    {
        ReadRawInput();
        TrackAimHold();
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
        {
            movementInput = movementJoystick.Direction;
        }
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
        {
            movementInput = Vector2.zero;
        }

        if (aimJoystick != null && aimJoystick.IsActive)
        {
            rawAimInput = aimJoystick.Direction;
        }
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
        {
            rawAimInput = Vector2.zero;
        }

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
        bool aimActive = rawAimInput.magnitude > deadzone;
        bool prevWasActive = prevAimInput.magnitude > deadzone;

        if (aimActive)
        {
            if (aimHoldStartTime < 0f)
                aimHoldStartTime = Time.time;

            lastNonZeroAimDir = rawAimInput.normalized;
        }
        else if (prevWasActive && !aimActive)
        {
            if (aimHoldStartTime >= 0f)
            {
                float duration = Time.time - aimHoldStartTime;
                pendingAimRelease = true;
                pendingReleaseDirection = lastNonZeroAimDir;
                pendingReleaseDuration = duration; // must capture here; aimHoldStartTime reset below
                aimHoldStartTime = -1f;
                Debug.Log($"[InputController] Aim released after {duration:F3}s, dir={lastNonZeroAimDir}");
            }
        }

        prevAimInput = rawAimInput;
    }

    /**
     * <summary>
     * Packages current input into a NetworkInputData for Fusion.
     * Called by NetworkCallbackHandler.OnInput() at tick-rate.
     * Consumes the latched aim-release atomically.
     * </summary>
     * <returns>The fully-populated network input struct for this tick.</returns>
     */
    public NetworkInputData GetNetworkInput()
    {
        var data = new NetworkInputData
        {
            MovementInput = movementInput,
            AimInput = rawAimInput,
            // While aiming: live duration. On the release tick: use the captured latch value,
            // because aimHoldStartTime is already -1 when AimJustReleased is true.
            AimHoldDuration = pendingAimRelease ? pendingReleaseDuration
                              : (aimHoldStartTime >= 0f ? Time.time - aimHoldStartTime : 0f),
            AimJustReleased = pendingAimRelease,
            LastAimDirection = pendingAimRelease ? pendingReleaseDirection : lastNonZeroAimDir,
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

        return data;
    }

    /**
     * <summary>Public API for mobile UI ability button.</summary>
     * <param name="pressed">Whether the button is pressed.</param>
     */
    public void SetAbility1Button(bool pressed) => ability1Pressed = pressed;
}
