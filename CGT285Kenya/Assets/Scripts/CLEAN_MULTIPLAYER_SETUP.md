# Clean Multiplayer Setup - Complete Guide

## The Fix Applied

**Problem:** Manual scene loading + Fusion scene loading = race condition = crashes

**Solution:** Let Fusion's NetworkSceneManagerDefault handle ALL scene loading. Don't manually load scenes.

## What You Need to Do

### ✅ Step 1: Verify GameManager in Both Scenes

**LobbyScene should have:**
```
Hierarchy:
├─ Canvas (UI)
├─ LobbyManager
└─ GameManager ← MUST be here
```

**MultiplayerTestScene should have:**
```
Hierarchy:
├─ GameManager ← Already here
├─ Ball prefab reference setup
├─ Player spawn points configured
└─ (other game objects)
```

**If GameManager is NOT in LobbyScene:**
1. Open MultiplayerTestScene
2. Find GameManager in hierarchy
3. Ctrl+D (or Cmd+D) to duplicate
4. Open LobbyScene
5. Paste it (Ctrl+V)
6. Save both scenes

### ✅ Step 2: Verify Build Settings

Go to **File → Build Settings** and verify:

```
Scene 0: LobbyScene
Scene 1: MultiplayerTestScene
```

**IMPORTANT:** Both scenes MUST be added to Build Settings!

### ✅ Step 3: Verify Scene Names Match

In LobbyManager inspector, check:
- **Game Scene Name:** "MultiplayerTestScene"
- This MUST match the actual scene name exactly

### ✅ Step 4: Configure LobbyManager

In LobbyScene, select LobbyManager and verify:
```
UI References
├─ Status Text: (assign TMP_Text)
├─ Start Button: (assign Button)
└─ Session Name Input: (assign TMP_InputField)

Scene Names
└─ Game Scene Name: "MultiplayerTestScene"

Network Settings
├─ Runner Prefab: (optional, can be empty)
├─ Network Callback Handler Prefab: (optional, can be empty)
├─ Max Players Per Match: 6
└─ Game Mode: Shared
```

### ✅ Step 5: Verify NetworkCallbackHandler

Make sure it has these fields assigned:
```
Ability Assignment
└─ Ability Config: (assign your AbilityAssignmentConfig)

Spawn Configuration
└─ Spawn Point Config: (assign your SpawnPointConfig)
```

## How It Works Now (Clean Flow)

```
1. Player clicks "Start Game" in LobbyScene
2. LobbyManager creates NetworkRunner
3. LobbyManager creates NetworkCallbackHandler
4. StartGameArgs created with scene reference
5. NetworkSceneManagerDefault ADDED to runner
6. networkRunner.StartGame() called
   ├─ Fusion loads the scene automatically
   ├─ OnPlayerJoined fires WHEN SCENE IS READY
   ├─ Player spawns in correct scene ✅
   └─ No manual scene loading = no race condition ✅
7. Game runs smoothly
```

## Expected Console Output

```
[LobbyManager] NetworkRunner marked with DontDestroyOnLoad
[LobbyManager] NetworkCallbackHandler instantiated from prefab
[LobbyManager] Starting game with scene build index: 1
[NetworkRunner] Calling StartGame...
[NetworkCallback] Player 1 joined
[GameManager] Match started! Duration: 300s
✅ Clean, no errors!
```

## If You Still Get Errors

### Error: "NullReferenceException in NetworkSceneManagerDefault"
→ GameManager is missing from LobbyScene
→ Add it (see Step 1)

### Error: "Scene not found in Build Settings"
→ MultiplayerTestScene not in Build Settings
→ Add it (see Step 2)

### Error: "Code 104 ServerLogic"
→ Scene name mismatch or GameManager missing
→ Verify Steps 2 and 3

### Player spawns in wrong place or duplicates
→ Likely caused by manual scene loading
→ Make sure you're using the latest LobbyManager (just updated)

## Quick Checklist

- [ ] GameManager in LobbyScene
- [ ] GameManager in MultiplayerTestScene
- [ ] Both scenes in Build Settings (indices 0 and 1)
- [ ] LobbyManager has game scene name set correctly
- [ ] LobbyManager has UI elements assigned
- [ ] NetworkCallbackHandler has config assets assigned
- [ ] Run game, click "Start Game"
- [ ] Player spawns cleanly in game scene ✅
- [ ] No errors or weird behavior

## The Key Principle

**Never manually manage scene loading when using Fusion's NetworkSceneManagerDefault.**

Just provide the scene reference in StartGameArgs and let Fusion handle it completely. This prevents race conditions and ensures clean, synchronized scene loading across all clients.

---

If you've done all these steps and still have issues, let me know what errors you're seeing! 🚀

