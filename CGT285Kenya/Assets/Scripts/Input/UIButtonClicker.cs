using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonClicker : MonoBehaviour, IPointerClickHandler
{
    public Action OnClick = delegate { };
    
    [SerializeField] private Slider cooldownSlider;
    [SerializeField] private Image cooldownImage;
    
    public Color cooldownColor;
    public Color activeColor;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.pointerClick == gameObject)
        {
            OnClick.Invoke();
        }
    }

    public void SetSliderPercentage(float percentage)
    {
        cooldownSlider.value = percentage;
    }
    
    public void CooldownFinished()
    {
        cooldownImage.color = activeColor;
    }

    public void CooldownStarted()
    {
        cooldownImage.color = cooldownColor;
    }
}