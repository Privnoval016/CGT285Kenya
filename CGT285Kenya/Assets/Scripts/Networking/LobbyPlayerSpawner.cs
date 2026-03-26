using UnityEngine;

namespace Networking
{
    /**
     * <summary>
     * LobbyPlayerSpawner holds references to lobby-specific player prefabs.
     * This allows the lobby scene to spawn players WITHOUT needing GameManager.
     * 
     * Setup:
     *   1. Create a GameObject in the LobbyScene named "[LobbyPlayerSpawner]"
     *   2. Add this script to it
     *   3. Assign the lobby player prefab in the inspector
     *   4. Mark it DontDestroyOnLoad (optional, but recommended)
     * </summary>
     */
    public class LobbyPlayerSpawner : MonoBehaviour
    {
        [SerializeField] private NetworkPlayer lobbyPlayerPrefab;

        public static LobbyPlayerSpawner Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.Log("[LobbyPlayerSpawner] Destroying duplicate instance");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LobbyPlayerSpawner] Initialized and marked DontDestroyOnLoad");
        }

        public NetworkPlayer GetLobbyPlayerPrefab()
        {
            if (lobbyPlayerPrefab == null)
            {
                Debug.LogError("[LobbyPlayerSpawner] Lobby player prefab is not assigned!");
            }
            return lobbyPlayerPrefab;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}


