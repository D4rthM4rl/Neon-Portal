using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Analytics;

public class Player : Teleportable
{
    // private static Player instance;
    private int groundContactCount = 0;
    public bool isGrounded = true;

    public PortalGun portalGun;

    public float initialJumpForce = 4f; // impulse on button down
    public float extraJumpForce = 7f; // continuous “hold” force
    public float jumpFalloffRate = 0.5f;
    public float maxJumpDuration = 0.3f; // how long you can hold
    public float moveSpeed = 20f; // your horizontal speed
    
    private bool isJumping; // are we in the “hold” phase?
    private float jumpTimeCounter; // how much “hold time” left
    public float groundCheckDistance = .58f;

    private int jumpBoostsGiven = 0;

    private bool jumpQueued = false;

    private float timeHoldingR = 0;

    public int numResets = 0;
    public int numDeaths = 0;

    private bool amHome = false;

    protected override void Start()
    {
        // if (instance == null)
        // {
        //     instance = this;
        //     DontDestroyOnLoad(gameObject);
        // }
        // else
        // {
        //     instance.GetComponent<PortalGun>().portals = GetComponent<PortalGun>().portals;
        //     instance.GetComponent<PortalGun>().ResetPortals();
        //     Destroy(gameObject);
        // }
        base.Start();
        Timer.instance.timer = 0;
        portalGun = GetComponent<PortalGun>();
        if (portalGun == null)
        {
            Debug.LogError("Player does not have a PortalGun component.");
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        if (amHome)
        {
            return;
        }
        base.Update();
        Timer.instance.UpdateTimer();
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
                timeHoldingR = 0;
                player_reset resetEvent = new player_reset
                {
                    level = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    x_pos = transform.position.x,
                    y_pos = transform.position.y,
                    timer = Timer.instance.timer
                };
                AnalyticsService.Instance.RecordEvent(resetEvent);

                numResets++;
                ResetWorld();
                ResetPlayer();
                ResetPortals();
            }
        }
        else
        {
            if (timeHoldingR > 0)
            {
                ResetPortals();
            }
            timeHoldingR = 0;
        }
        
        // isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, LayerMask.GetMask("Ground"));
        if (Input.GetButton("Jump"))
        {
            jumpQueued = true;
        }
        else
        {
            isJumping = false;
            jumpTimeCounter = maxJumpDuration;
        }

        if (transform.position.y < -10f || transform.position.x < -50f || transform.position.x > 60f || transform.position.y > 50)
        {
            numDeaths++;
            player_death deathEvent = new player_death
            {
                level = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                x_pos = transform.position.x,
                y_pos = transform.position.y,
                timer = Timer.instance.timer
            };
            AnalyticsService.Instance.RecordEvent(deathEvent);
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
        rb.velocity = Vector2.zero;
        transform.position = Vector3.up;
        rb.angularVelocity = 0;
        transform.rotation = Quaternion.identity;
        gravityDirection = defaultGravityDirection;
        Timer.instance.timer = 0;
    }

    /// <summary>Resets the portals in the scene</summary>
    public void ResetPortals()
    {
        portalGun.ResetPortals();
    }

    protected override void FixedUpdate() 
    {
        if (amHome)
        {
            return;
        }
        if (jumpQueued)
        {
            Jump();
            jumpQueued = false;

        }
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
                    -gravityDirection * Time.fixedDeltaTime, ForceMode2D.Force);
                
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

    public void GoHome()
    {
        portalGun.DestroyIndicators();
        // gameObject.SetActive(false);
        GetComponent<PortalGun>().enabled = false;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = false;
        }
        GetComponent<LineRenderer>().enabled = false;
        amHome = true;
    }

    public void GetReady()
    {
        GetComponent<PortalGun>().enabled = true;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = true;
        }
        GetComponent<LineRenderer>().enabled = true;
        amHome = false;
        ResetPlayer();
        ResetPortals();
        ResetWorld();
    }
}
