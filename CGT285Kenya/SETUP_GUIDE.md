# Multiplayer Soccer Game - Unity Setup Guide

## Overview

This guide walks you through setting up a complete multiplayer mobile soccer game using Photon Fusion in Unity. The game features 2v2 or 3v3 matches with client-side prediction, mobile and keyboard controls, ball possession mechanics, and an extensible ability system.

---

## Prerequisites

- Unity 2022.3+ (6000+ recommended)
- Photon Fusion SDK (already in project)
- TextMeshPro (will import on first use)
- A Photon account (free at https://dashboard.photonengine.com)

---

## Phase 1: Photon Fusion Setup (5 minutes)

### Step 1: Get Photon App ID

1. Go to https://dashboard.photonengine.com
2. Sign in or create a free account
3. Click "Create a New App"
4. Select "Photon Fusion" as the Photon Type
5. Name it "SoccerGame" (or your choice)
6. Copy the App ID (long alphanumeric string)

### Step 2: Configure Fusion in Unity

1. In Unity, go to **Window → Fusion → Realtime Settings**
2. Paste your App ID in the "App ID Fusion" field
3. Set Region to "Auto" (or select your closest region for testing)
4. Click "Save"

### Step 3: Build Settings

1. Go to **File → Build Settings**
2. Click "Add Open Scenes" to add your current scene
3. Make sure the scene is at index 0 in the list
4. Note the build index (should be 0)
5. Close Build Settings

---

## Phase 2: Create Prefabs (20 minutes)

### Player Prefab

1. **Create Base Object**
   - In Hierarchy, create an empty GameObject
   - Name it "Player"

2. **Add Fusion Components**
   - Add Component → Fusion → Network Object
   - **Add Component → Fusion → Network Transform** ⚠️ **CRITICAL - WITHOUT THIS PLAYER WILL BE JITTERY!**
     - Configure: Interpolate Target = Transform (All)
     - Interpolation Space = World
   - Add Component → Scripts → Network Player
   - Add Component → Scripts → Ability Controller

3. **Add Unity Components**
   - Add Component → Character Controller
   - Set Character Controller properties:
     - Radius: 0.5
     - Height: 2
     - Center: (0, 1, 0)

4. **Add Visual**
   - Right-click Player → 3D Object → Capsule
   - Name it "Visual"
   - Remove the Capsule Collider component (we use CharacterController)
   - Position: (0, 1, 0)

5. **Configure Network Object**
   - In Network Object component:
     - Check "Allow State Authority"
     - Leave "Destroy When State Authority Leaves" unchecked

6. **Configure Network Player**
   - Move Speed: 5
   - Rotation Speed: 720
   - Gravity: 20
   - Ball Pickup Range: 1.5
   - Ball Steal Range: 1.2
   - Pass Speed: 10
   - Shot Speed: 20
   - Shot Charge Threshold: 0.3
   - **Player Material**: Drag a material from your project
   - **Team 0 Color**: Blue (RGB: 0, 0, 255)
   - **Team 1 Color**: Red (RGB: 255, 0, 0)
   - **Visual Mesh**: Drag the child MeshRenderer (Capsule/Visual)

7. **Save as Prefab**
   - Create folder "Assets/Prefabs" if it doesn't exist
   - Drag Player from Hierarchy to Prefabs folder
   - Delete Player from Hierarchy

### Ball Prefab

1. **Create Base Object**
   - In Hierarchy, create GameObject → 3D Object → Sphere
   - Name it "Ball"
   - Set Scale to (0.3, 0.3, 0.3)

2. **Add Fusion Components**
   - Add Component → Fusion → Network Object
   - **Add Component → Fusion → Network Transform** ⚠️ **CRITICAL - WITHOUT THIS BALL WILL BE JITTERY!**
     - Configure: Interpolate Target = Transform (All)
     - Interpolation Space = World
   - Add Component → Scripts → Network Ball Controller

3. **Add Physics**
   - Add Component → Rigidbody
   - Set Rigidbody properties:
     - Mass: 0.5
     - Drag: 0
     - Angular Drag: 0.05
     - Use Gravity: Checked
     - Is Kinematic: Unchecked

4. **Configure Network Ball Controller**
   - Ground Drag: 2
   - Air Drag: 0.5
   - Max Speed: 25
   - Pickup Cooldown: 0.5
   - Ground Layer: Default (or create "Ground" layer)

5. **Set Tag**
   - In Inspector, Tag dropdown → Add Tag
   - Create new tag "Ball"
   - Select Ball object → Set Tag to "Ball"

6. **Save as Prefab**
   - Drag Ball from Hierarchy to Prefabs folder
   - Keep Ball in scene (or delete - GameManager will spawn one)

### Register Prefabs with Fusion

**CRITICAL STEP - Don't Skip!**

1. Go to **Window → Fusion → Network Project Config**
2. In the "Prefab List" section, click the "+" button
3. Drag the **Player** prefab from Assets/Prefabs into the new slot
4. Click "+" again
5. Drag the **Ball** prefab into the second slot
6. Click "Save" at the bottom

Without this step, Fusion cannot spawn your prefabs over the network!

---

## Phase 3: Scene Setup (15 minutes)

### Game Manager

**CRITICAL: GameManager must be a networked object!**

1. Create empty GameObject in Hierarchy
2. Name it "GameManager"
3. **Add Component → Fusion → Network Object** (REQUIRED!)
   - **Allow State Authority**: Checked
   - **Destroy When State Authority Leaves**: Unchecked
4. Add Component → Scripts → Game Manager
5. Configure:
   - Player Prefab: Drag Player from Assets/Prefabs
   - Ball Prefab: Drag Ball from Assets/Prefabs
   - Match Duration: 180 (3 minutes)
   - Score To Win: 3

**Why NetworkObject is Required:**
- GameManager is a NetworkBehaviour, which requires a NetworkObject component to function
- GameManager has [Networked] properties (Team0Score, Team1Score, MatchTimer) that sync across clients
- Without NetworkObject, you'll get errors and the game won't function properly

### Network Runner Handler

1. Create empty GameObject in Hierarchy
2. Name it "NetworkRunnerHandler"
3. Add Component → Scripts → Network Runner Handler
4. Configure:
   - Runner Prefab: Leave empty (auto-creates)
   - Game Scene Name: "SampleScene" (or your scene name)
   - Game Mode: Shared
   - Max Players: 6

### Input Controller

1. Create empty GameObject in Hierarchy
2. Name it "InputController"
3. Add Component → Scripts → Input Controller
4. Configure:
   - Movement Joystick: Leave empty (for keyboard testing)
   - Aim Joystick: Leave empty (for keyboard testing)
   - Use Keyboard Input: Checked
   - Input Smoothing: 0.1

### Main Camera

1. Select the Main Camera in Hierarchy
2. Add Component → Scripts → Camera Controller
3. Configure:
   - Offset: (0, 15, -8)
   - Smooth Speed: 5
   - Rotation Angle: 45
   - Constrain To Bounds: Checked
   - Min Bounds: (-20, -30)
   - Max Bounds: (20, 30)

### Playing Field

1. Create GameObject → 3D Object → Plane
2. Name it "Field"
3. Set Transform:
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (5, 1, 8) - adjust to your preference
4. Optional: Add a material/texture for grass

### Goals (Create Two)

**Goal A (Team 0 - Bottom):**

1. Create empty GameObject in Hierarchy
2. Name it "GoalA"
3. Add Component → Box Collider
4. Configure Box Collider:
   - Is Trigger: Checked
   - Center: (0, 0, 0)
   - Size: (10, 5, 1) - adjust to match your field width
5. Set Transform Position: (0, 2.5, -40) - at one end of field
6. Add Component → Scripts → Goal Trigger
7. Set Team: 0
8. Set Tag: "Goal" (create tag if needed: Inspector → Tag → Add Tag → "Goal")

**Goal B (Team 1 - Top):**

1. Duplicate GoalA (Ctrl+D or Cmd+D)
2. Name it "GoalB"
3. Set Position: (0, 2.5, 40) - opposite end of field
4. In Goal Trigger component, set Team: 1
5. Verify Tag is still "Goal"

### Optional: Boundaries

To prevent ball/players from leaving the field:

1. Create 4 Cube GameObjects for walls
2. Position at edges: Left, Right, Top, Bottom
3. Scale them appropriately (e.g., (1, 5, 80) for side walls)
4. Make sure they have Box Colliders (not triggers)

---

## Phase 4: UI Setup (10 minutes)

### Create Canvas

1. Right-click Hierarchy → UI → Canvas
2. Select Canvas → Canvas Scaler component:
   - UI Scale Mode: Scale With Screen Size
   - Reference Resolution: 1920 x 1080
3. Add Component → Scripts → Game UI

### Score Display

1. Right-click Canvas → UI → Text - TextMeshPro
   - If prompted, click "Import TMP Essentials"
2. Name it "ScoreText"
3. Configure:
   - Anchor: Top-center
   - Position: (0, -50) from top
   - Width: 200, Height: 60
   - Font Size: 48
   - Alignment: Center
   - Text: "0 - 0" (placeholder)
4. In Canvas → Game UI component:
   - Drag ScoreText to "Score Text" field

### Timer Display

1. Right-click Canvas → UI → Text - TextMeshPro
2. Name it "TimerText"
3. Configure:
   - Anchor: Top-center
   - Position: (0, -120) from top
   - Width: 150, Height: 50
   - Font Size: 36
   - Alignment: Center
   - Text: "03:00" (placeholder)
4. In Canvas → Game UI component:
   - Drag TimerText to "Timer Text" field

### Connection Status (Optional)

1. Right-click Canvas → UI → Text - TextMeshPro
2. Name it "ConnectionStatus"
3. Configure:
   - Anchor: Top-right
   - Position: (-10, -10) from top-right
   - Width: 250, Height: 40
   - Font Size: 24
   - Alignment: Right
   - Text: "Connecting..." (placeholder)
4. In Canvas → Game UI component:
   - Drag ConnectionStatus to "Connection Status Text" field

---

## Phase 5: Testing (10 minutes)

### Single Instance Test

1. Press Play in Unity Editor
2. Check Console (Ctrl+Shift+C / Cmd+Shift+C) for:
   - "Game started successfully in Shared mode!"
   - "Connected to server!"
   - "Player X joined the session"
   - "Spawned player object for X"
   - "Ball spawned!"
   - "Match started!"

If you see errors, check:
- Photon App ID is correct in Fusion Settings
- Scene is in Build Settings at index 0
- Player and Ball prefabs are in Network Prefab List
- All GameManager references are assigned

### Control Test

**Keyboard Controls:**
- WASD: Move player
- Arrow Keys: Aim (for passing/shooting)
- Space: Pick up ball / Pass ball
- E: Tackle (not fully implemented yet)
- Shift: Dash
- Q, F, R: Abilities (if equipped)

**Test the following:**
1. Player moves smoothly
2. Camera follows player
3. Walk near ball to pick it up
4. Ball follows in front of player when held
5. Press Space while aiming to pass
6. Hold aim for 0.3+ seconds, release to shoot

### Two Instance Test (Actual Multiplayer)

1. Go to File → Build and Run
2. Let the build window open and run
3. Back in Unity Editor, press Play
4. Both instances should connect to the same "SoccerMatch" session
5. You should see 2 players in the scene (yours + remote)

**Test:**
- Both players can move independently
- Ball can be picked up by either player
- Ball can be stolen by running into opponent from front
- Passing/shooting works and syncs
- Kick ball into goal → score updates on both clients
- Timer counts down on both clients

---

## Troubleshooting

### Players Don't Spawn

**Root Cause:** In Shared mode, `OnPlayerJoined` doesn't fire for the local player - only for additional players joining.

**Solution Applied:** Player spawning now happens in both `OnSceneLoadDone` (for local player in Shared mode) and `OnPlayerJoined` (for additional players).

**Expected Console Output:**
```
Game started successfully in Shared mode!
[NetworkCallback] Scene load completed
[NetworkCallback] Attempting to spawn local player in Shared mode
[NetworkCallback] Successfully spawned player object for Player 0
Ball spawned!
Match started!
```

**Checklist:**
- [ ] GameManager in scene has NetworkObject component
- [ ] GameManager has Player Prefab assigned (drag from Assets/Prefabs)
- [ ] Player prefab has NetworkObject component
- [ ] Player prefab has Visual child (Capsule with MeshRenderer)
- [ ] Player prefab is in Window → Fusion → Network Project Config → Prefab List
- [ ] Ball prefab is in the Prefab List too

**If Still Not Working:**
1. Check Hierarchy while playing - look for "Player(Clone)"
2. Check Scene view (not just Game view) - player might be outside camera
3. Select GameManager - verify Player Prefab field is not "None"
4. Check Console for "[NetworkCallback] GameManager.Instance is null" error

### GameManager Errors

**Error:** "NetworkBehaviour needs a NetworkObject component"

**Fix:** Select GameManager → Add Component → Fusion → Network Object

### Player Jittery or Flying

**Symptoms:** 
- Player movement is jittery/stuttering
- Player floats or flies upward
- Remote players don't move smoothly

**Fixes:**
1. **Missing NetworkTransform** (MOST COMMON)
   - Select Player prefab
   - Add Component → Fusion → Network Transform
   - This is CRITICAL for smooth movement synchronization
   
2. **CharacterController Gravity Issues**
   - Already fixed in code (proper grounded checks)
   - Ensure CharacterController Center is (0, 1, 0)
   - Ensure CharacterController Height is 2

3. **Prefab Not Updated**
   - Select Player prefab in Project
   - Click "Overrides" → "Apply All"
   - Reimport if needed

### Ball Physics Janky

**Symptoms:**
- Ball teleports or stutters
- Ball doesn't sync between clients
- Ball moves erratically when held

**Fixes:**
1. **Missing NetworkTransform** (MOST COMMON)
   - Select Ball prefab
   - Add Component → Fusion → Network Transform
   - Configure: Interpolate Target = Transform Position & Rotation
   
2. **Rigidbody Settings**
   - Interpolation: Interpolate (not None)
   - Collision Detection: Continuous
   - Is Kinematic: Unchecked (script controls this)

3. **State Authority**
   - Ball is controlled by master client only
   - Already fixed in code (HasStateAuthority checks)

### Only First Player Spawns in Multiplayer

**Symptoms:**
- First client sees their player
- Second client doesn't spawn or isn't visible

**Already Fixed:** Player spawning now works for all clients in Shared mode. If still seeing issues:
1. Check Console for "[NetworkCallback] Successfully spawned player object for Player X"
2. Verify both clients connect to same session
3. Check NetworkRunnerHandler Game Mode is "Shared"
4. Try clicking in both windows to give focus

### Players Can't See Each Other / Separate Sessions

**Symptoms:**
- Both players can play but can't see each other
- Each player sees only themselves
- Ball doesn't sync between clients
- Console shows different player counts in each window

**Root Cause:** Players are connecting to DIFFERENT sessions instead of the same one.

**How Shared Mode Works:**
- When Player 1 connects: `OnPlayerJoined` fires with Player 1's PlayerRef → Player 1 spawns
- When Player 2 connects: `OnPlayerJoined` fires on BOTH clients with Player 2's PlayerRef → Player 2 spawns on both
- Both clients should see both players

**Solution - Check Session Name:**
1. **In Unity Editor:**
   - Select NetworkRunnerHandler GameObject
   - Check "Session Name" field = "SoccerMatch" (or any consistent name)
   - Both instances MUST use the SAME session name

2. **Verify Connection (Check Console):**
   - Look for: `[NetworkRunner] Session: SoccerMatch, Region: ...`
   - Both windows should show the SAME session name and region
   - Look for: `[NetworkRunner] Player Count: X`
   - First client should show "1", second client should show "2"
   - Look for: `[NetworkCallback] Player X joined`

3. **Common Causes:**
   - Different session names in each build
   - Photon App ID not configured (using demo mode)
   - Firewall blocking connection
   - Different Fusion regions selected
   - OnPlayerJoined not firing (check console logs)

4. **Testing Steps:**
   - Start first instance (Editor or Build)
   - Wait for "Game started successfully" message
   - Console should show: "[NetworkCallback] Player 0 joined"
   - Check you see YOUR player
   - Start second instance
   - Second instance console: "[NetworkCallback] Player 1 joined"
   - **First instance should ALSO log:** "[NetworkCallback] Player 1 joined"
   - Both windows should now show BOTH players

**Still Not Working?**
- Window → Fusion → Realtime Settings
- Verify App ID is set (not empty)
- Set Region to "Auto" or specific region
- Both instances must use same App ID and Region
- Check Console for "Successfully spawned player object for Player X" messages

### Ball Doesn't Sync

**Symptoms:** Ball moves on one client but not others
**Fixes:**
- Verify Ball prefab has Network Object component
- Check Ball prefab is in Network Prefab List
- Ensure Ball has Rigidbody (not kinematic by default)
- Verify Ball tag is set to "Ball"

### Can't Connect to Photon

**Symptoms:** "Failed to start game" error in console
**Fixes:**
- Verify App ID in Window → Fusion → Realtime Settings
- Check your internet connection
- Try changing region in Fusion settings
- Check Photon dashboard for service status

### Input Doesn't Work

**Symptoms:** Player doesn't move with keyboard
**Fixes:**
- Ensure InputController GameObject is in scene
- Verify "Use Keyboard Input" is checked in InputController
- Make sure player has spawned (check console logs)
- Try clicking on Game view to ensure it has focus

### Camera Doesn't Follow

**Symptoms:** Camera stays in one place
**Fixes:**
- CameraController finds player by HasInputAuthority
- Wait 1-2 seconds after spawning
- Check Camera Offset is not (0,0,0)
- Verify Camera Controller is on Main Camera object

### Build Crashes or Doesn't Connect

**Symptoms:** Build closes immediately or can't connect
**Fixes:**
- Ensure all scenes in build settings
- Check no firewall blocking connection
- Verify Photon App ID is not in development mode limit
- Test editor Play first before building

---

## Architecture Overview

### Core Systems

**Networking (Photon Fusion)**
- `NetworkRunnerHandler`: Manages Fusion NetworkRunner lifecycle
- `NetworkCallbackHandler`: Handles all Fusion network events
- `NetworkInputData`: Input structure sent over network

**Player System**
- `NetworkPlayer`: Main player controller with movement and ball interaction
- `AbilityController`: Manages player abilities (framework ready)
- `AbilityBase`: Abstract base class for abilities (strategy pattern)

**Ball System**
- `NetworkBallController`: Shared ball with possession mechanics
- `GoalTrigger`: Detects goals and triggers scoring

**Game Management**
- `GameManager`: Match state, scoring, timers (singleton)
- `CameraController`: Top-down camera following local player
- `GameUI`: HUD display (score, timer, connection status)

**Input System**
- `InputController`: Unified input manager (singleton)
- `MobileJoystick`: Virtual joystick component (for mobile builds)

### Key Fusion Concepts

**Game Mode: Shared**
- All clients run full simulation
- Client-side prediction for local player
- Server reconciles state differences
- Best for fast-paced, responsive games

**Network Authority**
- **Input Authority**: Player owns their player object
- **State Authority**: Server owns shared objects (ball, game manager)

**Networked Properties**
- Use `[Networked]` attribute
- Automatically synchronized by Fusion
- No manual serialization needed

**Fixed Update Network**
- `FixedUpdateNetwork()` runs on all clients
- Used for game logic and movement
- Tick-based (not frame-based) for consistency

---

## Gameplay Mechanics

### Ball Possession

- **Pickup**: Automatic when within 1.5m
- **Hold**: Ball follows in front of player
- **Pass**: Quick flick of aim joystick + Space
- **Shot**: Hold aim > 0.3s, then release
- **Steal**: Run into opponent from front (within 1.2m, facing them)

### Match Flow

- **Duration**: 3 minutes (configurable)
- **Win Condition**: First to 3 goals OR time limit
- **Teams**: Automatically assigned (players 0-2 = Team 0, players 3-5 = Team 1)
- **Spawning**: Players spawn on their team's side
- **Reset**: Ball returns to center after goals

### Controls

**Keyboard:**
- **WASD**: Movement
- **Arrow Keys**: Aim direction
  - **Quick tap + release**: Pass ball
  - **Hold 0.3s + release**: Shoot ball with power
- **Q**: Ability (single ability per match)

**Mobile (when UI added):**
- **Left Joystick**: Movement
- **Right Joystick**: Aim
  - **Quick flick**: Pass
  - **Hold + release**: Shoot
- **Ability Button**: Execute equipped ability

**Shooting Mechanics:**
- Hold aim in direction → Release = Shoot/Pass
- Hold time < 0.3s = Pass (speed: 10)
- Hold time ≥ 0.3s = Shot (speed: 20)
- No spacebar needed!

---

## Extending the Game

### Adding Abilities

1. Create new class inheriting from `AbilityBase`
2. Override the `Execute()` method with your ability logic
3. In Player prefab, add to Ability Controller's abilities list via Inspector
4. Abilities use cooldowns automatically

### Adding More Players

1. Change `maxPlayers` in NetworkRunnerHandler
2. Adjust spawn positions in NetworkCallbackHandler
3. Update team assignment logic if needed

### Adding Power-ups

1. Create prefab with NetworkObject
2. Add to Network Prefab List
3. Spawn via Runner.Spawn()
4. Handle pickup in OnTriggerEnter

### Adding Matchmaking

1. Replace hardcoded "SoccerMatch" session name
2. Use dynamic names or Fusion's matchmaking APIs
3. Create lobby UI
4. Call StartGame() with dynamic SessionName

---

## Performance Tips

**Network Optimization:**
- Input is sent at 60Hz (Fusion default)
- Keep NetworkInputData struct small (<32 bytes)
- Use [Networked] sparingly - only for synced data
- Consider Area of Interest for large matches

**Client-Side Optimization:**
- Enable VSync to prevent excessive FPS
- Use object pooling for VFX/projectiles
- LOD for player/ball meshes if complex
- Reduce physics calculations when possible

---

## Common Mistakes to Avoid

1. **Forgetting to add prefabs to Network Prefab List** - Most common issue!
2. **Not checking HasStateAuthority before modifying shared state**
3. **Using Update() instead of FixedUpdateNetwork() for game logic**
4. **Mixing Time.deltaTime with Runner.DeltaTime**
5. **Not assigning references in GameManager inspector**
6. **Scene not in Build Settings**

---

## Next Steps

Once you have the basic game running:

1. **Polish Gameplay**
   - Tune movement speeds
   - Adjust ball physics
   - Balance abilities
   - Add player stamina

2. **Add Visual Polish**
   - Import 3D models
   - Add particle effects
   - Create goal celebrations
   - Add UI animations

3. **Expand Features**
   - Main menu
   - Team selection
   - More abilities
   - Power-ups on field
   - Replay system

4. **Mobile Build**
   - Create mobile UI with joysticks
   - Test on actual devices
   - Optimize performance
   - Handle touch input

5. **Production Ready**
   - Error handling
   - Reconnection logic
   - Match history
   - Analytics

---

## Support & Resources

**Photon Fusion Documentation:**
- https://doc.photonengine.com/fusion

**Unity Documentation:**
- https://docs.unity3d.com

**Project Structure:**
```
Assets/Scripts/
├── Core/
│   ├── GameManager.cs
│   ├── CameraController.cs
│   └── GameUI.cs
├── Networking/
│   ├── NetworkRunnerHandler.cs
│   ├── NetworkCallbackHandler.cs
│   └── NetworkInputData.cs
├── Player/
│   └── NetworkPlayer.cs
├── Ball/
│   ├── NetworkBallController.cs
│   └── GoalTrigger.cs
├── Input/
│   ├── InputController.cs
│   └── MobileJoystick.cs
└── Abilities/
    ├── AbilityBase.cs
    └── AbilityController.cs
```

---

## Final Checklist

Before considering setup complete:

- [ ] Photon App ID configured
- [ ] Scene in Build Settings
- [ ] Player prefab created with all components
- [ ] Ball prefab created with all components
- [ ] Both prefabs in Network Prefab List ← CRITICAL!
- [ ] GameManager in scene with prefabs assigned
- [ ] NetworkRunnerHandler in scene
- [ ] InputController in scene
- [ ] CameraController on Main Camera
- [ ] Field and goals created
- [ ] UI created (at minimum: score, timer)
- [ ] Single player test passes
- [ ] Two instance test passes (actual multiplayer!)
- [ ] Ball pickup/pass/shoot works
- [ ] Goals register correctly
- [ ] No errors in console

---

**You're ready to build an amazing multiplayer soccer game!** 🎮⚽

The framework is solid, the architecture is clean, and everything is documented. Now add your unique features, polish the gameplay, and make it your own!

Good luck! 🚀

