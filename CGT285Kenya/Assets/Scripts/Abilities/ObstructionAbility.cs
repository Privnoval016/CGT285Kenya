using UnityEngine;
using Fusion;

[System.Serializable]
public class ObstructionAbility : AbilityBase
{
    [Header("Obstruction Settings")]
    [Tooltip("Maximum radius (metres) from the player in which a block can be placed.")]
    [SerializeField] private float placementRange = 6f;

    [Tooltip("Prefab for the NetworkObject block. Must have ObstructionBlock + NetworkObject.")]
    [SerializeField] private GameObject blockPrefab;

    [Tooltip("Prefab for the local-only range indicator ring shown during targeting.")]
    [SerializeField] private GameObject rangeIndicatorPrefab;

    [Tooltip("Cooldown penalty (seconds) applied when the player cancels targeting.")]
    [SerializeField] private float cancelCooldown = 3f;

    #region Public Accessors

    /** The block prefab; read by AbilityController.RPC_SpawnObstructionBlock(). */
    public ObstructionBlock GetBlockPrefab() => blockPrefab != null ? blockPrefab.GetComponent<ObstructionBlock>() : null;

    /** True while the player is in targeting mode. */
    public bool IsTargeting => isTargeting;

    /** Placement range in metres, for UI display. */
    public float PlacementRange => placementRange;

    #endregion

    #region Runtime State

    private bool isTargeting;
    private GameObject rangeIndicatorInstance;

    #endregion

    #region AbilityBase Overrides

    /**
     * <summary>
     * Each tick: keeps the range indicator centred on the player.
     * </summary>
     */
    public override void TickAbility(AbilityContext context)
    {
        if (!isTargeting || context.Player == null) return;

        if (rangeIndicatorInstance != null)
        {
            Vector3 pos   = context.Player.transform.position;
            pos.y         = 0.02f;
            rangeIndicatorInstance.transform.position = pos;
        }
    }
    
    protected override void Execute(AbilityContext context)
    {
        if (!isTargeting)
        {
            // Enter targeting — no cooldown consumed here.
            EnterTargeting(context);
        }
        else
        {
            // Second button press while targeting = cancel.
            CancelTargeting();
            Controller.SetCooldown(cancelCooldown);
            Debug.Log("[ObstructionAbility] Targeting cancelled, short cooldown applied.");
        }
    }
    
    public override bool TryExecuteAtPosition(AbilityContext context, Vector3 worldPosition)
    {
        bool hasTapPos = worldPosition != Vector3.zero;

        if (hasTapPos && isTargeting)
        {
            // Case B: placement confirmation tap while in targeting mode.
            float dist = Vector3.Distance(context.Player.transform.position, worldPosition);
            if (dist > placementRange)
            {
                Debug.Log($"[ObstructionAbility] Tap out of range ({dist:F1}m > {placementRange:F1}m). Move closer.");
                return false;
            }

            PlaceBlock(context, worldPosition);
            return true;
        }

        if (hasTapPos && !isTargeting)
        {
            // Tap arrived but we are not in targeting mode yet — ignore it.
            // (Edge case: tap and Q arrive same tick but Q enters targeting.)
            return false;
        }

        if (!isTargeting)
        {
            EnterTargeting(context);
            return true;
        }
        else
        {
            // Cancel targeting with penalty cooldown.
            CancelTargeting();
            Controller.SetCooldown(cancelCooldown);
            Debug.Log("[ObstructionAbility] Targeting cancelled.");
            return true;
        }
    }

    #endregion

    #region Private Helpers

    private void EnterTargeting(AbilityContext context)
    {
        isTargeting = true;
        SpawnRangeIndicator(context);
        Debug.Log("[ObstructionAbility] Entered targeting mode.");
    }

    private void CancelTargeting()
    {
        isTargeting = false;
        DestroyRangeIndicator();
    }

    private void PlaceBlock(AbilityContext context, Vector3 position)
    {
        CancelTargeting();

        if (blockPrefab == null)
        {
            Debug.LogWarning("[ObstructionAbility] blockPrefab is not assigned!");
            return;
        }

        Vector3 flatPos = new Vector3(position.x, 0.5f, position.z);
        context.Player.GetComponent<AbilityController>().RPC_SpawnObstructionBlock(flatPos);

        AbilityFxPlayer.Instance?.PlayFx(AbilityFxEvent.ObstructionPlace, flatPos);

        StartCooldown();
        Debug.Log($"[ObstructionAbility] Block placed at {flatPos}");
    }

    private void SpawnRangeIndicator(AbilityContext context)
    {
        if (rangeIndicatorPrefab == null) return;
        if (rangeIndicatorInstance != null) return;

        Vector3 pos = context.Player.transform.position;
        pos.y = 0.02f;
        rangeIndicatorInstance = UnityEngine.Object.Instantiate(rangeIndicatorPrefab, pos, Quaternion.identity);

        Vector3 s = rangeIndicatorInstance.transform.localScale;
        rangeIndicatorInstance.transform.localScale = new Vector3(placementRange * 2f, s.y, placementRange * 2f);
    }

    private void DestroyRangeIndicator()
    {
        if (rangeIndicatorInstance != null)
        {
            UnityEngine.Object.Destroy(rangeIndicatorInstance);
            rangeIndicatorInstance = null;
        }
    }

    public override void OnValidate()
    {
        base.OnValidate();
        placementRange = Mathf.Max(1f, placementRange);
        cancelCooldown = Mathf.Max(0f, cancelCooldown);
    }

    #endregion
}
