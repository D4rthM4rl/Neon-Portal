using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchSpriteTimer : MonoBehaviour
{
    [SerializeField]
    private Sprite sprite1;
    [SerializeField]
    private Sprite sprite2;

    public float switchInterval = 1.0f; // Time in seconds to switch sprites
    private SpriteRenderer spriteRenderer;

    private float timer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component not found on this GameObject.");
            enabled = false; // Disable this script if no SpriteRenderer is found
        }
    }
    private void Start()
    {
        timer = switchInterval; // Initialize the timer
        spriteRenderer.sprite = sprite1; // Set initial sprite
    }

    private void Update()
    {
        timer -= Time.deltaTime; // Decrease the timer by the time since last frame

        if (timer <= 0f)
        {
            // Switch the sprite
            if (spriteRenderer.sprite == sprite1)
            {
                spriteRenderer.sprite = sprite2;
            }
            else
            {
                spriteRenderer.sprite = sprite1;
            }

            // Reset the timer
            timer = switchInterval;
        }
    }




}
