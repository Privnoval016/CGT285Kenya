# Game Rules and Matchmaking Setup Guide

This guide explains how to set up the game rules, matchmaking lobby, and field reset mechanics for your multiplayer soccer game.

## Overview

The system consists of three main components:

1. **Match Rules Configuration** - Configure match duration, early-end conditions, and scoring
2. **Spawn Point Configuration** - Define player and ball spawn positions on the field
3. **Lobby System** - Scene-based matchmaking before entering a match
4. **Field Reset Mechanics** - Automatic player and ball repositioning after goals

## Setup Steps

### Step 1: Create Configuration Assets

#### 1.1 Create MatchRulesConfig

1. Right-click in your Assets folder in Unity
2. Create → Game → Match Rules Config
3. Name it `MatchRulesConfig`
4. Configure the following settings:
   - **Match Duration Seconds**: Total match time (e.g., 300 for 5 minutes)
   - **Early End Check Time Seconds**: When to start checking for early-end condition (e.g., 120 for 2 minutes)
   - **Point Lead To End Early**: Point difference needed to end match early (e.g., 3 points)
   - **Score To Win**: Fallback score limit (e.g., 10 goals)
   - **Post Score Reset Delay**: Delay before field resets after a goal (e.g., 0.5 seconds)

**Example**: With these settings: Match ends at 5 minutes OR at 2 minutes if one team has a 3-point lead, whichever comes first.

#### 1.2 Create SpawnPointConfig

1. Right-click in your Assets folder in Unity
2. Create → Game → Spawn Point Config
3. Name it `SpawnPointConfig`
4. Expand the **Team Spawns** arrays for both Team 0 and Team 1
5. For each team, set 3 spawn positions (for 3v3 matches):
   - **Position 0**: First player spawn (e.g., (-5, 0.5, -3) for Team 0)
   - **Position 1**: Second player spawn (e.g., (-5, 0.5, 0) for Team 0)
   - **Position 2**: Third player spawn (e.g., (-5, 0.5, 3) for Team 0)
6. Set the **Ball Spawn Position** to the center field (e.g., (0, 0.5, 0))

**Tip**: Use symmetric positions for each team. If Team 0 uses x = -5, Team 1 should use x = 5.

### Step 2: Set Up the Game Scene

#### 2.1 Assign Configs to GameManager

1. Open your game scene (e.g., `MultiplayerTestScene`)
2. Find the **GameManager** GameObject in the scene
3. In the Inspector, assign:
   - **Match Rules Config**: Drag the `MatchRulesConfig` asset
   - **Spawn Point Config**: Drag the `SpawnPointConfig` asset

#### 2.2 Assign Configs to NetworkCallbackHandler

1. Find the **NetworkCallbackHandler** GameObject
2. In the Inspector, assign:
   - **Spawn Point Config**: Drag the same `SpawnPointConfig` asset
   - **Ability Config**: Your existing ability assignment config

### Step 3: Create the Lobby Scene

#### 3.1 Create a New Scene

1. Create a new scene named `LobbyScene`
2. Save it in `Assets/Scenes/`

#### 3.2 Set Up UI

1. Create a Canvas
2. Add the following UI elements:
   - **Panel** for background
   - **Text** element for status messages (position at top center)
   - **InputField** for session name (optional, for custom room names)
   - **Button** labeled "Start Game"

#### 3.3 Add LobbyManager Script

1. Create an empty GameObject named `LobbyManager`
2. Attach the `LobbyManager` script component
3. In the Inspector, configure:
   - **Status Text**: Drag the Text element
   - **Start Button**: Drag the Button element
   - **Session Name Input**: Drag the InputField (optional)
   - **Game Scene Name**: Set to your game scene name (e.g., "MultiplayerTestScene")
   - **Runner Prefab**: Drag your NetworkRunner prefab (if you have one)
   - **Network Callback Handler**: Drag the NetworkCallbackHandler GameObject from your game scene
   - **Max Players Per Match**: Set to 6 (for 3v3)

#### 3.4 Create NetworkCallbackHandler Instance (if needed)

If you're using a scene-persistent NetworkCallbackHandler, create it as a prefab and assign it to the LobbyManager. Otherwise, create one in the scene and configure the same way as your game scene.

### Step 4: Configure Match Settings in GameManager

The GameManager now automatically:
- **Tracks score** via Team0Score and Team1Score properties
- **Manages match timer** - starts at match duration and counts down
- **Checks early-end conditions** - every tick in FixedUpdateNetwork
- **Resets the field** - calls RPC_ResetField() when a goal is scored
- **Ends the match** - when time expires or winning condition is met

No additional configuration needed; the system reads from your MatchRulesConfig automatically.

### Step 5: Add Scenes to Build Settings

1. Go to File → Build Settings
2. Click "Add Open Scenes" to add both scenes, or manually drag them
3. Ensure **LobbyScene** is at a lower index than your **Game Scene** (players will start in the lobby)

Example order:
- Index 0: LobbyScene
- Index 1: MultiplayerTestScene

## Game Flow

```
1. Player starts application → Loads LobbyScene
2. Player clicks "Start Game" → LobbyManager creates NetworkRunner
3. NetworkRunner attempts to join/create a match session
4. When another player joins → Both load the Game Scene
5. Match starts → GameManager initializes score and timer
6. Player scores goal → GameManager.OnGoalScored() called
7. Field resets → All players move to spawn points, ball to center
8. Match continues until:
   - Early-end condition met (if configured), OR
   - Timer expires, OR
   - A team reaches score-to-win limit
9. Match ends → Players can return to lobby or app closes
```

## Field Reset Behavior

When a goal is scored:
1. **Obstruction blocks** are despawned (cleared from field)
2. **RPC_ResetField()** is called on all clients
3. **All players** are moved to their spawn positions
4. **Ball** is moved to the center field position
5. **Player state** is reset (velocity, movement direction)
6. Match continues with 0.5 second delay (configurable)

## Team Assignment

Teams are automatically assigned based on PlayerId:
- **PlayerId 1-3** → Team 0 (Blue)
- **PlayerId 4-6** → Team 1 (Red)

This is deterministic and identical on all clients.

## Testing

### Single Player Test
1. Start the game in one editor instance
2. Click "Start Game" in the lobby
3. You should see "Connected! Waiting for players..." until another player joins

### Two Player Test
1. Build the game for Windows/Mac
2. Run two instances side-by-side
3. Both click "Start Game"
4. Both should load into the same match
5. You can move both players and see them interact

### Testing Field Reset
1. In the game scene, position Player 1 near the goal
2. Pass or shoot the ball into the goal
3. Observe all players move to spawn positions and the ball resets to center

## Troubleshooting

### Players spawn but don't load together
- Check that LobbyManager's **Game Scene Name** matches your actual scene name exactly
- Ensure both clients are connecting to the same session (same session name)
- Check the console for any "Failed to load scene" errors

### Field doesn't reset after goal
- Verify **SpawnPointConfig** is assigned to both **GameManager** and **NetworkCallbackHandler**
- Check that spawn positions are valid (not overlapping, not underground)
- Ensure **GoalTrigger** is calling **GameManager.Instance.OnGoalScored()**

### Match never ends
- Check **MatchRulesConfig** values (duration must be > 0)
- Verify that **Early End Check Time** is less than **Match Duration** if using early-end condition
- Test with debug logs in GameManager.FixedUpdateNetwork()

### Players stuck in lobby
- Check that **NetworkRunner Prefab** is correctly assigned
- Ensure **Network Callback Handler** is properly configured in LobbyManager
- Check console for Fusion errors ("Failed to start game: ...")

## Advanced Configuration

### Customizing Spawn Points

You can create multiple **SpawnPointConfig** assets for different match types (2v2, 3v3, 4v4):
1. Create a new SpawnPointConfig for each match type
2. Set up different array sizes for Team Spawns
3. Dynamically load the correct config based on player count

### Dynamic Early-End Conditions

To create more complex early-end logic, modify the `ShouldEndEarly()` method in **MatchRulesConfig**:
```csharp
public bool ShouldEndEarly(int team0Score, int team1Score, float elapsedTimeSeconds)
{
    // Custom logic here
    // Example: End at 1 minute if one team has 5+ goals
    if (elapsedTimeSeconds >= 60f && (team0Score >= 5 || team1Score >= 5))
        return true;
    
    return false;
}
```

### Post-Score Celebrations

To add visual/audio feedback after goals:
1. Hook into **GameManager.OnGoalScored()** from another system
2. Play goal animations, sound effects, etc.
3. The 0.5-second delay gives time for celebration before reset

## Summary

You now have a complete match lifecycle with:
- ✅ Configurable match duration and early-end conditions
- ✅ Dynamic field reset after every goal
- ✅ Lobby-based matchmaking
- ✅ Automatic team assignment
- ✅ Networked state management via Photon Fusion

The system is designed to be data-driven and inspector-friendly, allowing you to tweak match rules without touching code.

