using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Configuration;

/**
 * <summary>
 * GameManager is the central authority for match state and game flow.
 * It's a singleton that manages score, match time, game events, and field resets.
 *
 * Pattern: Singleton for global access
 * Fusion: Uses [Networked] properties to sync game state across all clients
 *
 * IMPORTANT: GameManager MUST have a NetworkObject component in the scene!
 * This is automatically enforced by the [RequireComponent] attribute.
 * </summary>
 */
[RequireComponent(typeof(NetworkObject))]
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Prefabs")]
    [SerializeField] private NetworkPlayer playerPrefab;
    [SerializeField] private NetworkBallController ballPrefab;
    
    [Header("Configuration")]
    [SerializeField] private MatchRulesConfig matchRulesConfig;
    [SerializeField] private SpawnPointConfig spawnPointConfig;
    
    [Networked] public int Team0Score { get; set; }
    [Networked] public int Team1Score { get; set; }
    [Networked] public TickTimer MatchTimer { get; set; }
    [Networked] public bool MatchActive { get; set; }
    
    [Networked] private TickTimer fieldResetTimer { get; set; }
    
    public NetworkPlayer PlayerPrefab => playerPrefab;
    public NetworkBallController BallPrefab => ballPrefab;
    public float TimeRemaining => (Object != null && Object.IsValid) ? (MatchTimer.RemainingTime(Runner) ?? 0f) : 0f;
    public float ElapsedTime => (Object != null && Object.IsValid && matchRulesConfig != null) 
        ? (matchRulesConfig.MatchDurationSeconds - TimeRemaining) 
        : 0f;
    
    private NetworkBallController cachedBall;
    private List<NetworkPlayer> connectedPlayers = new List<NetworkPlayer>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[GameManager] Destroying duplicate GameManager instance");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        /* Only persist the game scene's GameManager */
        DontDestroyOnLoad(gameObject);
        Debug.Log("[GameManager] GameManager initialized and marked DontDestroyOnLoad");
    }

    private void Start()
    {
        /* Check if we're in the lobby or game scene */
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene.Contains("Lobby"))
        {
            Debug.Log("[GameManager] In lobby scene - not starting match yet");
            /* Don't start match in lobby, wait until game scene loads */
        }
        else
        {
            Debug.Log("[GameManager] In game scene");
        }
    }

    public override void Spawned()
    {
        Debug.Log($"[GameManager] Spawned called. HasStateAuthority: {Object.HasStateAuthority}");
        
        /* Initialize match for all clients, but only state authority spawns the ball */
        StartMatch();
    }

    public override void FixedUpdateNetwork()
    {
        if (MatchActive && Object.HasStateAuthority)
        {
            if (MatchTimer.ExpiredOrNotRunning(Runner))
            {
                EndMatch();
                return;
            }

            /* Check early-end condition: if one team has a lead at a certain time, end match */
            if (matchRulesConfig != null && 
                matchRulesConfig.ShouldEndEarly(Team0Score, Team1Score, ElapsedTime))
            {
                Debug.Log("[GameManager] Early end condition triggered!");
                EndMatch();
                return;
            }

            /* Check score-to-win fallback condition */
            if (matchRulesConfig != null &&
                (Team0Score >= matchRulesConfig.ScoreToWin || 
                 Team1Score >= matchRulesConfig.ScoreToWin))
            {
                EndMatch();
                return;
            }

            /* Check if field reset is complete and ready to resume play */
            if (fieldResetTimer.IsRunning && fieldResetTimer.ExpiredOrNotRunning(Runner))
            {
                fieldResetTimer = TickTimer.None;
            }
        }
    }

    private void StartMatch()
    {
        if (!Object.HasStateAuthority) 
        {
            Debug.Log("[GameManager] Not state authority, waiting for other client to initialize match");
            return;
        }
        
        /* Only start match in game scene, not in lobby */
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentScene.Contains("Lobby"))
        {
            Debug.Log("[GameManager] In lobby scene, skipping match start");
            return;
        }
        
        Team0Score = 0;
        Team1Score = 0;

        float duration = matchRulesConfig != null 
            ? matchRulesConfig.MatchDurationSeconds 
            : 300f;
        
        MatchTimer = TickTimer.CreateFromSeconds(Runner, duration);
        MatchActive = true;
        
        SpawnBall();
        
        Debug.Log($"[GameManager] Match started! Duration: {duration}s");
    }

    private void EndMatch()
    {
        if (!Object.HasStateAuthority) return;
        
        MatchActive = false;
        
        int winner = Team0Score > Team1Score ? 0 : 1;
        Debug.Log($"[GameManager] Match ended! Team {winner} wins! Final Score: {Team0Score} - {Team1Score}");
    }

    public void OnGoalScored(int scoringTeam)
    {
        if (!Object.HasStateAuthority) return;
        
        if (scoringTeam == 0)
        {
            Team0Score++;
            Debug.Log($"[GameManager] Team 0 scores! Score is now {Team0Score} - {Team1Score}");
        }
        else
        {
            Team1Score++;
            Debug.Log($"[GameManager] Team 1 scores! Score is now {Team0Score} - {Team1Score}");
        }

        /* Clear all obstruction blocks on goal */
        ObstructionBlock.DespawnAll(Runner);

        /* Trigger field reset after configured delay */
        RPC_ResetField();
    }

    private void SpawnBall()
    {
        if (!Object.HasStateAuthority) return;
        
        if (cachedBall == null && ballPrefab != null)
        {
            Vector3 ballSpawnPos = spawnPointConfig != null
                ? spawnPointConfig.GetBallSpawnPosition()
                : Vector3.zero;

            cachedBall = Runner.Spawn(ballPrefab, ballSpawnPos, Quaternion.identity);
            Debug.Log($"[GameManager] Ball spawned at {ballSpawnPos}");
        }
    }

    /**
     * <summary>
     * Calculates players per team based on max players in session.
     * </summary>
     * <returns>Number of players per team (e.g., 2 for 2v2, 3 for 3v3)</returns>
     */
    private int GetPlayersPerTeam()
    {
        if (Runner == null || Runner.SessionInfo == null)
        {
            return 3; /* Default to 3v3 */
        }

        int maxPlayers = Runner.SessionInfo.MaxPlayers;
        return maxPlayers / 2;
    }

    /**
     * <summary>
     * RPC called on all clients to reset the field after a goal.
     * Resets player positions to spawn points and ball to center.
     * </summary>
     */
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ResetField()
    {
        Debug.Log("[GameManager] RPC_ResetField called - resetting field");
        
        int playersPerTeam = GetPlayersPerTeam();
        
        /* Reset all players to their spawn positions */
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player.Object != null && player.Object.IsValid)
            {
                Vector3 spawnPos = spawnPointConfig != null
                    ? spawnPointConfig.GetPlayerSpawnByPlayerId(player.Object.InputAuthority.PlayerId, playersPerTeam)
                    : new Vector3(player.Team == 0 ? -5f : 5f, 0.5f, 0f);

                player.ResetToSpawnPosition(spawnPos);
            }
        }

        /* Reset ball to center */
        if (cachedBall != null)
        {
            Vector3 ballSpawnPos = spawnPointConfig != null
                ? spawnPointConfig.GetBallSpawnPosition()
                : Vector3.zero;

            cachedBall.ResetToSpawnPosition(ballSpawnPos);
        }
    }

    /**
     * <summary>
     * Registers a player with the GameManager when they spawn.
     * Used for tracking connected players for game logic.
     * </summary>
     */
    public void RegisterPlayer(NetworkPlayer player)
    {
        if (!connectedPlayers.Contains(player))
        {
            connectedPlayers.Add(player);
            Debug.Log($"[GameManager] Player {player.Object.InputAuthority.PlayerId} registered. Total players: {connectedPlayers.Count}");
        }
    }

    /**
     * <summary>
     * Unregisters a player when they disconnect.
     * </summary>
     */
    public void UnregisterPlayer(NetworkPlayer player)
    {
        if (connectedPlayers.Contains(player))
        {
            connectedPlayers.Remove(player);
            Debug.Log($"[GameManager] Player unregistered. Total players: {connectedPlayers.Count}");
        }
    }
    
    public void RegisterBall(NetworkBallController ball)
    {
        cachedBall = ball;
    }
}

