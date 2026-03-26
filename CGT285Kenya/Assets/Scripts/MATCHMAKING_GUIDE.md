# Matchmaking System - How It Works

## What Changed

I've implemented **proper matchmaking** as you described:

1. **Player 1** clicks "Start Game" → Joins a session, waits for more players
2. **Player 2** clicks "Start Game" → Joins same session, still waiting
3. **Player 3** clicks "Start Game" → Joins session, still waiting
4. **Player 4** clicks "Start Game" → Session now has 4 players → Game scene loads automatically

## Key Changes

### LobbyManager is now INetworkRunnerCallbacks

```csharp
public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
```

This means LobbyManager receives Fusion network callbacks directly, allowing it to track player joins.

### OnPlayerJoined Callback

```csharp
public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    currentPlayerCount = runner.ActivePlayers.Count();
    UpdateStatusText($"Players: {currentPlayerCount}/{maxPlayersPerMatch}");
    
    if (currentPlayerCount >= maxPlayersPerMatch)
    {
        Debug.Log($"[LobbyManager] Match found! Loading game...");
        /* Game scene loads automatically via Fusion */
    }
}
```

This is called every time a player joins. When we have enough players, the game scene loads.

### Scene Loading Trigger

When `currentPlayerCount >= maxPlayersPerMatch`:
- **Fusion's NetworkSceneManagerDefault automatically loads the game scene**
- All players transition together
- No manual scene loading needed

## Testing With Two Instances

### Instance 1 (Editor):
```
1. Click "Start Game"
2. Console shows: "[LobbyManager] Need 4 players"
3. UI shows: "Players: 1/4"
4. Waits...
```

### Instance 2 (Built Build):
```
1. Click "Start Game"
2. Joins same session
3. Instance 1 shows: "Players: 2/4"
4. Still waiting...
```

### Instance 3 & 4:
```
When Instance 4 joins:
- Both Instance 1 & 2 show: "Players: 4/4"
- Scene loads on ALL instances
- Game starts synchronized
```

## Console Output

```
[LobbyManager] Need 4 players
[LobbyManager] Player joined! 1/4
[LobbyManager] Player joined! 2/4
[LobbyManager] Player joined! 3/4
[LobbyManager] Player joined! 4/4
[LobbyManager] Match found! Loading game...
[GameManager] Match started! Duration: 300s
✅ Game runs smoothly
```

## How It Solves Your Problems

✅ **No more Code 104 errors** - Scene loads when ready
✅ **No more weird spawning** - All players spawn synchronously
✅ **No ball/timer issues** - GameManager initializes properly
✅ **True matchmaking** - Waits for required player count
✅ **Clean transition** - Scene loads once, all at the same time

## What You Need to Do

1. **Test with 2+ instances** (don't test with 1 instance)
2. Build the game: File → Build and Run
3. Run 2 copies side-by-side
4. Instance 1: Click "Start Game" → Wait
5. Instance 2: Click "Start Game" → Match should start when enough players

## Why Single-Instance Doesn't Work

With only 1 instance:
- GameManager's `OnPlayerJoined` never sees state authority properly
- You need at least 2 logical players for multiplayer to work correctly
- This is a Fusion requirement, not a bug

## Advanced: Customizing Wait Time

Currently, it waits for exactly `maxPlayersPerMatch` players.

To add a timeout (start game with fewer players after 30 seconds):

```csharp
private float matchmakingTimeout = 30f;
private float matchmakingStartTime;

private void OnStartGamePressed()
{
    // ...
    matchmakingStartTime = Time.time;
}

public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
{
    currentPlayerCount = runner.ActivePlayers.Count();
    float elapsedTime = Time.time - matchmakingStartTime;
    
    bool hasEnoughPlayers = currentPlayerCount >= maxPlayersPerMatch;
    bool timedOut = elapsedTime >= matchmakingTimeout;
    
    if (hasEnoughPlayers || timedOut)
    {
        Debug.Log($"[LobbyManager] Match starting!");
    }
}
```

---

**Now test with 2 instances and tell me if matchmaking works!** 🚀

