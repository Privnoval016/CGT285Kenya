using UnityEngine;
using Fusion;

/**
 * GameManager is the central authority for match state and game flow.
 * It's a singleton that manages score, match time, and game events.
 * 
 * Pattern: Singleton for global access
 * Fusion: Uses [Networked] properties to sync game state across all clients
 * 
 * IMPORTANT: GameManager MUST have a NetworkObject component in the scene!
 * This is automatically enforced by the [RequireComponent] attribute.
 */
[RequireComponent(typeof(NetworkObject))]
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Prefabs")]
    [SerializeField] private NetworkPlayer playerPrefab;
    [SerializeField] private NetworkBallController ballPrefab;
    
    [Header("Match Settings")]
    [SerializeField] private float matchDuration = 180f;
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

        // Remove all obstruction blocks as specified — blocks are cleared on goal.
        ObstructionBlock.DespawnAll(Runner);
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
        if (!Object.HasStateAuthority) return;
        
        if (cachedBall != null)
        {
            cachedBall.transform.position = new Vector3(0, 0.5f, 0);
            var rb = cachedBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
    
    public void RegisterBall(NetworkBallController ball)
    {
        cachedBall = ball;
    }
}

