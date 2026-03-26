# NetworkCallbackHandler Prefab Setup (Clean & Simple)

## How to Set It Up

### Step 1: Create the Prefab

1. In your MultiplayerTestScene (or any scene with a NetworkCallbackHandler), select the NetworkCallbackHandler GameObject
2. Drag it into `Assets/Prefabs/` folder
3. This creates `NetworkCallbackHandler.prefab`
4. Delete the instance from the scene (you don't need it anymore)

### Step 2: Assign to LobbyManager

1. Open LobbyScene
2. Select the LobbyManager GameObject
3. In the Inspector, find **Network Settings** section
4. Drag the `NetworkCallbackHandler.prefab` into the **Network Callback Handler Prefab** field

That's it! ✅

### Step 3: Done

When LobbyManager runs:
```
LobbyManager.ConnectToMatchAsync()
  ↓
if (networkCallbackHandlerPrefab != null)
  ↓
Instantiate(networkCallbackHandlerPrefab)
  ↓
Get NetworkCallbackHandler component from the GameObject
  ↓
DontDestroyOnLoad(it)
  ↓
Add to runner callbacks
```

**Result:** Clean, prefab-based, no FindFirstObjectByType! ✅

---

## What the Code Does

```csharp
// This is now a GameObject prefab field (not a component field)
[SerializeField] private GameObject networkCallbackHandlerPrefab;

// When connecting:
if (networkCallbackHandlerPrefab != null)
{
    // Instantiate the GameObject prefab
    var handlerGo = Instantiate(networkCallbackHandlerPrefab);
    
    // Get the NetworkCallbackHandler component from it
    var callbackHandler = handlerGo.GetComponent<NetworkCallbackHandler>();
    
    // Use it
    DontDestroyOnLoad(handlerGo);
    networkRunner.AddCallbacks(callbackHandler);
}
```

---

## Inspector Setup

Your LobbyManager should show:

```
LobbyManager
├─ UI References
│  ├─ Status Text: (TextMeshProUGUI)
│  ├─ Start Button: (Button)
│  └─ Session Name Input: (InputField)
│
├─ Scene Names
│  └─ Game Scene Name: "MultiplayerTestScene"
│
└─ Network Settings
   ├─ Runner Prefab: (NetworkRunner) [optional]
   ├─ Network Callback Handler Prefab: [NetworkCallbackHandler.prefab] ← Drag here
   ├─ Max Players Per Match: 6
   └─ Game Mode: Shared
```

---

## Console Output When Running

```
[LobbyManager] NetworkRunner marked with DontDestroyOnLoad
[LobbyManager] NetworkCallbackHandler instantiated from prefab
[NetworkRunner] Calling StartGame...
```

Perfect! ✅

---

## Why This Is Clean

✅ **Inspector-based** - Drag and drop, no code changes
✅ **No FindFirstObjectByType** - Explicit prefab reference
✅ **Professional** - Standard Unity pattern
✅ **Reusable** - Prefab can be used anywhere
✅ **Persistent** - Automatically handled
✅ **Optional** - If you don't assign prefab, it creates dynamically

---

## If You Don't Assign a Prefab

It still works! LobbyManager has a fallback:

```csharp
else
{
    var go = new GameObject("[NetworkCallbackHandler]");
    callbackHandler = go.AddComponent<NetworkCallbackHandler>();
    Debug.Log("[LobbyManager] NetworkCallbackHandler created dynamically");
}
```

So you can leave it empty and it still works. But assigning the prefab is cleaner. ✅

---

**Done! Clean, simple, prefab-based setup!** 🎉

