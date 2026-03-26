# Complete LobbyManager + NetworkCallbackHandler Setup

## What Changed

The original setup required you to manually place NetworkCallbackHandler in a scene and drag it to LobbyManager - which was confusing.

**Now:** LobbyManager automatically creates and manages it for you.

---

## How It Works: The Three Fallback Strategies

When LobbyManager.ConnectToMatchAsync() runs, it executes this logic:

### Step 1: Search for Existing Instance
```csharp
NetworkCallbackHandler callbackHandler = FindFirstObjectByType<NetworkCallbackHandler>();
```
- Searches all active GameObjects for an existing NetworkCallbackHandler
- **If found:** Uses it (and marks with DontDestroyOnLoad)
- **If not found:** Proceeds to Step 2

### Step 2: Check if Prefab is Assigned
```csharp
if (networkCallbackHandlerPrefab != null)
{
    callbackHandler = Instantiate(networkCallbackHandlerPrefab);
}
```
- Checks if you assigned a prefab in the inspector
- **If prefab exists:** Instantiates it
- **If not assigned:** Proceeds to Step 3

### Step 3: Create Dynamically as Fallback
```csharp
else
{
    var go = new GameObject("[NetworkCallbackHandler]");
    callbackHandler = go.AddComponent<NetworkCallbackHandler>();
}
```
- Creates a brand new GameObject
- Adds NetworkCallbackHandler component dynamically
- Assigns it a name for debugging

### Step 4: Persist Across Scenes
```csharp
DontDestroyOnLoad(callbackHandler.gameObject);
networkRunner.AddCallbacks(callbackHandler);
```
- Marks it to survive scene loads
- Adds it to the runner's callbacks

---

## Setup Guide: Three Options

### ✅ Option 1: Use a Prefab (Recommended for Production)

**Best for:** Professional projects, reusability, clean hierarchy

**Step 1: Create the Prefab**
1. In your MultiplayerTestScene, select the NetworkCallbackHandler GameObject
2. Drag it to `Assets/Prefabs/NetworkCallbackHandler.prefab`
3. Delete the NetworkCallbackHandler instance from the scene

**Step 2: Assign to LobbyManager**
1. Open LobbyScene
2. Select LobbyManager GameObject
3. In Inspector, find **Network Settings** section
4. Drag the NetworkCallbackHandler.prefab to **Network Callback Handler Prefab** field

**Step 3: Clean Up**
1. Make sure MultiplayerTestScene does NOT have a NetworkCallbackHandler instance
2. LobbyManager will create it when needed

**Result in Console:**
```
[LobbyManager] NetworkCallbackHandler created from prefab
```

---

### ✅ Option 2: No Setup Required (Automatic)

**Best for:** Rapid prototyping, minimal configuration

**Step 1: Leave it Empty**
1. Open LobbyScene
2. Select LobbyManager
3. Leave **Network Callback Handler Prefab** field empty (don't assign anything)

**Step 2: That's It**
- LobbyManager will auto-create NetworkCallbackHandler dynamically
- No setup, no prefabs, no confusion

**Result in Console:**
```
[LobbyManager] NetworkCallbackHandler created dynamically (no prefab assigned)
```

**Pro Tip:** You can switch to Option 1 later just by assigning the prefab.

---

### ✅ Option 3: Put It in the Scene (Visual)

**Best for:** Debugging, visual clarity, simple setups

**Step 1: Add to LobbyScene**
1. Open LobbyScene
2. Create new empty GameObject: `NetworkCallbackHandler`
3. Add component: `Networking.NetworkCallbackHandler`
4. Leave **Network Callback Handler Prefab** empty

**Step 2: Leave the Game Scene Untouched**
1. MultiplayerTestScene should NOT have NetworkCallbackHandler
2. LobbyManager will find the one in LobbyScene and reuse it

**Step 3: It Works**
- LobbyManager searches, finds it in LobbyScene
- Uses it with DontDestroyOnLoad
- Persists to MultiplayerTestScene

**Result in Console:**
```
[LobbyManager] Using existing NetworkCallbackHandler from scene
```

---

## Decision Tree: Which Option Should I Use?

```
Are you building for production/shipping?
├─ YES → Use Option 1 (Prefab)
│  └─ Most professional, reusable, clean
│
└─ NO → Use Option 2 or 3
   ├─ Want zero setup? → Option 2 (Automatic)
   ├─ Want visual clarity? → Option 3 (Scene)
```

---

## What Gets Created

No matter which option you choose, LobbyManager creates these objects:

```
During ConnectToMatchAsync():

[NetworkRunner]
├─ Name: [NetworkRunner]
├─ Component: NetworkRunner
├─ Parent: None (root of hierarchy)
└─ DontDestroyOnLoad: ✅ YES

[NetworkCallbackHandler]
├─ Name: [NetworkCallbackHandler]
├─ Component: Networking.NetworkCallbackHandler
├─ Component: References to abilityConfig, spawnPointConfig
├─ Parent: None (root of hierarchy)
└─ DontDestroyOnLoad: ✅ YES
```

Both persist to the game scene automatically.

---

## Flow Diagram: Complete Lifecycle

```
┌──────────────────────────────────────────────────────────┐
│ User clicks "Start Game" in LobbyScene                  │
└──────────────────────────────────────────────────────────┘
                         ↓
┌──────────────────────────────────────────────────────────┐
│ LobbyManager.OnStartGamePressed()                        │
│ └─ StartCoroutine(ConnectToMatchAsync())                │
└──────────────────────────────────────────────────────────┘
                         ↓
        ┌────────────────────────────┐
        │ Check for existing runner  │
        └────────────────────────────┘
           Found? NO ↓
        ┌────────────────────────────┐
        │ Create NetworkRunner       │
        │ ├─ From prefab OR          │
        │ └─ Dynamically             │
        └────────────────────────────┘
           ↓ DontDestroyOnLoad
        ┌────────────────────────────────────┐
        │ Check for existing callback handler│
        └────────────────────────────────────┘
           Found? → Use it
           Not found? ↓
        ┌────────────────────────────────────┐
        │ Create NetworkCallbackHandler      │
        │ ├─ From prefab (Option 1)    OR    │
        │ └─ Dynamically (Option 2)    OR    │
        │ └─ From scene (Option 3)           │
        └────────────────────────────────────┘
           ↓ DontDestroyOnLoad
        ┌────────────────────────────┐
        │ runner.AddCallbacks()       │
        └────────────────────────────┘
           ↓
        ┌────────────────────────────┐
        │ runner.StartGame()          │
        └────────────────────────────┘
           ↓
        ┌────────────────────────────┐
        │ Wait for second player      │
        └────────────────────────────┘
           ↓
        ┌────────────────────────────┐
        │ OnPlayerJoined() called     │
        └────────────────────────────┘
           ↓
        ┌────────────────────────────────┐
        │ LoadScene(gameSceneName)        │
        │ ├─ Runner persists ✅          │
        │ └─ Callback persists ✅        │
        └────────────────────────────────┘
           ↓
        ┌────────────────────────────────┐
        │ MultiplayerTestScene starts     │
        │ ├─ Both objects are present     │
        │ ├─ Callbacks wired up          │
        │ └─ Network working ✅          │
        └────────────────────────────────┘
```

---

## Troubleshooting

### "Multiple NetworkCallbackHandlers in hierarchy"
**Cause:** Option 3 (scene instance) + Option 1 (prefab) at same time
**Fix:** Choose ONE option. Either:
- Use scene instance (delete prefab field)
- OR use prefab (delete scene instance)

### "NetworkCallbackHandler not initialized"
**Cause:** Object created but callbacks not added
**Fix:** Not possible - LobbyManager always calls AddCallbacks()

### "Game loads but no networking"
**Cause:** Callback handler not persisting
**Fix:** Check if DontDestroyOnLoad is being called. Look for console log:
```
[LobbyManager] NetworkCallbackHandler created...
[LobbyManager] Using existing...
```

### "Script shows as missing component"
**Cause:** NetworkCallbackHandler script not found
**Fix:** Ensure it's in `Assets/Scripts/Networking/NetworkCallbackHandler.cs`

---

## Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Where to put callback handler?** | Confusing | Auto-handled by LobbyManager |
| **Manual setup required?** | Yes (required) | No (optional) |
| **Prefab needed?** | Unclear | Optional, fallback works |
| **Lifecycle management?** | Manual | Automatic (DontDestroyOnLoad) |
| **Error prone?** | High | Zero (three fallbacks) |

---

## One Last Thing: Inspector Setup

After choosing your option, LobbyManager inspector should look like:

```
LobbyManager
├─ UI References
│  ├─ Status Text: [TextMeshProUGUI]
│  ├─ Start Button: [Button]
│  └─ Session Name Input: [InputField]
│
├─ Scene Names
│  └─ Game Scene Name: "MultiplayerTestScene"
│
└─ Network Settings
   ├─ Runner Prefab: [NetworkRunner] (optional)
   ├─ Network Callback Handler Prefab: 
   │     Option 1: [NetworkCallbackHandler.prefab]
   │     Option 2 & 3: (empty)
   ├─ Max Players Per Match: 6
   └─ Game Mode: Shared
```

---

**You now have three clean options for setup - pick the one you like!** 🎉

No more confusion about where to put NetworkCallbackHandler - LobbyManager handles it all automatically!

