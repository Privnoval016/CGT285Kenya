using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs")] [SerializeField] private NetworkPlayer playerPrefab;
    [SerializeField] private NetworkBallController ballPrefab;

    [Header("Spawn Configuration")] [SerializeField]
    private SpawnPointConfig spawnPointConfig;

    [Header("Match Settings")] [SerializeField]
    private float matchDuration = 180f;

    [SerializeField] private int scoreToWin = 3;

    [Networked] public int Team0Score { get; set; }
    [Networked] public int Team1Score { get; set; }
    [Networked] public TickTimer MatchTimer { get; set; }
    [Networked] public bool MatchActive { get; set; }

    public NetworkPlayer PlayerPrefab => playerPrefab;
    public NetworkBallController BallPrefab => ballPrefab;
    public float TimeRemaining => (Object != null && Object.IsValid) ? (MatchTimer.RemainingTime(Runner) ?? 0f) : 0f;

    private NetworkBallController cachedBall;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            StartMatch();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (MatchActive && Object.HasStateAuthority)
        {
            if (MatchTimer.ExpiredOrNotRunning(Runner))
            {
                EndMatch();
            }

            if (Team0Score >= scoreToWin || Team1Score >= scoreToWin)
            {
                EndMatch();
            }
        }
    }

    private void StartMatch()
    {
        if (!Object.HasStateAuthority) return;

        Team0Score = 0;
        Team1Score = 0;
        MatchTimer = TickTimer.CreateFromSeconds(Runner, matchDuration);
        MatchActive = true;

        SpawnBall();
        
        ResetAllPlayers();

        Debug.Log("Match started!");
    }

    private void EndMatch()
    {
        if (!Object.HasStateAuthority) return;

        MatchActive = false;

        int winner = Team0Score > Team1Score ? 0 : 1;
        Debug.Log($"Match ended! Team {winner} wins! Score: {Team0Score} - {Team1Score}");
    }

    public void OnGoalScored(int scoringTeam)
    {
        if (!Object.HasStateAuthority) return;

        if (scoringTeam == 0)
        {
            Team1Score++;
            Debug.Log($"Team 1 scores! Score is now {Team0Score} - {Team1Score}");
        }
        else
        {
            Team0Score++;
            Debug.Log($"Team 0 scores! Score is now {Team0Score} - {Team1Score}");
        }

        // Despawn all obstruction blocks
        ObstructionBlock.DespawnAll(Runner);
        
        // Reset ball to center via RPC
        ResetBall();
        
        // Reset all players to their spawn positions via RPC
        ResetAllPlayers();
    }

    private void ResetAllPlayers()
    {
        //if (!Object.HasStateAuthority) return;

        var allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        
        // Determine players per team dynamically from session info
        int playersPerTeam = 3; // Default to 3v3
        if (Runner != null && Runner.SessionInfo != null)
        {
            int maxPlayers = Runner.SessionInfo.MaxPlayers;
            playersPerTeam = maxPlayers / 2;
        }

        foreach (var player in allPlayers)
        {
            if (player == null || player.Object == null || !player.Object.IsValid)
                continue;

            // Get spawn position using SpawnPointConfig
            Vector3 spawnPosition = GetPlayerSpawnPosition(player, playersPerTeam);
            // Send RPC to this player to reset themselves on their InputAuthority
            player.RPC_ResetToSpawnPosition(spawnPosition);
            Debug.Log($"[GameManager] Sent reset RPC to player {player.Object.InputAuthority.PlayerId} for position {spawnPosition}");
        }

        Debug.Log("[GameManager] Reset RPCs sent to all players");
    }

    private Vector3 GetPlayerSpawnPosition(NetworkPlayer player, int playersPerTeam)
    {
        int playerId = player.Object.InputAuthority.PlayerId;

        // Use SpawnPointConfig if available
        if (spawnPointConfig != null)
        {
            return spawnPointConfig.GetPlayerSpawnByPlayerId(playerId, playersPerTeam);
        }

        // Fallback: Calculate spawn position based on team and player ID
        int zeroBasedId = playerId - 1;
        int team = zeroBasedId / playersPerTeam;
        int positionInTeam = zeroBasedId % playersPerTeam;

        // Default spawn positions (3v3 layout)
        float xPos = team == 0 ? -5f : 5f;
        float zPos = (positionInTeam - 1) * 3f;

        return new Vector3(xPos, 0.5f, zPos);
    }

    private void SpawnBall()
    {
        if (!Object.HasStateAuthority) return;

        if (cachedBall == null && ballPrefab != null)
        {
            cachedBall = Runner.Spawn(ballPrefab, new Vector3(0, 0.5f, 0), Quaternion.identity);
            Debug.Log("Ball spawned!");
        }
    }

    public void ResetBall()
    {
        //if (!Object.HasStateAuthority) return;

        if (cachedBall != null && cachedBall.Object != null && cachedBall.Object.IsValid)
        {
            Vector3 centerPosition = new Vector3(0, 0.5f, 0);
            // Send RPC to reset the ball on its state authority
            cachedBall.RPC_ResetToSpawnPosition(centerPosition);
            Debug.Log("[GameManager] Sent reset RPC to ball");
        }
    }

    public void RegisterBall(NetworkBallController ball)
    {
        cachedBall = ball;
    }
}
