# How LobbyManager Creates NetworkCallbackHandler

Great question! Let me clarify exactly how this works.

## The Problem You Found

Looking at the old setup:
```csharp
[SerializeField] private NetworkCallbackHandler networkCallbackHandler;
```

This requires you to manually drag the NetworkCallbackHandler into the inspector, but if it's not in any scene, where do you put it?

**Confusion:** Do I put it in LobbyScene? MultiplayerTestScene? Both?

---

## The Solution: LobbyManager Creates It

I've updated LobbyManager to **dynamically create** the NetworkCallbackHandler. Here's how it works:

### Code Flow

```csharp
/* New inspector field - accepts a prefab */
[SerializeField] private NetworkCallbackHandler networkCallbackHandlerPrefab;

// In ConnectToMatchAsync():

/* Try to find existing callback handler */
NetworkCallbackHandler callbackHandler = FindFirstObjectByType<NetworkCallbackHandler>();

if (callbackHandler == null)
{
    /* If not found, create one from prefab */
    if (networkCallbackHandlerPrefab != null)
    {
        callbackHandler = Instantiate(networkCallbackHandlerPrefab);
        DontDestroyOnLoad(callbackHandler.gameObject);
    }
    else
    {
        /* Fallback: create it dynamically if no prefab */
        var go = new GameObject("[NetworkCallbackHandler]");
        callbackHandler = go.AddComponent<NetworkCallbackHandler>();
        DontDestroyOnLoad(go);
    }
}

/* Either way, we now have a callbackHandler and can use it */
networkRunner.AddCallbacks(callbackHandler);
```

---

## Setup Options (Choose ONE)

### **Option A: Use Prefab (Recommended)**

1. Create a prefab from NetworkCallbackHandler
   - In your game scene, select the NetworkCallbackHandler GameObject
   - Drag it to `Assets/Prefabs/NetworkCallbackHandler.prefab`
   - Delete the instance from the scene

2. Assign to LobbyManager
   - Open LobbyScene
   - Select LobbyManager
   - Drag the prefab to **Network Callback Handler Prefab** field

3. Result
   - LobbyManager will instantiate it when needed
   - It persists across scenes with `DontDestroyOnLoad`

### **Option B: Leave Empty (Fallback)**

1. Don't assign anything to the prefab field

2. LobbyManager creates it dynamically
   - Automatically adds NetworkCallbackHandler component
   - Creates a new GameObject called `[NetworkCallbackHandler]`
   - Marks it with `DontDestroyOnLoad`

3. Result
   - Works fine, but no inspector control
   - Less flexible if you want to customize it

### **Option C: Put it in Scene (Simple)**

1. Keep a NetworkCallbackHandler in both scenes
   - LobbyScene: Add a NetworkCallbackHandler GameObject
   - MultiplayerTestScene: Add a NetworkCallbackHandler GameObject

2. LobbyManager finds and uses the existing one
   - `FindFirstObjectByType<NetworkCallbackHandler>()` finds it
   - Uses the scene instance instead of creating new

3. Result
   - Visual, obvious, easy to debug
   - Duplicate objects but it's OK (stateless callbacks)

---

## How It Works: Priority Order

When LobbyManager.ConnectToMatchAsync() runs:

```
1. Search scene for existing NetworkCallbackHandler
   └─ If found → Use it (with DontDestroyOnLoad)
   
2. If not found, check if prefab assigned
   └─ If assigned → Instantiate from prefab
   └─ If not assigned → Create dynamically
   
3. Add callbacks to runner
   └─ networkRunner.AddCallbacks(callbackHandler)
```

**Result:** One NetworkCallbackHandler is guaranteed to exist and be connected.

---

## Lifecycle Diagram

```
┌─────────────────────────────────────────────────────┐
│ User clicks "Start Game" in LobbyScene              │
└─────────────────────────────────────────────────────┘
                       ↓
┌─────────────────────────────────────────────────────┐
│ LobbyManager.ConnectToMatchAsync()                  │
└─────────────────────────────────────────────────────┘
                       ↓
        ┌──────────────┴──────────────┐
        ↓                             ↓
    Does callback handler exist?   (Use FindFirstObjectByType)
        │
    NO  │  YES
        │   └─→ Use existing instance
        ↓       └─→ DontDestroyOnLoad
    Create new
        │
        ├─ If prefab assigned
        │  └─→ Instantiate from prefab
        │
        └─ If no prefab
           └─→ Create dynamic GameObject
        
        Both: DontDestroyOnLoad
                       ↓
        Add to runner.AddCallbacks()
                       ↓
        Create NetworkRunner
                       ↓
        Mark runner: DontDestroyOnLoad
                       ↓
        StartGame() on runner
                       ↓
        Wait for second player to join
                       ↓
        Load MultiplayerTestScene
                       ↓
        Runner + CallbackHandler persist
                       ↓
        Game plays with networked players
```

---

## Implementation Summary

### Before
```csharp
[SerializeField] private NetworkCallbackHandler networkCallbackHandler;

// User had to:
// 1. Create NetworkCallbackHandler in scene
// 2. Drag to inspector
// 3. Hope it persists correctly
// ❌ Confusing, error-prone
```

### After
```csharp
[SerializeField] private NetworkCallbackHandler networkCallbackHandlerPrefab;

// LobbyManager handles it:
// 1. Searches for existing instance
// 2. If not found, creates from prefab or dynamically
// 3. Marks with DontDestroyOnLoad automatically
// ✅ Clear, automatic, foolproof
```

---

## Setup Checklist

Choose ONE approach:

### ✅ Approach A: Prefab (Best for production)
- [ ] Create NetworkCallbackHandler prefab
- [ ] Assign to LobbyManager.networkCallbackHandlerPrefab
- [ ] Remove from all scenes
- [ ] Test: Logs should show "created from prefab"

### ✅ Approach B: Automatic (Simplest)
- [ ] Leave networkCallbackHandlerPrefab empty
- [ ] LobbyManager auto-creates it
- [ ] Test: Logs should show "created dynamically"

### ✅ Approach C: Scene Instances (Visual)
- [ ] Add NetworkCallbackHandler to LobbyScene
- [ ] Add NetworkCallbackHandler to MultiplayerTestScene
- [ ] Leave prefab field empty
- [ ] Test: Logs should show "Using existing"

---

## Debug Logs to Look For

Successful setup will show:
```
[LobbyManager] NetworkRunner marked with DontDestroyOnLoad
[LobbyManager] NetworkCallbackHandler created from prefab
(OR: "created dynamically" or "Using existing")
[NetworkCallback] Player 1 joined
(Game loads successfully)
```

---

## Why This Design?

✅ **Flexible** - Works with prefab, scene instance, or dynamic
✅ **Foolproof** - Handles all cases automatically
✅ **Clean** - No duplicate setup required
✅ **Persistent** - Both runner and callbacks survive scene loads
✅ **Professional** - Production-ready architecture

You no longer need to worry about where to put NetworkCallbackHandler - LobbyManager handles it for you! 🎉

