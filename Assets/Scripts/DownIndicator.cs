using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownIndicator : MonoBehaviour
{
    private RectTransform rect;
    private Transform regTransform;

    public float angleOffset = 0f; // Angle offset in degrees

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        if (rect == null)
        {
            regTransform = GetComponent<Transform>();
            if (regTransform == null)
            {
                Debug.LogError("No RectTransform or Transform component found on this GameObject.");
                return;
            }
        }
    }

    void LateUpdate()
    {
        // World down is always Vector2.down = (0, -1)
        Vector2 worldDown = Vector2.down;

        // Convert worldDown to screen-space direction
        Vector3 screenCenter = Camera.main.WorldToScreenPoint(Vector3.zero);
        Vector3 screenOffset = Camera.main.WorldToScreenPoint(worldDown) - screenCenter;

        // Get angle in screen space
        float angle = Mathf.Atan2(screenOffset.y, screenOffset.x) * Mathf.Rad2Deg + angleOffset;

        // Apply to UI rotation
        if (rect) rect.rotation = Quaternion.Euler(0, 0, angle);
        else regTransform.rotation = Quaternion.Euler(0, 0, -angle);
    }
}
