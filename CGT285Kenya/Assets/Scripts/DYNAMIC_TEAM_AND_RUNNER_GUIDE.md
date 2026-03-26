# Dynamic Team Assignment & NetworkRunner Setup Guide

## Part 1: Dynamic Team Assignment

I've updated the system to automatically calculate team assignment based on maxPlayers specified in your NetworkRunner. Here's how it works:

### How It Works

When you set `maxPlayers = 4` in either:
- LobbyManager
- NetworkRunnerHandler

The system will:
1. Calculate `playersPerTeam = maxPlayers / 2` (e.g., 4 → 2 per team)
2. Assign PlayerId 1-2 → Team 0 (Blue)
3. Assign PlayerId 3-4 → Team 1 (Red)

For `maxPlayers = 6`:
1. Calculate `playersPerTeam = 6 / 2 = 3` 
2. Assign PlayerId 1-3 → Team 0
3. Assign PlayerId 4-6 → Team 1

### Files Modified

1. **SpawnPointConfig.cs**
   - Added `GetPlayerSpawnByPlayerId(int playerId, int playersPerTeam)` with explicit team sizing
   - Old `GetPlayerSpawnByPlayerId(int playerId)` now calls new version with default playersPerTeam=3

2. **NetworkCallbackHandler.cs**
   - Updated `GetSpawnPosition()` to calculate playersPerTeam from `runner.SessionInfo.MaxPlayers`
   - Team calculation: `team = (playerId - 1) / playersPerTeam`
   - Position in team: `positionIndex = (playerId - 1) % playersPerTeam`

3. **NetworkPlayer.cs**
   - Updated `Spawned()` to calculate team dynamically
   - Reads `Runner.SessionInfo.MaxPlayers` and divides by 2

4. **GameManager.cs**
   - Added `GetPlayersPerTeam()` helper method
   - Updated `RPC_ResetField()` to use dynamic playersPerTeam when resetting positions

### Example Configurations

**For 2v2 (4 players total):**
- Set `maxPlayers = 4` in LobbyManager
- Player 1 & 2 → Team 0 (Blue)
- Player 3 & 4 → Team 1 (Red)

**For 3v3 (6 players total):**
- Set `maxPlayers = 6` in LobbyManager
- Player 1, 2, 3 → Team 0 (Blue)
- Player 4, 5, 6 → Team 1 (Red)

**For 4v4 (8 players total):**
- Set `maxPlayers = 8` in LobbyManager
- Player 1, 2, 3, 4 → Team 0 (Blue)
- Player 5, 6, 7, 8 → Team 1 (Red)

## Part 2: NetworkRunner Setup

### Do NOT Duplicate the NetworkRunnerHandler!

**The NetworkRunner should be created ONCE and persist across scenes.** Here are your options:

### Option A: Use LobbyManager's Runner (Recommended)

**Setup:**
1. Delete NetworkRunnerHandler from your game scene
2. Keep LobbyManager in the lobby scene
3. LobbyManager creates the NetworkRunner and loads the game scene
4. The runner persists from lobby → game scene automatically

**Why it works:**
- When LobbyManager calls `SceneManager.LoadScene(gameSceneName)`, the NetworkRunner gameobject is NOT in the scene being unloaded
- The NetworkRunner stays active across scene transitions
- All callbacks continue to work

**Pros:**
- Single runner instance
- Clean lobby flow
- No duplication

**Cons:**
- Must start in LobbyScene
- Can't test game scene directly (must go through lobby first)

### Option B: Use NetworkRunnerHandler in Both Scenes (Not Recommended)

If you want to test game scene directly, you could:

1. Create NetworkRunnerHandler as a prefab
2. Add it to both LobbyScene and MultiplayerTestScene
3. Mark it with `[ExecuteAlways]` and add `DontDestroyOnLoad` logic

**BUT THIS IS NOT RECOMMENDED** because:
- You'd get multiple NetworkRunner instances
- Confusing network state
- Hard to debug
- Violates Fusion best practices

### Option C: Add DontDestroyOnLoad to LobbyManager's Runner (Hybrid)

If you really want the runner to persist and be testable from the game scene:

Add this to LobbyManager after runner creation:

```csharp
networkRunner.gameObject.name = "[NetworkRunner]";
DontDestroyOnLoad(networkRunner.gameObject);
```

This way:
- Runner persists across all scenes
- You can load game scene directly from editor
- Runner is created on-demand if missing

**Implementation:**
```csharp
private void Start()
{
    /* Check if runner already exists in scene */
    var existingRunner = FindFirstObjectByType<NetworkRunner>();
    if (existingRunner != null)
    {
        networkRunner = existingRunner;
        Debug.Log("[LobbyManager] Using existing NetworkRunner");
        return;
    }

    /* Otherwise create new runner */
    StartCoroutine(ConnectToMatchAsync());
}
```

### Recommended Setup

**I recommend Option A + Option C combined:**

1. **LobbyScene:**
   - LobbyManager creates NetworkRunner
   - Runner marked with `DontDestroyOnLoad`
   - Runner persists to game scene

2. **MultiplayerTestScene:**
   - Check if runner exists (for direct testing)
   - If not, create it with default settings
   - If yes, use existing runner

3. **No NetworkRunnerHandler in game scene needed**

### How to Implement Option A + C

In **LobbyManager.cs**:

```csharp
private System.Collections.IEnumerator ConnectToMatchAsync()
{
    if (networkRunner != null)
    {
        UpdateStatusText("Runner already exists!");
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

    /* Mark runner to persist across scene loads */
    networkRunner.gameObject.name = "[NetworkRunner]";
    DontDestroyOnLoad(networkRunner.gameObject);

    networkRunner.AddCallbacks(networkCallbackHandler);

    /* ... rest of connection code ... */
}
```

## Summary

### Team Assignment
✅ **Now fully dynamic** - Calculate teams from maxPlayers
- 4 players = 2v2
- 6 players = 3v3  
- 8 players = 4v4
- Any even number works!

### NetworkRunner
✅ **Use Option A + C** - Single persistent runner
- Created in LobbyScene
- Marked with `DontDestroyOnLoad`
- Loads game scene
- Runner persists for entire play session

**Result:** Clean, single-instance network setup that scales to any player count!

