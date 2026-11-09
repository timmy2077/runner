using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class JourneyHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Action<bool> onHeld;
    public void OnPointerDown(PointerEventData data)
    {
        onHeld?.Invoke(true);
    }

    public void OnPointerUp(PointerEventData data)
    {
        onHeld?.Invoke(false);
    }

    public void OnPointerExit(PointerEventData data)
    {
        onHeld?.Invoke(false);
    }

    void OnDisable()
    {
        onHeld?.Invoke(false);
    }
}
