using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PortalJoystick : OnScreenStick, IPointerDownHandler, IPointerUpHandler
{
    public Color unpressedColor;
    public Color pressedColor;

    private Image buttonImage;

    public bool isDragging = false;
    public readonly float sensitivityMagnitude = 4;
    private Vector3 lastValidPos = Vector3.zero;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
        buttonImage.color = unpressedColor;
    }

    private void Update()
    {
        Vector3 pos = transform.localPosition;
        if (pos != Vector3.zero) lastValidPos = transform.localPosition;
    }

    public void SetColors(Color unpressed, Color pressed)
    {
        buttonImage.color = unpressed;
        unpressedColor = unpressed;
        pressedColor = pressed;
    }

    public new void OnPointerUp(PointerEventData data)
    {
        base.OnPointerUp(data);
        buttonImage.color = unpressedColor;
        isDragging = false;
        if (transform.position.sqrMagnitude > sensitivityMagnitude && Player.instance)
            MobileControls.instance.ShootPortal(lastValidPos);
        
    }

    public new void OnPointerDown(PointerEventData data)
    {
        base.OnPointerDown(data);
        buttonImage.color = pressedColor;
        isDragging = true;
    }
}