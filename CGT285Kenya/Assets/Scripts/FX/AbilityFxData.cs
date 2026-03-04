using UnityEngine;

/**
 * <summary>
 * AbilityFxData holds all the audio/visual assets and tuning values for a
 * single ability event. Every field is exposed directly in the inspector via
 * AbilityFxLibrary, so a designer can tweak any parameter without touching code.
 *
 * VFX are spawned as a new instance of the prefab and left to clean themselves
 * up (e.g. via a Particle System Stop Action → Destroy, or a timed Destroy on
 * the root). They are never networked — each client spawns its own local copy
 * when it receives the broadcast RPC.
 * </summary>
 */
[System.Serializable]
public class AbilityFxData
{
    [Header("Audio")]
    [Tooltip("Sound played when this event fires. Leave empty for no sound.")]
    [SerializeField] public AudioClip sfxClip;

    [Tooltip("Volume scale for the clip (0–1).")]
    [Range(0f, 1f)]
    [SerializeField] public float volume = 1f;

    [Tooltip("Pitch for the clip. 1 = normal speed. Randomised between pitchMin and pitchMax each play.")]
    [SerializeField] public float pitchMin = 0.95f;

    [Tooltip("Upper bound of the pitch randomisation range.")]
    [SerializeField] public float pitchMax = 1.05f;

    [Header("Visual Effect")]
    [Tooltip("Prefab instantiated at the event's world position. Should self-destruct (e.g. ParticleSystem Stop Action = Destroy).")]
    [SerializeField] public GameObject vfxPrefab;

    [Tooltip("Uniform scale applied to the spawned VFX prefab. Useful for resizing a shared particle prefab.")]
    [SerializeField] public float vfxScale = 1f;

    [Tooltip("World-space Y offset added to the spawn position. Use this to lift effects above the floor.")]
    [SerializeField] public float vfxYOffset = 0f;
}

