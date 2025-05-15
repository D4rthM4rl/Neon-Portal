using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DownIndicator : MonoBehaviour
{
    private RectTransform arrowRect;

    void Awake()
    {
        arrowRect = GetComponent<RectTransform>();
    }

    void LateUpdate()
    {
        // World down is always Vector2.down = (0, -1)
        Vector2 worldDown = Vector2.down;

        // Convert worldDown to screen-space direction
        Vector3 screenCenter = Camera.main.WorldToScreenPoint(Vector3.zero);
        Vector3 screenOffset = Camera.main.WorldToScreenPoint(worldDown) - screenCenter;

        // Get angle in screen space
        float angle = Mathf.Atan2(screenOffset.y, screenOffset.x) * Mathf.Rad2Deg;

        // Apply to UI rotation
        arrowRect.rotation = Quaternion.Euler(0, 0, angle);
    }
}
