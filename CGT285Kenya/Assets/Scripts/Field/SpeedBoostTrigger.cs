using UnityEngine;

/**
 * <summary>
 * SpeedBoostTrigger is a local trigger that increases player movement speed.
 * This is a purely local effect - no networking involved.
 * Attach this to a trigger collider to create speed boost zones.
 * </summary>
 */
[RequireComponent(typeof(Collider))]
public class SpeedBoostTrigger : MonoBehaviour
{
    [Header("Speed Boost Settings")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private bool visualizeZone = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var networkPlayer = other.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                networkPlayer.SetLocalSpeedMultiplier(speedMultiplier);
                Debug.Log($"[SpeedBoostTrigger] Player {networkPlayer.Object.InputAuthority.PlayerId} entered speed boost zone (x{speedMultiplier})");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var networkPlayer = other.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                networkPlayer.SetLocalSpeedMultiplier(1f);
                Debug.Log($"[SpeedBoostTrigger] Player {networkPlayer.Object.InputAuthority.PlayerId} exited speed boost zone");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!visualizeZone) return;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawCube(transform.position, col.bounds.size);
        }
    }
}

