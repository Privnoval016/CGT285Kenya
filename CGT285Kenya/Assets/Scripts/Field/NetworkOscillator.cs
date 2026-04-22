using UnityEngine;
using Fusion;

public class NetworkOscillator : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Direction to move (will be normalized)")]
    public Vector3 moveDirection = Vector3.forward;
    
    [Tooltip("Distance to move in the specified direction")]
    public float moveDistance = 5f;
    
    [Tooltip("Time in seconds to complete one way of the movement")]
    public float moveDuration = 2f;
    
    private Vector3 startPosition;
    private Vector3 targetPosition;
    
    [Networked] private float ElapsedTime { get; set; }
    [Networked] private NetworkBool MovingForward { get; set; }
    [Networked] private Vector3 NetworkedStartPosition { get; set; }

    public override void Spawned()
    {
        startPosition = transform.position;
        
        moveDirection = moveDirection.normalized;
        targetPosition = startPosition + (moveDirection * moveDistance);
        
        if (Object.HasStateAuthority)
        {
            ElapsedTime = 0f;
            MovingForward = true;
            NetworkedStartPosition = startPosition;
        }
        else
        {
            startPosition = NetworkedStartPosition;
            targetPosition = startPosition + (moveDirection * moveDistance);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            PerformMovement();
        }
    }

    public override void Render()
    {
        RenderPosition();
    }

    private void PerformMovement()
    {
        ElapsedTime += Runner.DeltaTime;
        
        if (ElapsedTime >= moveDuration)
        {
            ElapsedTime = 0f;
            
            MovingForward = !MovingForward;
        }
    }
    
    private void RenderPosition()
    {
        float normalizedTime = Mathf.Clamp01(ElapsedTime / moveDuration);
        
        float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
        
        if (MovingForward)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, easedTime);
        }
        else
        {
            transform.position = Vector3.Lerp(targetPosition, startPosition, easedTime);
        }
    }

    void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? startPosition : transform.position;
        Vector3 direction = moveDirection.normalized;
        Vector3 end = start + (direction * moveDistance);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(start, 0.2f);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(end, 0.2f);
        
        Gizmos.color = Color.yellow;
        DrawArrow(start, direction * moveDistance);
    }
    
    private void DrawArrow(Vector3 start, Vector3 direction)
    {
        Gizmos.DrawRay(start, direction);
        
        // Draw arrowhead
        Vector3 end = start + direction;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
        
        Gizmos.DrawRay(end, right * 0.3f);
        Gizmos.DrawRay(end, left * 0.3f);
    }
}