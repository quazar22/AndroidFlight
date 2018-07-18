using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class FireButton : MonoBehaviour, IPointerUpHandler, IPointerDownHandler
{
    public bool isPressed;

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    void Start () {
        isPressed = false;
	}
	
    public bool getIsPressed()
    {
        return isPressed;
    }
}
