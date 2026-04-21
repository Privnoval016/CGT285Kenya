using UnityEngine;
using Fusion;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Vector3 _offset = new Vector3(0, 15, -8);
    [SerializeField] private float _smoothSpeed = 5f;
    [SerializeField] private float _rotationAngle = 45f;
    
    [Header("Bounds")]
    [SerializeField] private bool _constrainToBounds = true;
    [SerializeField] private Vector2 _minBounds = new Vector2(-20, -30);
    [SerializeField] private Vector2 _maxBounds = new Vector2(20, 30);
    
    private Transform _target;
    private Camera _camera;
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        
        // Set initial rotation for top-down angle
        transform.rotation = Quaternion.Euler(_rotationAngle, 0, 0);
    }

    private void LateUpdate()
    {
        // Find local player if we don't have a target
        if (_target == null)
        {
            FindLocalPlayer();
            return;
        }
        
        // Calculate desired position
        Vector3 desiredPosition = _target.position + _offset;
        
        // Constrain to bounds if enabled
        if (_constrainToBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, _minBounds.x, _maxBounds.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, _minBounds.y, _maxBounds.y);
        }
        
        // Smoothly interpolate
        transform.position = Vector3.Lerp(transform.position, desiredPosition, _smoothSpeed * Time.deltaTime);
    }

    private void FindLocalPlayer()
    {
        // Find the player that belongs to this client
        var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            // Check if this is the local player (has input authority)
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                SetTarget(player.transform);
                break;
            }
        }
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    // Visualization in editor
    private void OnDrawGizmosSelected()
    {
        if (_constrainToBounds)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (_minBounds.x + _maxBounds.x) / 2,
                transform.position.y,
                (_minBounds.y + _maxBounds.y) / 2
            );
            Vector3 size = new Vector3(
                _maxBounds.x - _minBounds.x,
                1,
                _maxBounds.y - _minBounds.y
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}

