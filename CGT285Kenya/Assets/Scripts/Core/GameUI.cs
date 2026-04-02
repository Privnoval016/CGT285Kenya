using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

/**
 * <summary>
 * GameUI manages the in-game HUD display.
 * Shows score, timer, and ability cooldown.
 * </summary>
 */
public class GameUI : MonoBehaviour
{
    [Header("Score Display")]
    [SerializeField] private TextMeshProUGUI scoreText;
    
    [Header("Timer Display")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Ability Display")] [SerializeField]
    private UIButtonClicker abilityButton;
    
    [Header("Connection Status")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    
    private GameManager gameManager;
    private NetworkPlayer localPlayer;
    public AbilityController localAbilityController;
    
    /**
     * <summary>
     * Initialize by finding the GameManager instance.
     * </summary>
     */
    private void Start()
    {
        gameManager = GameManager.Instance;
    }

    /**
     * <summary>
     * Update all UI elements every frame.
     * </summary>
     */
    private void Update()
    {
        // Find local player if we don't have one
        if (localPlayer == null)
        {
            FindLocalPlayer();
        }
        
        UpdateScoreDisplay();
        UpdateTimerDisplay();
        UpdateAbilityDisplay();
        UpdateConnectionStatus();
    }

    /**
     * <summary>
     * Finds the local player by checking which player has input authority.
     * </summary>
     */
    private void FindLocalPlayer()
    {
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                localPlayer = player;
                localAbilityController = player.GetComponent<AbilityController>();
                Debug.Log($"[GameUI] Found local player: Player {player.Object.InputAuthority.PlayerId}");
                break;
            }
        }
    }

    /**
     * <summary>
     * Updates the score display text.
     * </summary>
     */
    private void UpdateScoreDisplay()
    {
        if (gameManager != null && scoreText != null && gameManager.Object != null && gameManager.Object.IsValid)
        {
            scoreText.text = $"{gameManager.Team0Score} - {gameManager.Team1Score}";
        }
    }

    /**
     * <summary>
     * Updates the match timer display.
     * </summary>
     */
    private void UpdateTimerDisplay()
    {
        if (gameManager != null && timerText != null && gameManager.Object != null && gameManager.Object.IsValid)
        {
            float timeRemaining = gameManager.TimeRemaining;
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    /**
     * <summary>
     * Updates the ability cooldown display for the single equipped ability.
     * </summary>
     */
    private void UpdateAbilityDisplay()
    {
        if (localAbilityController == null || abilityButton == null) 
            return;     

        float cooldown = localAbilityController.GetCooldownRemaining();
        bool isReady = localAbilityController.IsAbilityReady();
        var ability = localAbilityController.ActiveAbility;
        
        float sliderPercentage = cooldown > 0f ? Mathf.Clamp01(cooldown / ability.CooldownDuration) : 0f;
        
        abilityButton.SetSliderPercentage(sliderPercentage);
        
        if (isReady)
            abilityButton.CooldownFinished();
        else
            abilityButton.CooldownStarted();
    }

    /**
     * <summary>
     * Updates the connection status display showing ping.
     * Distinguishes between disconnected, connecting, and fully connected states.
     * </summary>
     */
    private void UpdateConnectionStatus()
    {
        if (connectionStatusText == null) return;

        var runner = FindFirstObjectByType<NetworkRunner>();
        if (runner == null)
        {
            connectionStatusText.text = "Disconnected";
            return;
        }

        if (!runner.IsRunning)
        {
            connectionStatusText.text = "Connecting...";
            return;
        }

        // Runner is running — show ping. GetPlayerRtt returns round-trip in seconds.
        double rtt = runner.GetPlayerRtt(runner.LocalPlayer);
        int pingMs = Mathf.RoundToInt((float)(rtt * 1000.0));
        connectionStatusText.text = $"Connected | Ping: {pingMs}ms";
    }
}

