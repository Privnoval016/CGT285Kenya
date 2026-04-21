using UnityEngine;

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

