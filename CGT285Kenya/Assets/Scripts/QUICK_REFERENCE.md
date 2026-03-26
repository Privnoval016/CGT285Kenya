# Quick Reference: Dynamic Teams & NetworkRunner

## TL;DR Setup

### 1. Set Player Count in LobbyManager
```
Inspector → LobbyManager → Max Players Per Match: 4 (or 6, 8, etc.)
```

### 2. That's it!
Everything else is automatic:
- ✅ Teams calculated from maxPlayers
- ✅ Players spawn in correct positions
- ✅ NetworkRunner persists across scenes
- ✅ Field resets use dynamic teams

## How Teams Are Assigned

With `maxPlayers = 4`:
- **Team 0** (Blue): Player ID 1, 2
- **Team 1** (Red): Player ID 3, 4

With `maxPlayers = 6`:
- **Team 0** (Blue): Player ID 1, 2, 3
- **Team 1** (Red): Player ID 4, 5, 6

**Pattern:** `team = (playerId - 1) / (maxPlayers / 2)`

## NetworkRunner: Do NOT Duplicate!

**❌ WRONG:**
```
LobbyScene
  └─ NetworkRunnerHandler (creates runner)
  
MultiplayerTestScene  
  └─ NetworkRunnerHandler (creates ANOTHER runner) ← WRONG!
```

**✅ RIGHT:**
```
LobbyScene
  └─ LobbyManager (creates runner + DontDestroyOnLoad)
      └─ Runner persists to MultiplayerTestScene
  
MultiplayerTestScene
  └─ (No runner creation needed - uses one from lobby)
```

## Scene Configuration

### LobbyScene
- Add Canvas with UI
- Add LobbyManager component
- Configure settings

**LobbyManager Inspector:**
```
Max Players Per Match: 4
Game Scene Name: MultiplayerTestScene
Network Callback Handler: (drag from prefab or create)
```

### MultiplayerTestScene
- Add GameManager (has NetworkObject)
- Add SpawnPointConfig reference
- Assign MatchRulesConfig reference
- **Delete NetworkRunnerHandler** (not needed anymore)

## Verification Checklist

- [ ] LobbyManager has maxPlayers set to your desired value
- [ ] SpawnPointConfig has enough spawn positions for your team size
- [ ] NetworkRunnerHandler removed from game scene
- [ ] MatchRulesConfig assigned to GameManager
- [ ] SpawnPointConfig assigned to both GameManager and NetworkCallbackHandler
- [ ] Both scenes added to Build Settings
- [ ] Console shows dynamic team calculation logs

## Troubleshooting

### "Player 3 is Team 0 instead of Team 1"
→ Check maxPlayers value. With maxPlayers=4, Player 3 should be Team 1. Verify in console log.

### "Multiple NetworkRunners in scene"
→ Delete NetworkRunnerHandler from game scene. Only LobbyManager should create runner.

### "Runner destroyed when loading game scene"
→ Verify LobbyManager calls `DontDestroyOnLoad(networkRunner.gameObject)`. Check logs.

### "Players don't spawn at correct positions"
→ Verify SpawnPointConfig has enough positions for your team size. Check spawn indices.

## Testing Shortcut

To test any team size:
1. Open LobbyScene
2. Set LobbyManager maxPlayers to desired value
3. Build two instances
4. Both click "Start Game"
5. Check team assignments in console
6. Verify spawns and resets work

Done! 🚀

