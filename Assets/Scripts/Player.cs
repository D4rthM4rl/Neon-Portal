using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Teleportable
{
    private int groundContactCount = 0;
    public bool isGrounded = true;

    public float initialJumpForce = 4f;    // impulse on button down
    public float extraJumpForce = 7f;    // continuous “hold” force
    public float jumpFalloffRate = 0.5f;
    public float maxJumpDuration = 0.3f;  // how long you can hold
    public float moveSpeed = 20f;   // your horizontal speed
    
    private bool      isJumping;           // are we in the “hold” phase?
    private float     jumpTimeCounter;     // how much “hold time” left
    public float groundCheckDistance = .58f;

    private int jumpBoostsGiven = 0;

    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        // If escape is pressed, pause the game by stopping time
        if (Input.GetButtonDown("Pause"))
        {
            PauseMenuController.instance.ToggleMenu();
        }
        
        // isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, LayerMask.GetMask("Ground"));
        if (Input.GetButton("Jump"))
        {
            Jump();
        }
        else
        {
            isJumping = false;
            jumpTimeCounter = maxJumpDuration;
        }

        if (transform.position.y < -10f)
        {
            // Reset the player position if they fall off the screen
            transform.position = new Vector3(0, 0, 0);
            rb.velocity = Vector2.zero; // Reset velocity
        }
    }

    protected override void FixedUpdate() 
    {
        // isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, LayerMask.GetMask("Ground"));
        base.FixedUpdate();
        float h = Input.GetAxisRaw("Horizontal");
        Vector2 hVel = Vector2.right * h * Time.deltaTime;
        if (isGrounded) hVel *= 10000;
        else hVel *= 4000;
        rb.AddForce(hVel, ForceMode2D.Force);
    }

    void Jump()
    {
        if (!isJumping && isGrounded) 
        {
            isJumping = true;
            jumpTimeCounter = maxJumpDuration;
            rb.AddForce(initialJumpForce * Vector2.up, ForceMode2D.Impulse);
            jumpBoostsGiven = 0;
        }
        else
        {
            if (jumpTimeCounter > 0f && isJumping)
            {
                // apply small extra lift each frame
                rb.AddForce(Mathf.Max(extraJumpForce - jumpFalloffRate * jumpTimeCounter, 0f) *
                    Vector2.up * Time.fixedDeltaTime, ForceMode2D.Impulse);
                
                jumpTimeCounter -= Time.fixedDeltaTime;
                jumpBoostsGiven++;
                // Debug.Log("Jump Boosts Given: " + jumpBoostsGiven);
            }
            else
            {
                // Ran out of “hold” time
                isJumping = false;
                if (isGrounded)
                    jumpTimeCounter = maxJumpDuration;
            }
        }
    }

    /// <summary>
    /// Check if we hit a walkable surface (mostly flat)
    /// </summary>
    void OnCollisionEnter2D(Collision2D col)
    {
        foreach (ContactPoint2D contact in col.contacts)
        {
            if (col.gameObject.CompareTag("Ground") && contact.normal.y > 0.5f)
            {
                groundContactCount++;
                isGrounded = true;
                // Debug.Log("Landed");
                break;
            }
        }
    }

    /// <summary>
    /// Check if we lost contact with a ground surface
    /// </summary>
    void OnCollisionExit2D(Collision2D col)
    {
        // Do a conservative re-check of all remaining collisions
        // because Unity doesn't tell us which contact ended.
        Invoke(nameof(RecheckGroundedState), 0f);
    }

    private void RecheckGroundedState()
    {
        // Reset count and check all contacts again
        groundContactCount = 0;

        ContactPoint2D[] contacts = new ContactPoint2D[10];
        int count = rb.GetContacts(contacts);
        for (int i = 0; i < count; i++)
        {
            if (contacts[i].collider.CompareTag("Ground") && contacts[i].normal.y > 0.5f)
            {
                groundContactCount++;
            }
        }

        isGrounded = groundContactCount > 0;

        // if (!isGrounded)
        //     Debug.Log("Left Ground");
    }

}
