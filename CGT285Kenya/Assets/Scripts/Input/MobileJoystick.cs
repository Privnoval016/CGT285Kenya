using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// MobileJoystick provides a virtual joystick for mobile touch input.
/// It can be used in both fixed and floating modes.
/// 
/// Architecture: Standalone component that can be easily placed in UI
/// </summary>
public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Components")]
    [SerializeField] private RectTransform _joystickBackground;
    [SerializeField] private RectTransform _joystickHandle;
    
    [Header("Settings")]
    [SerializeField] private float _handleRange = 50f;
    [SerializeField] private bool _floatingJoystick = false;
    [SerializeField] private float _deadZone = 0.1f;
    
    // State
    private Vector2 _inputDirection = Vector2.zero;
    private bool _isActive = false;
    private Vector2 _joystickStartPosition;
    
    // Properties
    public Vector2 Direction => _inputDirection.magnitude > _deadZone ? _inputDirection : Vector2.zero;
    public bool IsActive => _isActive;
    
    private void Start()
    {
        if (_joystickBackground != null)
        {
            _joystickStartPosition = _joystickBackground.anchoredPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isActive = true;
        
        // For floating joystick, move to touch position
        if (_floatingJoystick && _joystickBackground != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystickBackground.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );
            _joystickBackground.anchoredPosition = localPoint;
        }
        
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isActive = false;
        _inputDirection = Vector2.zero;
        
        // Reset handle to center
        if (_joystickHandle != null)
        {
            _joystickHandle.anchoredPosition = Vector2.zero;
        }
        
        // Reset floating joystick position
        if (_floatingJoystick && _joystickBackground != null)
        {
            _joystickBackground.anchoredPosition = _joystickStartPosition;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_joystickBackground == null || _joystickHandle == null)
            return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );
        
        // Calculate direction and clamp to handle range
        Vector2 direction = localPoint;
        float magnitude = direction.magnitude;
        
        // Normalize and clamp
        if (magnitude > _handleRange)
        {
            direction = direction.normalized * _handleRange;
        }
        
        // Update handle position
        _joystickHandle.anchoredPosition = direction;
        
        // Calculate input direction (normalized)
        _inputDirection = direction / _handleRange;
    }
}

