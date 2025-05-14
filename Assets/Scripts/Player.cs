using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Teleportable
{
    private static Player instance;
    private int groundContactCount = 0;
    public bool isGrounded = true;

    private PortalGun portalGun;

    public float initialJumpForce = 4f; // impulse on button down
    public float extraJumpForce = 7f; // continuous “hold” force
    public float jumpFalloffRate = 0.5f;
    public float maxJumpDuration = 0.3f; // how long you can hold
    public float moveSpeed = 20f; // your horizontal speed
    
    private bool isJumping; // are we in the “hold” phase?
    private float jumpTimeCounter; // how much “hold time” left
    public float groundCheckDistance = .58f;

    private int jumpBoostsGiven = 0;

    private float timeHoldingR = 0;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    protected override void Start()
    {
        base.Start();
        portalGun = GetComponent<PortalGun>();
        if (portalGun == null)
        {
            Debug.LogError("Player does not have a PortalGun component.");
        }
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

        if (Input.GetButton("Reset"))
        {
            timeHoldingR += Time.deltaTime;
            if (timeHoldingR > 1.0)
            {
                ResetPlayer();
            }
        }
        else
        {
            if (timeHoldingR > 1.0f)
            {
                ResetWorld();
                ResetPlayer();
                ResetPortals();
            }
            else if (timeHoldingR > 0)
            {
                ResetPortals();
            }
            timeHoldingR = 0;
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

        if (transform.position.y < -10f || transform.position.x < -50f || transform.position.x > 50f || transform.position.y > 50)
        {
            // Reset the player position if they fall off the screen
            ResetPlayer();
            ResetPortals();
            ResetWorld();
        }
        RotateWithGravity();
    }

    public void RotateWithGravity()
    {
        Vector2 gravDir = gravityDirection.normalized;
        float targetAngle = Mathf.Atan2(gravDir.y, gravDir.x) * Mathf.Rad2Deg + 90f;

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            720f * Time.deltaTime // Rotation speed in degrees/sec
        );
    }

    public void ResetWorld()
    {
        // Reset the world state
        // This can be used to reset any other game objects or states as needed
        // reset enemies, collectibles, etc.

        foreach (GravityAffected obj in FindObjectsOfType<GravityAffected>())
        {
            if (obj != null && obj.GetComponent<Player>() == null)
            {
                obj.transform.position = obj.respawnPosition;
                obj.rb.velocity = Vector2.zero;
                obj.gravityDirection = obj.defaultGravityDirection;
            }
        }
    }

    /// <summary>Sends player back to start</summary>
    public void ResetPlayer()
    {
        transform.position = Vector3.up;
        rb.velocity = Vector2.zero;
        gravityDirection = defaultGravityDirection;
    }

    /// <summary>Resets the portals in the scene</summary>
    public void ResetPortals()
    {
        portalGun.ResetPortals();
    }

    protected override void FixedUpdate() 
    {
        base.FixedUpdate();
        float h = Input.GetAxisRaw("Horizontal");
        Vector2 gravDir = gravityDirection.normalized;
        Vector2 moveAxis = new Vector2(-gravDir.y, gravDir.x); // perpendicular to gravity
        Vector2 hVel = moveAxis * h * Time.deltaTime;
        if (isGrounded) hVel *= 5000;
        else hVel *= 4000;
        rb.AddForce(hVel, ForceMode2D.Force);
    }

    void Jump()
    {
        if (!isJumping && isGrounded) 
        {
            isJumping = true;
            jumpTimeCounter = maxJumpDuration;
            rb.AddForce(initialJumpForce * -gravityDirection.normalized, ForceMode2D.Impulse);
            jumpBoostsGiven = 0;
        }
        else
        {
            if (jumpTimeCounter > 0f && isJumping)
            {
                // apply small extra lift each frame
                rb.AddForce(Mathf.Max(extraJumpForce - jumpFalloffRate * jumpTimeCounter, 0f) *
                    -gravityDirection * Time.fixedDeltaTime, ForceMode2D.Impulse);
                
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
            if (col.gameObject.CompareTag("Ground") && Vector2.Dot(contact.normal, gravityDirection.normalized) < -0.5f)
            {
                groundContactCount++;
                isGrounded = true;
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
            if (contacts[i].collider.CompareTag("Ground") && Vector2.Dot(contacts[i].normal, gravityDirection.normalized) < -0.5f)
            {
                groundContactCount++;
            }
        }

        isGrounded = groundContactCount > 0;

        // if (!isGrounded)
        //     Debug.Log("Left Ground");
    }

}
