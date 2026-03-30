using UnityEngine;


/**
 * <summary>
 * SpawnPointConfig defines spawn positions for players and ball on the field.
 * Allows designers to configure exact spawn locations per team and position.
 * </summary>
 */
[CreateAssetMenu(menuName = "Game/Spawn Point Config", fileName = "SpawnPointConfig")]
public class SpawnPointConfig : ScriptableObject
{
    [System.Serializable]
    public struct TeamSpawns
    {
        [Tooltip("Spawn positions for this team (e.g., 3 positions for 3v3)")]
        public Vector3[] positions;
    }

    [Header("Team Spawns")]
    [SerializeField] private TeamSpawns team0Spawns;
    [SerializeField] private TeamSpawns team1Spawns;

    [Header("Lobby Spawns")]
    [Tooltip("Spawn positions for players in the lobby")]
    [SerializeField] private TeamSpawns lobbyTeam0Spawns;
    [SerializeField] private TeamSpawns lobbyTeam1Spawns;

    [Header("Ball Spawn")]
    [Tooltip("Center field spawn position for the ball")]
    [SerializeField] private Vector3 ballSpawnPosition = Vector3.zero;

    /**
     * <summary>
     * Gets the spawn position for a player of a specific team and position index.
     * </summary>
     * <param name="team">0 or 1</param>
     * <param name="positionIndex">Player's position index on the team</param>
     * <returns>Spawn position</returns>
     */
    public Vector3 GetPlayerSpawnPosition(int team, int positionIndex)
    {
        var spawns = team == 0 ? team0Spawns : team1Spawns;

        if (spawns.positions == null || spawns.positions.Length == 0)
        {
            Debug.LogWarning($"[SpawnPointConfig] No spawn positions configured for Team {team}");
            return new Vector3(team == 0 ? -5f : 5f, 0.5f, 0f);
        }

        positionIndex = Mathf.Clamp(positionIndex, 0, spawns.positions.Length - 1);
        return spawns.positions[positionIndex];
    }

    /**
     * <summary>
     * Gets the ball spawn position.
     * </summary>
     * <returns>Ball spawn position</returns>
     */
    public Vector3 GetBallSpawnPosition() => ballSpawnPosition;

    /**
     * <summary>
     * Gets spawn position based on PlayerId with dynamic team sizing.
     * Calculates team and position based on playersPerTeam value.
     * Example: if playersPerTeam=2, PlayerId 1-2 are Team 0, PlayerId 3-4 are Team 1
     * </summary>
     * <param name="playerId">The player's ID (1-based)</param>
     * <param name="playersPerTeam">Number of players per team (e.g., 2 for 2v2, 3 for 3v3)</param>
     * <returns>Spawn position for this player</returns>
     */
    public Vector3 GetPlayerSpawnByPlayerId(int playerId, int playersPerTeam)
    {
        int zeroBasedId = playerId - 1;
        int team = zeroBasedId / playersPerTeam;
        int positionIndex = zeroBasedId % playersPerTeam;
        return GetPlayerSpawnPosition(team, positionIndex);
    }

    /**
     * <summary>
     * Gets spawn position based on PlayerId (deterministic across clients).
     * Uses default 3 players per team (3v3 matches).
     * Deprecated: Use GetPlayerSpawnByPlayerId(playerId, playersPerTeam) instead.
     * </summary>
     */
    public Vector3 GetPlayerSpawnByPlayerId(int playerId)
    {
        return GetPlayerSpawnByPlayerId(playerId, 3);
    }

    /**
     * <summary>
     * Gets spawn position for lobby (waiting area before match).
     * </summary>
     */
    public Vector3 GetLobbySpawnPosition(int team, int positionIndex)
    {
        var spawns = team == 0 ? lobbyTeam0Spawns : lobbyTeam1Spawns;

        if (spawns.positions == null || spawns.positions.Length == 0)
        {
            /* Fallback to regular team spawns if lobby spawns not configured */
            return GetPlayerSpawnPosition(team, positionIndex);
        }

        positionIndex = Mathf.Clamp(positionIndex, 0, spawns.positions.Length - 1);
        return spawns.positions[positionIndex];
    }

    /**
     * <summary>
     * Gets lobby spawn position based on PlayerId with dynamic team sizing.
     * </summary>
     */
    public Vector3 GetLobbySpawnByPlayerId(int playerId, int playersPerTeam)
    {
        int zeroBasedId = playerId - 1;
        int team = zeroBasedId / playersPerTeam;
        int positionIndex = zeroBasedId % playersPerTeam;
        return GetLobbySpawnPosition(team, positionIndex);
    }
}





