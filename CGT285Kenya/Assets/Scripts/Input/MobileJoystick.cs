using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Components")]
    [SerializeField] private RectTransform _joystickBackground;
    [SerializeField] private RectTransform _joystickHandle;
    
    [Header("Settings")]
    [SerializeField] private float _handleRange = 50f;
    [SerializeField] private bool _floatingJoystick = false;
    [SerializeField] private float _deadZone = 0.1f;
    
    private Vector2 _inputDirection = Vector2.zero;
    private bool _isActive = false;
    private Vector2 _joystickStartPosition;
    
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
        
        if (_joystickHandle != null)
        {
            _joystickHandle.anchoredPosition = Vector2.zero;
        }
        
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
        
        Vector2 direction = localPoint;
        float magnitude = direction.magnitude;
        
        if (magnitude > _handleRange)
        {
            direction = direction.normalized * _handleRange;
        }
        
        _joystickHandle.anchoredPosition = direction;
        
        _inputDirection = direction / _handleRange;
    }
}