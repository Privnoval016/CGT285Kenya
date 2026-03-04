using UnityEngine;
using Fusion;

/**
 * <summary>
 * AbilityFxPlayer is the single networked hub for playing cosmetic ability
 * feedback across all clients.
 *
 * Network model:
 *   Any client (InputAuthority of the caster) calls RPC_PlayFx() which is
 *   routed to ALL peers (RpcSources.All → RpcTargets.All). Every peer then
 *   instantiates the VFX prefab and plays the audio clip locally using a
 *   temporary AudioSource. No [Networked] state is required because these
 *   effects are cosmetic-only — a late-joining client simply misses past
 *   events, which is acceptable.
 *
 * Scene setup:
 *   Place this component on a persistent scene NetworkObject (e.g. the
 *   GameManager object or a dedicated FxManager object). Assign one
 *   AbilityFxLibrary asset in the inspector.
 *
 * Designer workflow:
 *   All tuning lives in the AbilityFxLibrary ScriptableObject. The designer
 *   never needs to modify this script.
 * </summary>
 */
public class AbilityFxPlayer : NetworkBehaviour
{
    [Header("FX Library")]
    [Tooltip("ScriptableObject asset that maps every AbilityFxEvent to its audio + VFX data.")]
    [SerializeField] private AbilityFxLibrary fxLibrary;

    [Header("Audio Source Pool")]
    [Tooltip("AudioSource used as a template for one-shot effect playback. Does not need to be playing.")]
    [SerializeField] private AudioSource audioSourceTemplate;

    // ──────────────────────────────────────────────────────────────────────────
    // Singleton access
    // ──────────────────────────────────────────────────────────────────────────

    /** Scene-singleton reference. Valid after Spawned(). */
    public static AbilityFxPlayer Instance { get; private set; }

    public override void Spawned()
    {
        Instance = this;

        if (audioSourceTemplate == null)
            audioSourceTemplate = gameObject.AddComponent<AudioSource>();

        audioSourceTemplate.playOnAwake = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Public API — called by abilities
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Broadcasts a request to all clients to play the FX for the given event
     * at the specified world position.
     *
     * Safe to call from FixedUpdateNetwork() on the InputAuthority peer.
     * </summary>
     * <param name="fxEvent">The ability event identifier.</param>
     * <param name="worldPosition">World-space position at which to spawn VFX / play audio.</param>
     */
    public void PlayFx(AbilityFxEvent fxEvent, Vector3 worldPosition)
    {
        if (!Object.IsValid) return;
        RPC_PlayFx((int)fxEvent, worldPosition);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // RPC
    // ──────────────────────────────────────────────────────────────────────────

    /**
     * <summary>
     * Received on every connected client. Looks up the FX data and plays
     * the sound and/or spawns the VFX prefab at the given position.
     * Uses int instead of the enum directly for Fusion RPC compatibility.
     * </summary>
     * <param name="fxEventId">Cast of AbilityFxEvent.</param>
     * <param name="position">World-space spawn / play position.</param>
     */
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_PlayFx(int fxEventId, Vector3 position)
    {
        if (fxLibrary == null)
        {
            Debug.LogWarning("[AbilityFxPlayer] No AbilityFxLibrary assigned.");
            return;
        }

        var fxEvent = (AbilityFxEvent)fxEventId;
        AbilityFxData data = fxLibrary.GetData(fxEvent);

        if (data == null) return;

        PlayAudio(data, position);
        SpawnVfx(data, position);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Local playback helpers
    // ──────────────────────────────────────────────────────────────────────────

    private void PlayAudio(AbilityFxData data, Vector3 position)
    {
        if (data.sfxClip == null) return;

        // Spawn a temporary AudioSource at the position so audio has correct 3D spatialization.
        var go = new GameObject("FX_Audio_OneShot");
        go.transform.position = position;

        var src = go.AddComponent<AudioSource>();
        src.clip = data.sfxClip;
        src.volume = data.volume;
        src.pitch = Random.Range(data.pitchMin, data.pitchMax);
        src.spatialBlend = 1f; // full 3D
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 50f;
        src.playOnAwake = false;
        src.Play();

        // Destroy the temporary object once the clip finishes.
        Destroy(go, data.sfxClip.length + 0.1f);
    }

    private void SpawnVfx(AbilityFxData data, Vector3 position)
    {
        if (data.vfxPrefab == null) return;

        Vector3 spawnPos = position + Vector3.up * data.vfxYOffset;
        GameObject vfxInstance = Instantiate(data.vfxPrefab, spawnPos, Quaternion.identity);

        if (data.vfxScale != 1f && data.vfxScale > 0f)
            vfxInstance.transform.localScale = Vector3.one * data.vfxScale;

        // Safety fallback: auto-destroy after 10 s in case the prefab lacks its own cleanup.
        Destroy(vfxInstance, 10f);
    }
}

