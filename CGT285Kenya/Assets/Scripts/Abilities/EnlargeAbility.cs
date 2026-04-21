using UnityEngine;
using Fusion;

[System.Serializable]
public class EnlargeAbility : AbilityBase
{
    [Header("Enlarge Settings")]
    [Tooltip("How many seconds the enlarged state lasts.")]
    [SerializeField] private float enlargeDuration = 5f;

    [Tooltip("Uniform scale multiplier applied to the player's transform while enlarged.")]
    [SerializeField] private float enlargeScaleMultiplier = 1.8f;

    [Tooltip("Multiplier applied to the ball intercept/steal radius while enlarged.")]
    [SerializeField] private float enlargeInterceptMultiplier = 1.6f;

    [Tooltip("Multiplier applied to shot charge speed while enlarged (shot fires sooner on hold).")]
    [SerializeField] private float enlargeShotChargeMultiplier = 1.75f;
    
    /** Uniform scale multiplier while enlarged, 1 otherwise. */
    public float ScaleMultiplier       => enlargeScaleMultiplier;

    /** Intercept radius multiplier while enlarged, 1 otherwise. */
    public float InterceptMultiplier   => enlargeInterceptMultiplier;

    /** Shot charge speed multiplier while enlarged, 1 otherwise. */
    public float ShotChargeMultiplier  => enlargeShotChargeMultiplier;
    
    /**
     * <summary>
     * Activates the enlarged state by writing the timer on the NetworkPlayer
     * via RPC so all peers replicate it.
     * </summary>
     * <param name="context">Runtime context.</param>
     */
    protected override void Execute(AbilityContext context)
    {
        if (context.Player == null) return;

        // Ask state authority to set the EnlargeTimer on the player.
        context.Player.RPC_SetEnlarged(enlargeDuration);

        AbilityFxPlayer.Instance?.PlayFx(AbilityFxEvent.EnlargeActivate, context.Player.transform.position);

        StartCooldown();
        Debug.Log($"[EnlargeAbility] Enlarged for {enlargeDuration}s");
    }

    public override void OnValidate()
    {
        base.OnValidate();
        enlargeDuration               = Mathf.Max(0.5f,   enlargeDuration);
        enlargeScaleMultiplier        = Mathf.Max(1f,      enlargeScaleMultiplier);
        enlargeInterceptMultiplier    = Mathf.Max(1f,      enlargeInterceptMultiplier);
        enlargeShotChargeMultiplier   = Mathf.Max(1f,      enlargeShotChargeMultiplier);
    }
}

