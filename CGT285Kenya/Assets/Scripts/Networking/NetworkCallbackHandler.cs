using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Fusion;
using Fusion.Sockets;

/**
 * NetworkCallbackHandler implements INetworkRunnerCallbacks to handle all Fusion network events.
 * This acts as a central hub for network lifecycle events and delegates them to appropriate systems.
 */
public class NetworkCallbackHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    /**
     * <summary>
     * Called when a player joins the session.
     * Each client spawns their own player when they join.
     * </summary>
     * <param name="runner">The NetworkRunner instance</param>
     * <param name="player">The PlayerRef that joined</param>
     */
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkCallback] Player {player.PlayerId} joined. IsLocal: {player == runner.LocalPlayer}, IsMaster: {runner.IsSharedModeMasterClient}");
        
        // Only spawn your own player (when YOU join)
        if (player == runner.LocalPlayer)
        {
            Debug.Log($"[NetworkCallback] This is my player, spawning now");
            SpawnPlayer(runner, player);
        }
    }

    /**
     * <summary>
     * Spawns a player object for the given PlayerRef.
     * Centralized spawning logic.
     * </summary>
     * <param name="runner">The NetworkRunner instance</param>
     * <param name="player">The PlayerRef to spawn for</param>
     */
    private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkCallback] SpawnPlayer called for Player {player.PlayerId}");
        
        if (GameManager.Instance == null)
        {
            Debug.LogError("[NetworkCallback] GameManager.Instance is null! Make sure GameManager is in the scene with a NetworkObject component.");
            return;
        }
        
        Debug.Log($"[NetworkCallback] GameManager found");
        
        var playerPrefab = GameManager.Instance.PlayerPrefab;
        if (playerPrefab == null)
        {
            Debug.LogError("[NetworkCallback] PlayerPrefab is not assigned in GameManager!");
            return;
        }
        
        Debug.Log($"[NetworkCallback] PlayerPrefab found: {playerPrefab.name}");
        
        // Check if player already exists
        var existingPlayer = FindPlayerObject(runner, player);
        if (existingPlayer != null)
        {
            Debug.Log($"[NetworkCallback] Player {player.PlayerId} already exists, skipping spawn");
            return;
        }
        
        Vector3 spawnPosition = GetSpawnPosition(player);
        Debug.Log($"[NetworkCallback] Attempting to spawn player {player.PlayerId} at position: {spawnPosition}");
        
        try
        {
            var spawnedPlayer = runner.Spawn(
                playerPrefab.GetComponent<NetworkObject>(),
                spawnPosition,
                Quaternion.identity,
                player
            );
            
            if (spawnedPlayer != null)
            {
                Debug.Log($"[NetworkCallback] Successfully spawned player object for Player {player.PlayerId} at {spawnPosition}");
            }
            else
            {
                Debug.LogError($"[NetworkCallback] runner.Spawn returned null for Player {player.PlayerId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[NetworkCallback] Exception spawning player {player.PlayerId}: {e.Message}\n{e.StackTrace}");
        }
    }

    /**
     * Finds an existing player NetworkObject for the given PlayerRef.
     */
    private NetworkObject FindPlayerObject(NetworkRunner runner, PlayerRef player)
    {
        var allPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var netPlayer in allPlayers)
        {
            if (netPlayer.Object != null && netPlayer.Object.InputAuthority == player)
            {
                return netPlayer.Object;
            }
        }
        return null;
    }

    /**
     * <summary>
     * Called when a player leaves the session.
     * Cleanup happens automatically for NetworkObjects with PlayerRef ownership,
     * but we log it for debugging.
     * </summary>
     * <param name="runner">The NetworkRunner instance</param>
     * <param name="player">The PlayerRef that left</param>
     */
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkCallback] Player {player.PlayerId} left the session");
        
        // Find and despawn the player's object
        if (runner.IsSharedModeMasterClient)
        {
            var playerObj = FindPlayerObject(runner, player);
            if (playerObj != null)
            {
                Debug.Log($"[NetworkCallback] Despawning player {player.PlayerId}");
                runner.Despawn(playerObj);
            }
        }
    }

    /**
     * Called every simulation tick for input polling.
     * This is where we read local input and send it to the network.
     */
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var inputController = InputController.Instance;
        if (inputController != null)
        {
            var data = inputController.GetNetworkInput();
            input.Set(data);
        }
    }

    /**
     * Called when input has been received for a player from the network.
     * This is useful for lag compensation and prediction.
     */
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        // Fusion handles this automatically with prediction
    }

    /**
     * Called when the local connection is established.
     */
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[NetworkCallback] Connected to server!");
    }

    /**
     * Called when disconnected from the session.
     */
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"[NetworkCallback] Disconnected from server: {reason}");
    }

    /**
     * Called when a shutdown occurs (planned or error).
     */
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Runner shutdown: {shutdownReason}");
    }

    /**
     * Called when there's a network error or connectivity issue.
     */
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"Connection failed to {remoteAddress}: {reason}");
    }

    /**
     * Called when attempting to connect to a session.
     */
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
        request.Accept();
    }

    /**
     * Called when the scene loading begins.
     */
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("Scene load started");
    }

    /**
     * Called when the scene has finished loading.
     */
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[NetworkCallback] Scene load completed");
        Debug.Log($"[NetworkCallback] Local Player: {runner.LocalPlayer.PlayerId}, Active Players: {runner.ActivePlayers.Count()}");
        
        // Don't spawn here - OnPlayerJoined will handle it
        // Just log the state for debugging
    }

    /**
     * Called when user simulation messages are received.
     */
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
        // Can be used for custom messaging between clients
    }

    /**
     * Called each simulation frame, after physics.
     */
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        // Useful for lobby/matchmaking UI
    }

    /**
     * Called when custom properties change.
     */
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
        // For custom authentication systems
    }

    /**
     * Called when host migration occurs (if enabled).
     */
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("Host migration occurred");
    }

    /**
     * Called when the client receives reliable data from server.
     */
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        // For reliable custom data transmission
    }

    /**
     * Called when the client receives progress updates for reliable data.
     */
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        // For tracking large data transfers
    }

    /**
     * Called when an object is about to be pooled/despawned.
     */
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Area of Interest (AOI) management - advanced feature
    }

    /**
     * Called when an object enters a player's area of interest.
     */
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
        // Area of Interest (AOI) management - advanced feature
    }

    private Vector3 GetSpawnPosition(PlayerRef player)
    {
        int playerId = player.PlayerId;
        int team = playerId < 3 ? 0 : 1;
        int positionInTeam = playerId % 3;
        
        float xPos = team == 0 ? -5f : 5f;
        float zPos = (positionInTeam - 1) * 3f;
        
        return new Vector3(xPos, 0.5f, zPos);
    }
}

