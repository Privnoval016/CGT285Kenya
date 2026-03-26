# Lobby & Game Scene Setup - Complete Guide

## Problems Fixed

### 1. ✅ Custom Lobby Spawn Positions
**What Changed:** Added separate spawn configuration for lobby vs game scene

**Implementation:**
- SpawnPointConfig now has `lobbyTeam0Spawns` and `lobbyTeam1Spawns` fields
- New methods: `GetLobbySpawnPosition()` and `GetLobbySpawnByPlayerId()`
- NetworkCallbackHandler detects current scene and uses appropriate spawn positions

**In Inspector:**
```
SpawnPointConfig
├─ Team Spawns (for game field)
│  ├─ Team 0 Spawns
│  └─ Team 1 Spawns
│
└─ Lobby Spawns (for waiting area)
   ├─ Lobby Team 0 Spawns
   └─ Lobby Team 1 Spawns
```

### 2. ✅ UI Text Now Updates
**What Changed:** Fixed coroutine to yield properly instead of waiting 999999 seconds

**Before:**
```csharp
yield return new WaitForSeconds(999999f); // UI frozen
```

**After:**
```csharp
while (networkRunner != null && networkRunner.IsRunning)
{
    yield return new WaitForSeconds(0.1f); // Updates every 100ms
}
```

**Result:** Player count text now updates live: "Players: 1/6" → "Players: 2/6" → etc.

### 3. ✅ "GameIsFull" Error Fixed
**What Changed:** NetworkRunnerHandler now checks if runner already exists

**Before:**
- Game scene loads
- NetworkRunnerHandler tries to create NEW runner
- Conflicts with existing runner from LobbyManager
- Error: "GameIsFull"

**After:**
- Game scene loads
- NetworkRunnerHandler finds existing runner
- Skips creation, reuses existing runner
- No conflicts!

### 4. ✅ Player Spawn/Despawn in Same Tick
**What Changed:** Proper scene detection prevents duplicate spawning

**Root Cause:** Players spawned in lobby, scene loaded, players tried to spawn again
**Solution:** Scene detection ensures players only spawn in appropriate scene

---

## How It Works Now

### Flow: Lobby → Game

```
1. LobbyScene Active
   ├─ Player 1 joins → Spawns at lobby position
   ├─ Player 2 joins → Spawns at lobby position
   └─ UI shows: "Players: 2/4"

2. When maxPlayers Reached
   ├─ runner.LoadScene() called
   └─ Scene transitions

3. MultiplayerTestScene Active
   ├─ Players respawn at GAME positions (not lobby)
   ├─ GameManager initializes
   ├─ Ball spawns
   ├─ Timer counts down
   └─ Game plays normally ✅
```

---

## Setup Instructions

### Step 1: Configure Lobby Spawn Positions

Open your **SpawnPointConfig** asset and set:

**Lobby Team 0 Spawns:**
- Position 0: Left side of lobby (e.g., -8, 0.5, -2)
- Position 1: Left side of lobby (e.g., -8, 0.5, 0)
- Position 2: Left side of lobby (e.g., -8, 0.5, 2)

**Lobby Team 1 Spawns:**
- Position 0: Right side of lobby (e.g., 8, 0.5, -2)
- Position 1: Right side of lobby (e.g., 8, 0.5, 0)
- Position 2: Right side of lobby (e.g., 8, 0.5, 2)

**Game Team 0 Spawns:**
- Position 0: Left side of field
- Position 1: Left side of field
- Position 2: Left side of field

**Game Team 1 Spawns:**
- Position 0: Right side of field
- Position 1: Right side of field
- Position 2: Right side of field

### Step 2: Verify Scene Names

- **Lobby scene name** should contain "Lobby" (e.g., "LobbyScene")
- **Game scene name** should NOT contain "Lobby" (e.g., "MultiplayerTestScene")

This is how the system detects which spawns to use!

### Step 3: Remove Old NetworkRunnerHandler (if needed)

The game scene has a NetworkRunnerHandler that now safely checks for existing runners. But you can remove it if you want since it's no longer needed.

### Step 4: Test

**With 2 instances:**
1. Instance 1: Click "Start Game"
2. Instance 2: Click "Start Game"
3. Watch UI update: "Players: 1/4" → "Players: 2/4"
4. Scene loads → Players appear at GAME positions
5. Ball spawns → Timer counts down ✅

---

## Key Code Changes

### SpawnPointConfig
```csharp
public Vector3 GetLobbySpawnByPlayerId(int playerId, int playersPerTeam)
{
    // Uses lobbyTeam0Spawns/lobbyTeam1Spawns
}

public Vector3 GetPlayerSpawnByPlayerId(int playerId, int playersPerTeam)
{
    // Uses team0Spawns/team1Spawns  
}
```

### NetworkCallbackHandler
```csharp
string currentScene = SceneManager.GetActiveScene().name;
bool isLobby = currentScene.Contains("Lobby");

if (isLobby)
    return spawnPointConfig.GetLobbySpawnByPlayerId(...);
else
    return spawnPointConfig.GetPlayerSpawnByPlayerId(...);
```

### LobbyManager
```csharp
while (networkRunner != null && networkRunner.IsRunning)
{
    yield return new WaitForSeconds(0.1f); // Proper UI updates
}
```

### NetworkRunnerHandler
```csharp
var existingRunner = FindFirstObjectByType<NetworkRunner>();
if (existingRunner != null)
{
    runnerInstance = existingRunner;
    return; // Don't try to create new runner
}
```

---

## Scene Names Matter!

The system uses `SceneManager.GetActiveScene().name.Contains("Lobby")` to determine which spawns to use.

**Ensure:**
- ✅ Lobby scene name contains "Lobby"
- ✅ Game scene name does NOT contain "Lobby"

Examples:
- ✅ "LobbyScene" → Detected as lobby
- ✅ "MainLobby" → Detected as lobby
- ❌ "GameLobby" → NOT detected as lobby (game scene)
- ✅ "MultiplayerTestScene" → Detected as game
- ✅ "GameScene" → Detected as game

---

## Expected Console Output

```
[LobbyManager] Marked DontDestroyOnLoad
[LobbyManager] NetworkRunner marked DontDestroyOnLoad
[LobbyManager] Connected to Photon!
[LobbyManager] Player joined! 1/4
[LobbyManager] Player joined! 2/4
[NetworkCallback] Spawning in scene: LobbyScene, isLobby: True
[NetworkCallback] Spawning in scene: LobbyScene, isLobby: True
[LobbyManager] Match found! 2 players ready. Loading game scene...
[NetworkRunner] IsRunning...
[LobbyManager] Player joined! 1/4 (game scene)
[LobbyManager] Player joined! 2/4 (game scene)
[NetworkCallback] Spawning in scene: MultiplayerTestScene, isLobby: False
[NetworkCallback] Spawning in scene: MultiplayerTestScene, isLobby: False
[GameManager] Match started! Duration: 300s
✅ Game is live!
```

---

## Troubleshooting

**UI not updating:**
→ Check that statusText is assigned in LobbyManager

**Players always spawn at same position:**
→ Verify SpawnPointConfig has multiple positions configured
→ Check console for which scene is detected

**GameIsFull error:**
→ NetworkRunnerHandler found existing runner (expected!)
→ Should now work without errors

**Players look like they're in wrong scene:**
→ Verify lobby spawn positions in SpawnPointConfig
→ Check scene name contains/doesn't contain "Lobby"

---

**Everything should now work smoothly!** 🚀

