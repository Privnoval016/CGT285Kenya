using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Fusion;
using TMPro;

namespace Networking
{
    /**
     * <summary>
     * LobbyManager handles the lobby scene and session matchmaking.
     * Waits for enough players to join before loading the game scene.
     * </summary>
     */
    public class LobbyManager : MonoBehaviour
    {
        [Header("UI References")] [SerializeField]
        private UI uiComponents;

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "MultiplayerTestScene";

        [Header("Network Settings")]
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private GameObject networkCallbackHandlerPrefab;
        [SerializeField] private int maxPlayersPerMatch = 6;
        [SerializeField] private GameMode gameMode = GameMode.Shared;

        private NetworkRunner networkRunner;
        private int currentPlayerCount;
        private string currentSessionName;
        private int gameSceneBuildIndex = -1;
        
        public static Queue<int> playerIndexQueue = new Queue<int>();

        private void Awake()
        {
            /* Persist across scene loads so we can track player count */
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LobbyManager] Marked DontDestroyOnLoad");
        }

        private void Start()
        {
            if (uiComponents.play != null)
            {
                uiComponents.play.clicked += OnStartGamePressed;
            }

            UpdateStatusText("Ready to find a match. Press Start Game.");
        }

        private void OnStartGamePressed()
        {
            if (networkRunner != null)
            {
                UpdateStatusText("Already looking for a match...");
                return;
            }

            UpdateStatusText("Looking for a match...");
            StartCoroutine(ConnectToMatchAsync());
        }

        private IEnumerator ConnectToMatchAsync()
        {
            var existingRunner = FindFirstObjectByType<NetworkRunner>();
            if (existingRunner != null)
            {
                networkRunner = existingRunner;
                Debug.Log("[LobbyManager] Using existing NetworkRunner");
                yield break;
            }

            if (runnerPrefab != null)
            {
                networkRunner = Instantiate(runnerPrefab);
            }
            else
            {
                var go = new GameObject("[NetworkRunner]");
                networkRunner = go.AddComponent<NetworkRunner>();
            }

            networkRunner.gameObject.name = "[NetworkRunner]";
            DontDestroyOnLoad(networkRunner.gameObject);
            Debug.Log("[LobbyManager] NetworkRunner marked DontDestroyOnLoad");

            /* Create NetworkCallbackHandler */
            NetworkCallbackHandler callbackHandler;
            if (networkCallbackHandlerPrefab != null)
            {
                var handlerGo = Instantiate(networkCallbackHandlerPrefab);
                handlerGo.name = "[NetworkCallbackHandler]";
                callbackHandler = handlerGo.GetComponent<NetworkCallbackHandler>();
                Debug.Log("[LobbyManager] NetworkCallbackHandler from prefab");
            }
            else
            {
                var go = new GameObject("[NetworkCallbackHandler]");
                callbackHandler = go.AddComponent<NetworkCallbackHandler>();
                Debug.Log("[LobbyManager] NetworkCallbackHandler created dynamically");
            }

            DontDestroyOnLoad(callbackHandler.gameObject);
            networkRunner.AddCallbacks(callbackHandler);

            currentSessionName = !string.IsNullOrEmpty(uiComponents.code?.text)
                ? uiComponents.code.text
                : "SoccerMatch"; /* Default session name - all players join same session */

            /* Get game scene build index */
            gameSceneBuildIndex = -1;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                if (scenePath.Contains(gameSceneName))
                {
                    gameSceneBuildIndex = i;
                    break;
                }
            }

            if (gameSceneBuildIndex < 0)
            {
                Debug.LogError($"[LobbyManager] Scene '{gameSceneName}' not in Build Settings");
                UpdateStatusText($"Error: Scene not found");
                networkRunner = null;
                yield break;
            }

            UpdateStatusText($"Searching for match...");
            Debug.Log($"[LobbyManager] Need {maxPlayersPerMatch} players");

            /* Don't include Scene in StartGameArgs yet - we'll load it manually when ready */
            var startGameArgs = new StartGameArgs()
            {
                GameMode = gameMode,
                SessionName = currentSessionName,
                Scene = SceneRef.FromIndex(0), /* Stay on current (lobby) scene */
                SceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                PlayerCount = maxPlayersPerMatch,
            };

            networkRunner.StartGame(startGameArgs).ContinueWith(task =>
            {
                if (!task.Result.Ok)
                {
                    Debug.LogError($"[LobbyManager] StartGame error: {task.Result.ErrorMessage}");
                    UpdateStatusText($"Connection error");
                    networkRunner = null;
                }
                else
                {
                    Debug.Log("[LobbyManager] Connected to Photon!");
                }
            });

            /* Keep coroutine alive with proper yields for UI updates */
            while (networkRunner != null && networkRunner.IsRunning)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        /* Track players and load scene when ready */
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            currentPlayerCount = runner.ActivePlayers.Count();
            Debug.Log($"[LobbyManager] Player joined! {currentPlayerCount}/{maxPlayersPerMatch}");
            UpdateStatusText($"Players: {currentPlayerCount}/{maxPlayersPerMatch}");

            if (currentPlayerCount >= maxPlayersPerMatch)
            {
                Debug.Log($"[LobbyManager] Match found! {currentPlayerCount} players ready. Loading game scene...");
                UpdateStatusText("Match found! Loading game...");
                
                /* Only the master client can load the scene */
                if (runner.IsSharedModeMasterClient && gameSceneBuildIndex >= 0)
                {
                    Debug.Log($"[LobbyManager] Master client loading scene index {gameSceneBuildIndex}");
                    runner.LoadScene(SceneRef.FromIndex(gameSceneBuildIndex));
                }
                else if (!runner.IsSharedModeMasterClient)
                {
                    Debug.Log($"[LobbyManager] Not master client, waiting for host to load scene...");
                }
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            currentPlayerCount = runner.ActivePlayers.Count();
            UpdateStatusText($"Players: {currentPlayerCount}/{maxPlayersPerMatch}");
        }

        private void UpdateStatusText(string message)
        {
            Debug.Log($"[LobbyManager] {message}");
            if (uiComponents.statusText != null)
            {
                uiComponents.statusText.text = message;
            }
        }

        private void OnDestroy()
        {
            if (uiComponents.play != null)
            {
                uiComponents.play.clicked -= OnStartGamePressed;
            }
        }
    }
}
