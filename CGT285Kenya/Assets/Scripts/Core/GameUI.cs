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
    
    [Header("Ability Display")]
    [SerializeField] private Image abilityCooldownImage;
    [SerializeField] private TextMeshProUGUI abilityCooldownText;
    
    [Header("Connection Status")]
    [SerializeField] private TextMeshProUGUI connectionStatusText;
    
    private GameManager gameManager;
    private NetworkPlayer localPlayer;
    private AbilityController localAbilityController;
    
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
        if (localAbilityController == null || abilityCooldownImage == null) 
            return;
        
        float cooldown = localAbilityController.GetAbilityCooldown(0);
        bool isReady = localAbilityController.IsAbilityReady(0);
        var ability = localAbilityController.EquippedAbility;
        
        // Update fill amount
        if (isReady)
        {
            abilityCooldownImage.fillAmount = 1f;
            abilityCooldownImage.color = Color.white;
        }
        else if (ability != null)
        {
            abilityCooldownImage.fillAmount = 1f - (cooldown / ability.CooldownDuration);
            abilityCooldownImage.color = Color.gray;
        }
        
        // Update text
        if (abilityCooldownText != null)
        {
            if (ability == null)
            {
                abilityCooldownText.text = "No Ability";
            }
            else if (isReady)
            {
                abilityCooldownText.text = "Ready";
            }
            else
            {
                abilityCooldownText.text = $"{cooldown:F1}s";
            }
        }
    }

    /**
     * <summary>
     * Updates the connection status display showing ping.
     * </summary>
     */
    private void UpdateConnectionStatus()
    {
        if (connectionStatusText != null)
        {
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null && runner.IsRunning)
            {
                connectionStatusText.text = $"Connected | Ping: {runner.GetPlayerRtt(runner.LocalPlayer) * 1000:F0}ms";
            }
            else
            {
                connectionStatusText.text = "Disconnected";
            }
        }
    }
}

