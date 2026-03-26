# GameManager Initialization Diagnostic

## Checklist: GameManager in MultiplayerTestScene

Open **MultiplayerTestScene** and select **GameManager**. In the Inspector, verify ALL of these:

### Required Components
- [ ] Has **NetworkObject** component
- [ ] Has **GameManager** script component
- [ ] Has **CharacterController** on... wait, that's for Player, not GameManager

### Inspector Fields - Prefabs (Critical!)
```
Prefabs
├─ Player Prefab: [MUST be assigned - your NetworkPlayer prefab]
└─ Ball Prefab: [MUST be assigned - your NetworkBallController prefab]
```

**If these are empty → Ball won't spawn!**

### Inspector Fields - Configuration (Critical!)
```
Configuration
├─ Match Rules Config: [MUST be assigned - your MatchRulesConfig asset]
└─ Spawn Point Config: [MUST be assigned - your SpawnPointConfig asset]
```

**If these are empty → Match won't initialize properly!**

---

## Checklist: GameManager in LobbyScene

Open **LobbyScene** and verify GameManager exists here too:

- [ ] GameManager GameObject exists in hierarchy
- [ ] Has NetworkObject component
- [ ] Has GameManager script
- [ ] Same prefab/config assignments as MultiplayerTestScene (copy from there if needed)

---

## Most Likely Problem: Missing Prefab/Config Assignments

**Symptom:** Scene loads, Code 104 error, no ball, timer doesn't count

**Cause:** Player/Ball prefabs not assigned to GameManager

**Fix:**
1. Open MultiplayerTestScene
2. Select GameManager
3. In Inspector → Prefabs section:
   - **Player Prefab:** Drag your NetworkPlayer prefab here
   - **Ball Prefab:** Drag your NetworkBallController prefab here
4. In Inspector → Configuration section:
   - **Match Rules Config:** Assign MatchRulesConfig asset
   - **Spawn Point Config:** Assign SpawnPointConfig asset
5. Copy exact same assignments to GameManager in LobbyScene
6. Save both scenes

---

## Debug: Check the Console

When you click "Start Game", you should see:

```
[GameManager] Match started! Duration: 300s
```

If you DON'T see this, GameManager.Spawned() is never being called.

---

## What Happens in Spawned()

```csharp
public override void Spawned()
{
    if (Object.HasStateAuthority)
    {
        StartMatch(); // This initializes everything
    }
}

private void StartMatch()
{
    Team0Score = 0;
    Team1Score = 0;
    
    MatchTimer = TickTimer.CreateFromSeconds(Runner, duration);
    MatchActive = true;
    
    SpawnBall(); // Creates ball - needs BallPrefab assigned!
    
    Debug.Log($"[GameManager] Match started! Duration: {duration}s");
}
```

If BallPrefab is null, SpawnBall() does nothing and you get no ball.

---

## Instructions: Fix It Now

### Step 1: Open MultiplayerTestScene
1. File → Open Scene → MultiplayerTestScene

### Step 2: Find GameManager
1. In Hierarchy, find "GameManager"
2. Click it to select

### Step 3: Assign Prefabs
1. In Inspector, scroll to **Prefabs** section
2. **Player Prefab:** 
   - Click the circle icon next to field
   - Search "NetworkPlayer" (the prefab)
   - Select it
3. **Ball Prefab:**
   - Click the circle icon
   - Search "NetworkBallController" (the prefab)
   - Select it

### Step 4: Assign Configs
1. In Inspector, scroll to **Configuration** section
2. **Match Rules Config:**
   - Click circle icon
   - Search "MatchRulesConfig"
   - Select it
3. **Spawn Point Config:**
   - Click circle icon
   - Search "SpawnPointConfig"
   - Select it

### Step 5: Copy to LobbyScene
1. Open LobbyScene
2. Find GameManager
3. Repeat Steps 3-4 (assign same prefabs/configs)

### Step 6: Save & Test
1. Save all scenes
2. Play game
3. Click "Start Game"
4. You should see:
   - Scene loads cleanly
   - Ball appears
   - Timer counts down
   - No disconnect errors

---

## If You Don't Have These Assets

You need to create them first:

**NetworkPlayer Prefab:**
- Your player character model with NetworkPlayer script

**NetworkBallController Prefab:**
- The ball GameObject with NetworkBallController script

**MatchRulesConfig:**
- Already created (should be in Assets/ScriptableObjects or similar)

**SpawnPointConfig:**
- Already created (should be in Assets/ScriptableObjects or similar)

If any are missing, create/find them and assign.

---

## Expected Result After Fix

```
Console output:
[LobbyManager] NetworkRunner marked with DontDestroyOnLoad
[LobbyManager] NetworkCallbackHandler created dynamically
[LobbyManager] Starting game with scene build index: 1
[NetworkRunner] Calling StartGame...
[NetworkCallback] Player 1 joined
[GameManager] Match started! Duration: 300s
✅ No errors!

Game visuals:
- Player visible in scene
- Ball visible in center
- Timer counting down
- Score showing 0-0
```

---

**Do this now and let me know what happens!** 🚀

