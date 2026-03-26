using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

/**
 * NetworkRunnerHandler manages the Photon Fusion NetworkRunner lifecycle.
 * This is the entry point for multiplayer, handling connection, session creation,
 * and delegating network events to the game's callback system.
 */
public class NetworkRunnerHandler : MonoBehaviour
{
    [Header("Runner Settings")]
    [SerializeField] private NetworkRunner runnerPrefab;
    [SerializeField] private NetworkCallbackHandler networkCallbackHandler;
    
    [Header("Game Settings")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private GameMode gameMode = GameMode.Shared;
    [SerializeField] private string sessionName = "SoccerMatch";
    [SerializeField] private int maxPlayers = 4;
    
    private NetworkRunner runnerInstance;
    
    public NetworkRunner RunnerInstance => runnerInstance;

    private void Start()
    {
        /* Check if runner already exists (from LobbyManager) */
        var existingRunner = FindFirstObjectByType<NetworkRunner>();
        if (existingRunner != null)
        {
            Debug.Log("[NetworkRunnerHandler] Runner already exists from lobby, skipping Start");
            runnerInstance = existingRunner;
            return;
        }

        /* Only start game if no runner exists */
        StartGameAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"Failed to start game: {task.Exception}");
            }
        });
    }

    /**
     * <summary>
     * Starts or joins a Fusion game session.
     * Uses Shared mode which is ideal for fast-paced games with client-side prediction.
     * </summary>
     * <returns>Task that completes when connection is established</returns>
     */
    private async Task StartGameAsync()
    {
        if (runnerInstance != null)
        {
            Debug.LogWarning("[NetworkRunner] Runner already exists!");
            return;
        }
        
        Debug.Log($"[NetworkRunner] Starting game in {gameMode} mode, Session: {sessionName}");
        
        if (runnerPrefab != null)
        {
            runnerInstance = Instantiate(runnerPrefab);
        }
        else
        {
            var go = new GameObject("NetworkRunner");
            runnerInstance = go.AddComponent<NetworkRunner>();
        }
        
        runnerInstance.AddCallbacks(networkCallbackHandler);
        
        var startGameArgs = new StartGameArgs()
        {
            GameMode = gameMode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = runnerInstance.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = maxPlayers,
            SessionProperties = new Dictionary<string, SessionProperty>()
            {
                { "GameVersion", "1.0" }
            }
        };
        
        Debug.Log($"[NetworkRunner] Calling StartGame...");
        var result = await runnerInstance.StartGame(startGameArgs);
        
        if (result.Ok)
        {
            Debug.Log($"[NetworkRunner] Game started successfully in {gameMode} mode!");
            Debug.Log($"[NetworkRunner] Session: {runnerInstance.SessionInfo.Name}, Region: {runnerInstance.SessionInfo.Region}");
            Debug.Log($"[NetworkRunner] Is Server: {runnerInstance.IsServer}, Is Client: {runnerInstance.IsClient}");
            Debug.Log($"[NetworkRunner] Is Shared Mode Master: {runnerInstance.IsSharedModeMasterClient}");
            Debug.Log($"[NetworkRunner] Player Count: {runnerInstance.ActivePlayers.Count()}");
            Debug.Log($"[NetworkRunner] Local Player: {runnerInstance.LocalPlayer.PlayerId}");
        }
        else
        {
            Debug.LogError($"[NetworkRunner] Failed to start game: {result.ShutdownReason}");
            Debug.LogError($"[NetworkRunner] Error info: {result.ErrorMessage}");
        }
    }

    /**
     * Cleanly shutdown the network session
     */
    public async Task ShutdownRunner()
    {
        if (runnerInstance != null)
        {
            await runnerInstance.Shutdown();
            Destroy(runnerInstance.gameObject);
            runnerInstance = null;
        }
    }

    private void OnDestroy()
    {
        if (runnerInstance != null)
        {
            _ = ShutdownRunner();
        }
    }
}
