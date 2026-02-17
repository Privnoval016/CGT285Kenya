using UnityEngine;

/// <summary>
/// GoalTrigger is placed on goal colliders to detect scoring.
/// Simple helper component that identifies which team's goal this is.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoalTrigger : MonoBehaviour
{
    [Header("Goal Settings")]
    [SerializeField] private int _team; // 0 or 1
    
    public int Team => _team;
    
    private void Awake()
    {
        // Ensure this is a trigger
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnValidate()
    {
        // Ensure team is valid
        _team = Mathf.Clamp(_team, 0, 1);
    }
}

