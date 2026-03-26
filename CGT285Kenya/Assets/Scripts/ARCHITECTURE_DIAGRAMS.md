# Dynamic Team Assignment Architecture

## Team Assignment Flow

```
NetworkRunner starts with maxPlayers = 6
         ↓
Player joins with PlayerId = 3
         ↓
┌─────────────────────────────────────────────────────────┐
│ Calculate Team                                          │
│ ─────────────────────────────────────────              │
│ playersPerTeam = maxPlayers / 2 = 6 / 2 = 3           │
│ zeroBasedId = playerId - 1 = 3 - 1 = 2                │
│ team = zeroBasedId / playersPerTeam = 2 / 3 = 0       │
│ positionIndex = zeroBasedId % playersPerTeam = 2 % 3 = 2 │
└─────────────────────────────────────────────────────────┘
         ↓
Player assigned to Team 0, Position 2
         ↓
Spawn at Team0Spawns[2] from SpawnPointConfig
```

## Multi-Player Example: 3v3 (maxPlayers=6)

```
┌──────────────────────────────────────────────────────────────┐
│ Player Joins                                                 │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│ PlayerId 1: team = 0/3 = 0, pos = 0 % 3 = 0               │
│   → Team 0 (Blue) Position 0                               │
│                                                              │
│ PlayerId 2: team = 1/3 = 0, pos = 1 % 3 = 1               │
│   → Team 0 (Blue) Position 1                               │
│                                                              │
│ PlayerId 3: team = 2/3 = 0, pos = 2 % 3 = 2               │
│   → Team 0 (Blue) Position 2                               │
│                                                              │
│ PlayerId 4: team = 3/3 = 1, pos = 3 % 3 = 0               │
│   → Team 1 (Red) Position 0                                │
│                                                              │
│ PlayerId 5: team = 4/3 = 1, pos = 4 % 3 = 1               │
│   → Team 1 (Red) Position 1                                │
│                                                              │
│ PlayerId 6: team = 5/3 = 1, pos = 5 % 3 = 2               │
│   → Team 1 (Red) Position 2                                │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

## NetworkRunner Lifecycle

```
┌─────────────────────────────────────────────────────────────┐
│                      LOBBY SCENE                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Player 1 presses "Start Game" in LobbyManager             │
│              ↓                                              │
│  LobbyManager.ConnectToMatchAsync()                        │
│              ↓                                              │
│  Creates NetworkRunner                                     │
│  DontDestroyOnLoad(networkRunner.gameObject)               │
│              ↓                                              │
│  Player 2 joins same session                              │
│              ↓                                              │
│  Both players connected                                    │
│              ↓                                              │
│  SceneManager.LoadScene("MultiplayerTestScene")            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
                         ↓
         ⚡ NetworkRunner Persists ⚡
                         ↓
┌─────────────────────────────────────────────────────────────┐
│                    GAME SCENE                               │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  NetworkRunner already exists from lobby                   │
│              ↓                                              │
│  NetworkCallbackHandler.OnPlayerJoined()                   │
│              ↓                                              │
│  Calculate team using runner.SessionInfo.MaxPlayers        │
│              ↓                                              │
│  Spawn player at correct position                          │
│              ↓                                              │
│  Both players visible, networked, and ready to play        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Field Reset with Dynamic Teams

```
                    Goal Scored!
                        ↓
              GameManager.OnGoalScored()
                        ↓
              RPC_ResetField() called
                        ↓
    ┌──────────────────────────────────────┐
    │ For each player in scene:            │
    ├──────────────────────────────────────┤
    │ playersPerTeam = GetPlayersPerTeam() │
    │ spawnPos = GetPlayerSpawnByPlayerId( │
    │   playerId, playersPerTeam)          │
    │ player.ResetToSpawnPosition(pos)     │
    └──────────────────────────────────────┘
                        ↓
              Ball reset to center
                        ↓
              Play resumes with players
              in original starting positions
```

## Configuration Flow

```
┌─ Inspector ────────────────────────────────────────────────┐
│                                                            │
│  LobbyManager.maxPlayersPerMatch = 4                      │
│         ↓                                                  │
│  Passed to NetworkRunner.StartGame(maxPlayers)            │
│         ↓                                                  │
│  Stored in runner.SessionInfo.MaxPlayers = 4              │
│         ↓                                                  │
│  ┌────────────────────────────────────────────┐           │
│  │ When Player Spawns:                        │           │
│  │ playersPerTeam = maxPlayers / 2 = 2        │           │
│  │ This value flows to:                       │           │
│  │  • NetworkCallbackHandler.GetSpawnPosition()│          │
│  │  • NetworkPlayer.Spawned()                 │           │
│  │  • GameManager.RPC_ResetField()            │           │
│  │  • SpawnPointConfig.GetPlayerSpawnByPlayerId()         │
│  └────────────────────────────────────────────┘           │
│         ↓                                                  │
│  All systems use same playersPerTeam value                │
│  ✅ Deterministic across all clients                      │
│                                                            │
└────────────────────────────────────────────────────────────┘
```

## Code Path: Player Spawn with Dynamic Teams

```
NetworkCallbackHandler.OnPlayerJoined(player)
  ↓
SpawnPlayer(runner, player)
  ↓
GetSpawnPosition(player, runner)
  ├─ playersPerTeam = runner.SessionInfo.MaxPlayers / 2
  ├─ zeroBasedId = player.PlayerId - 1
  ├─ team = zeroBasedId / playersPerTeam
  ├─ positionIndex = zeroBasedId % playersPerTeam
  └─ spawnPointConfig.GetPlayerSpawnByPlayerId(playerId, playersPerTeam)
    └─ Returns Team[team].positions[positionIndex]
  ↓
runner.Spawn(playerPrefab, spawnPos, player)
  ↓
NetworkPlayer.Spawned()
  ├─ playersPerTeam = runner.SessionInfo.MaxPlayers / 2
  ├─ zeroBasedId = InputAuthority.PlayerId - 1
  ├─ Team = zeroBasedId / playersPerTeam
  └─ SetupTeamColor()
```

## Example: 2v2 Match (maxPlayers=4)

```
Session Created: maxPlayers = 4
  ↓ playersPerTeam = 4/2 = 2

Player 1 joins
  ├─ Team = (1-1)/2 = 0 ✓ Team 0 (Blue)
  ├─ Position = (1-1)%2 = 0
  └─ Spawn at Team0Spawns[0]

Player 2 joins
  ├─ Team = (2-1)/2 = 0 ✓ Team 0 (Blue)
  ├─ Position = (2-1)%2 = 1
  └─ Spawn at Team0Spawns[1]

Player 3 joins
  ├─ Team = (3-1)/2 = 1 ✓ Team 1 (Red)
  ├─ Position = (3-1)%2 = 0
  └─ Spawn at Team1Spawns[0]

Player 4 joins
  ├─ Team = (4-1)/2 = 1 ✓ Team 1 (Red)
  ├─ Position = (4-1)%2 = 1
  └─ Spawn at Team1Spawns[1]

┌────────────────┬────────────────┐
│   Team 0       │   Team 1       │
│   (Blue)       │   (Red)        │
├────────────────┼────────────────┤
│ Player 1, 2    │ Player 3, 4    │
│ 2v2 Ready! ✅  │                │
└────────────────┴────────────────┘
```

## Summary

**Before:** Team assignment hardcoded to PlayerId <= 2 (3v3 only)
```
Team = (playerId <= 2) ? 0 : 1  ❌ NOT SCALABLE
```

**Now:** Team assignment dynamic from maxPlayers
```
playersPerTeam = maxPlayers / 2
Team = (playerId - 1) / playersPerTeam  ✅ SCALABLE
```

Works with: 2v2, 3v3, 4v4, 5v5, 10v10, or any even number! 🎉

