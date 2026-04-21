using System.Collections.Generic;
using UnityEngine;

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

    private Dictionary<AbilityFxEvent, AbilityFxData> lookup;

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
    
    private void BuildLookupIfNeeded()
    {
        if (lookup != null) return;
        lookup = new Dictionary<AbilityFxEvent, AbilityFxData>(entries.Count);
        foreach (var entry in entries)
        {
            if (entry == null || entry.data == null) continue;
            lookup[entry.fxEvent] = entry.data;
        }
    }

    private void OnValidate() => lookup = null;
}

