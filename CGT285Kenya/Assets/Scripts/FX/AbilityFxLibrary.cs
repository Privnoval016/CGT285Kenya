using System.Collections.Generic;
using UnityEngine;

/**
 * <summary>
 * AbilityFxLibrary is a ScriptableObject asset that maps every AbilityFxEvent
 * to its audio/visual configuration. Create one via the Unity menu
 * (Assets → Create → Soccer / Ability FX Library) and assign it to the
 * AbilityFxPlayer scene object.
 *
 * Designer workflow:
 *   1. Create or locate the AbilityFxLibrary asset.
 *   2. Expand the "Entries" list in the inspector.
 *   3. For each event you want to support, add an entry, pick the event from
 *      the dropdown, then fill in the AudioClip, VFX prefab, and tuning values.
 *   4. Events with no entry (or a null clip/prefab) are silently skipped.
 * </summary>
 */
[CreateAssetMenu(menuName = "Soccer/Ability FX Library", fileName = "AbilityFxLibrary")]
public class AbilityFxLibrary : ScriptableObject
{
    /**
     * <summary>
     * One designer-facing row binding an event to its FX data.
     * </summary>
     */
    [System.Serializable]
    public class Entry
    {
        [Tooltip("Which ability moment this row configures.")]
        public AbilityFxEvent fxEvent;

        [Tooltip("All audio / visual settings for this event.")]
        public AbilityFxData data = new AbilityFxData();
    }

    [Header("FX Entries")]
    [Tooltip("List of all event-to-FX bindings. Each row corresponds to one ability moment.")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    // Runtime lookup — built lazily the first time GetData() is called.
    private Dictionary<AbilityFxEvent, AbilityFxData> lookup;

    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Returns the FX data for the given event, or null if no entry exists.
     * </summary>
     * <param name="fxEvent">The ability event to look up.</param>
     * <returns>AbilityFxData for the event, or null.</returns>
     */
    public AbilityFxData GetData(AbilityFxEvent fxEvent)
    {
        BuildLookupIfNeeded();
        lookup.TryGetValue(fxEvent, out AbilityFxData data);
        return data;
    }

    /**
     * <summary>
     * True when at least one entry exists for the given event.
     * </summary>
     * <param name="fxEvent">The event to check.</param>
     * <returns>True if an entry exists.</returns>
     */
    public bool HasEntry(AbilityFxEvent fxEvent)
    {
        BuildLookupIfNeeded();
        return lookup.ContainsKey(fxEvent);
    }

    // ──────────────────────────────────────────────────────────────────────────

    // Rebuild the dictionary whenever the asset is modified in the editor
    // or first accessed at runtime.
    private void BuildLookupIfNeeded()
    {
        if (lookup != null) return;
        lookup = new Dictionary<AbilityFxEvent, AbilityFxData>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry == null || entry.data == null) continue;
            // Last entry wins if there are duplicates (designer convenience).
            lookup[entry.fxEvent] = entry.data;
        }
    }

    // Invalidate cache whenever the asset changes in the editor.
    private void OnValidate() => lookup = null;
}

