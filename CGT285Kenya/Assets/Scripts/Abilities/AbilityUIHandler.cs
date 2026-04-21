using UnityEngine;
using Fusion;
using System.Collections.Generic;

/**
 * <summary>
 * AbilityUIHandler manages ability selection via UI button clicks.
 * Listens to ability button events and assigns the selected ability to the local player.
 * Uses RPCs to notify the network of ability changes.
 * Also persists the selected ability across scene transitions via PlayerAbilityPreferences.
 * </summary>
 */
public class AbilityUIHandler : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UI uiComponents;

    [Header("Ability Templates")]
    [SerializeField] private DashAbility dashAbilityTemplate;
    [SerializeField] private TeleportAbility teleportAbilityTemplate;
    [SerializeField] private EnlargeAbility enlargeAbilityTemplate;
    [SerializeField] private ObstructionAbility obstructionAbilityTemplate;

    [Header("Default Ability")]
    [SerializeField] private bool randomizeDefaultAbility = true;

    private NetworkPlayer localPlayer;

    public enum AbilityType
    {
        Dash = 0,
        Teleport = 1,
        Enlarge = 2,
        Obstruction = 3
    }

    /**
     * <summary>
     * Static dictionary that persists the selected ability index per PlayerRef.
     * This survives scene transitions and is checked when spawning players in the game scene.
     * </summary>
     */
    private static readonly Dictionary<PlayerRef, int> PlayerAbilityPreferences = new Dictionary<PlayerRef, int>();

    private void OnEnable()
    {
        if (uiComponents == null)
        {
            uiComponents = GetComponent<UI>();
        }

        if (uiComponents != null)
        {
            if (uiComponents.dash != null)
                uiComponents.dash.clicked += OnDashClicked;
            if (uiComponents.teleport != null)
                uiComponents.teleport.clicked += OnTeleportClicked;
            if (uiComponents.enlarge != null)
                uiComponents.enlarge.clicked += OnEnlargeClicked;
            if (uiComponents.obstruction != null)
                uiComponents.obstruction.clicked += OnObstructionClicked;
        }
    }

    private void Start()
    {
        localPlayer = FindFirstObjectByType<NetworkPlayer>();
        if (localPlayer != null && localPlayer.Object != null && localPlayer.Object.HasInputAuthority)
        {
            AbilityType abilityToAssign;
            
            if (randomizeDefaultAbility)
            {
                int randomIndex = UnityEngine.Random.Range(0, 4);
                abilityToAssign = (AbilityType)randomIndex;
                Debug.Log($"[AbilityUIHandler] Randomly selected default ability: {abilityToAssign} (index {randomIndex})");
            }
            else
            {
                abilityToAssign = AbilityType.Dash;
            }
            
            AssignAbility(abilityToAssign);
        }
    }

    private void OnDashClicked() => OnAbilityButtonClicked(AbilityType.Dash);
    private void OnTeleportClicked() => OnAbilityButtonClicked(AbilityType.Teleport);
    private void OnEnlargeClicked() => OnAbilityButtonClicked(AbilityType.Enlarge);
    private void OnObstructionClicked() => OnAbilityButtonClicked(AbilityType.Obstruction);

    private void OnAbilityButtonClicked(AbilityType abilityType)
    {
        AssignAbility(abilityType);
    }

    private void AssignAbility(AbilityType abilityType)
    {
        if (localPlayer == null || localPlayer.Object == null || !localPlayer.Object.HasInputAuthority)
        {
            Debug.LogWarning("[AbilityUIHandler] Cannot assign ability - not input authority");
            return;
        }

        AbilityBase template = GetAbilityTemplate(abilityType);
        if (template == null)
        {
            Debug.LogWarning($"[AbilityUIHandler] No template for ability type {abilityType}");
            return;
        }

        /* Store preference FIRST before sending RPC */
        int abilityIndex = (int)abilityType;
        PlayerRef playerRef = localPlayer.Object.InputAuthority;
        StoreAbilityPreference(playerRef, abilityIndex);
        Debug.Log($"[AbilityUIHandler] Stored ability preference: Player {playerRef.PlayerId} → ability {abilityIndex}");

        /* Then attempt to update the ability in the current scene via RPC */
        var controller = localPlayer.GetComponent<AbilityController>();
        if (controller != null)
        {
            controller.RPC_AssignAbilityByType(abilityIndex);
            Debug.Log($"[AbilityUIHandler] Sent RPC to update ability to {abilityType}");
        }
        else
        {
            Debug.LogWarning("[AbilityUIHandler] No AbilityController found on local player (this is OK in lobby)");
        }

        Debug.Log($"[AbilityUIHandler] Assigned ability: {abilityType} for player {playerRef.PlayerId}");
    }

    /**
     * <summary>
     * Returns the ability template for the given type.
     * </summary>
     * <param name="type">The ability type.</param>
     * <returns>The AbilityBase template.</returns>
     */
    private AbilityBase GetAbilityTemplate(AbilityType type)
    {
        return type switch
        {
            AbilityType.Dash => dashAbilityTemplate,
            AbilityType.Teleport => teleportAbilityTemplate,
            AbilityType.Enlarge => enlargeAbilityTemplate,
            AbilityType.Obstruction => obstructionAbilityTemplate,
            _ => null
        };
    }

    private void OnDisable()
    {
        if (uiComponents != null)
        {
            if (uiComponents.dash != null)
                uiComponents.dash.clicked -= OnDashClicked;
            if (uiComponents.teleport != null)
                uiComponents.teleport.clicked -= OnTeleportClicked;
            if (uiComponents.enlarge != null)
                uiComponents.enlarge.clicked -= OnEnlargeClicked;
            if (uiComponents.obstruction != null)
                uiComponents.obstruction.clicked -= OnObstructionClicked;
        }
    }

    /**
     * <summary>
     * Stores a player's ability preference to persist across scene transitions.
     * </summary>
     * <param name="playerRef">The PlayerRef who selected the ability.</param>
     * <param name="abilityIndex">The ability index (0=Dash, 1=Teleport, 2=Enlarge, 3=Obstruction).</param>
     */
    public static void StoreAbilityPreference(PlayerRef playerRef, int abilityIndex)
    {
        PlayerAbilityPreferences[playerRef] = abilityIndex;
        Debug.Log($"[AbilityUIHandler] Stored preference for Player {playerRef.PlayerId}: ability index {abilityIndex}");
    }

    /**
     * <summary>
     * Retrieves a player's stored ability preference (if one exists).
     * Returns the index, or -1 if no preference was stored.
     * </summary>
     * <param name="playerRef">The PlayerRef to look up.</param>
     * <returns>Ability index, or -1 if not found.</returns>
     */
    public static int GetStoredAbilityPreference(PlayerRef playerRef)
    {
        if (PlayerAbilityPreferences.TryGetValue(playerRef, out int index))
        {
            Debug.Log($"[AbilityUIHandler] Retrieved preference for Player {playerRef.PlayerId}: ability index {index}");
            return index;
        }
        return -1;
    }

    /**
     * <summary>
     * Clears all stored ability preferences (useful when returning to menu).
     * </summary>
     */
    public static void ClearAllPreferences()
    {
        PlayerAbilityPreferences.Clear();
        Debug.Log("[AbilityUIHandler] Cleared all ability preferences");
    }

    /**
     * <summary>
     * Prints all stored ability preferences for debugging.
     * </summary>
     */
    public static void PrintAllPreferences()
    {
        if (PlayerAbilityPreferences.Count == 0)
        {
            Debug.Log("[AbilityUIHandler] No preferences stored");
            return;
        }

        string prefs = "Stored Preferences:\n";
        foreach (var kvp in PlayerAbilityPreferences)
        {
            prefs += $"  Player {kvp.Key.PlayerId} → Ability {kvp.Value}\n";
        }
        Debug.Log(prefs);
    }
}




