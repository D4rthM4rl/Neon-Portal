using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UpDownButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler 
{
    public bool buttonPressed;

    public Color unpressedColor;
    public Color pressedColor;
    private Image buttonImage;

    private void Awake()
    {
        buttonPressed = false;
        buttonImage = GetComponent<Image>();
        buttonImage.color = unpressedColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        buttonPressed = true;
        buttonImage.color = pressedColor;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        buttonPressed = false;
        buttonImage.color = unpressedColor;
    }
}