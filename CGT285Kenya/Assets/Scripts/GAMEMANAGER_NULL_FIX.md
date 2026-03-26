# GameManager Initialization Error - Solution

## The Problem

Error: `GameManager.Instance is null!`

This happens when a player joins before GameManager has been initialized. The issue is a **scene loading order problem**.

## Why It Happens

Current flow:
```
1. LobbyScene: Player clicks "Start Game"
2. LobbyManager creates NetworkRunner
3. NetworkRunner.StartGame() is called
4. Player joins → OnPlayerJoined fires IMMEDIATELY
5. OnPlayerJoined tries to access GameManager.Instance
6. BUT GameManager is in MultiplayerTestScene, which hasn't loaded yet!
7. GameManager is null → Error
```

## The Solution

You must ensure GameManager exists in the scene BEFORE players join. Here are your options:

### ✅ Option 1: Put GameManager in LobbyScene Too (Recommended Quick Fix)

**Simple approach:**
1. Open MultiplayerTestScene
2. Select GameManager
3. Duplicate it
4. Open LobbyScene
5. Paste it there
6. Save both scenes

**Result:** 
- LobbyScene has a GameManager instance
- When players join in lobby, GameManager is available
- When game scene loads, both instances exist (fine, the lobby one persists)

**Pros:**
- Quickest fix
- Works immediately
- No code changes

**Cons:**
- Duplicate GameManager
- But it's okay because GameManager is a singleton (only one instance active)

---

### ✅ Option 2: Proper Initialization (Better Long-term)

Change the LobbyManager to load the game scene BEFORE starting the network game.

**Code fix in LobbyManager:**
```csharp
private IEnumerator ConnectToMatchAsync()
{
    /* 1. Load game scene FIRST */
    SceneManager.LoadScene(gameSceneName, LoadSceneMode.Additive);
    yield return new WaitForSeconds(0.5f); /* Wait for scene to load */
    
    /* 2. THEN create runner and start networking */
    /* (existing code) */
    networkRunner = Instantiate(runnerPrefab);
    DontDestroyOnLoad(networkRunner.gameObject);
    
    /* ... rest of connection code ... */
}
```

**Result:**
- Game scene loads first
- GameManager initializes  
- Players join to an already-loaded scene
- Clean architecture

---

### ✅ Option 3: Lazy Initialization

Keep GameManager spawning only when needed.

**In NetworkCallbackHandler.SpawnPlayer:**
```csharp
if (GameManager.Instance == null)
{
    /* Spawn GameManager dynamically */
    var gmGo = new GameObject("GameManager");
    var gm = gmGo.AddComponent<GameManager>();
    var no = gmGo.AddComponent<NetworkObject>();
    Runner.Spawn(no);
}
```

**Result:**
- GameManager created on-demand
- No manual setup needed
- Works with any scene

---

## Recommended Quick Fix: Option 1

Since you just want to get it working:

1. **Open MultiplayerTestScene**
2. **Select GameManager in hierarchy**
3. **Ctrl+D (or Cmd+D) to duplicate it**
4. **Open LobbyScene**
5. **Paste the GameManager there** (or drag from project if you made it a prefab)
6. **Save both scenes**

That's it! Now GameManager exists in both scenes and the error is fixed.

---

## Verification

After applying Option 1:

```
LobbyScene Hierarchy:
├─ Canvas (UI)
├─ LobbyManager
└─ GameManager ← Added this

MultiplayerTestScene Hierarchy:
├─ GameManager ← Already here
└─ (other game objects)
```

When you start:
```
[NetworkCallback] Player 1 joined
[GameManager] Match started! Duration: 300s
✅ No more null reference error!
```

---

## Why This Works

GameManager is a **Singleton**:
```csharp
public static GameManager Instance { get; private set; }

private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject); // Only one instance
        return;
    }
    Instance = this;
}
```

So even though there are two GameManager instances:
- One in LobbyScene
- One in MultiplayerTestScene

Only the **first one to Awake()** becomes `Instance`. The second one is automatically destroyed.

Result: One active GameManager, always available when needed. ✅

---

## After Quick Fix (Next Step)

Once this works, you can refactor to Option 2 or 3 for cleaner architecture. But Option 1 gets you working immediately.

**Do this now:**
1. Duplicate GameManager to LobbyScene
2. Save
3. Test clicking "Start Game"
4. Error should be gone!

Let me know when you've done this and if you hit any other issues! 🚀

