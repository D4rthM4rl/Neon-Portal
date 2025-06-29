using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Analytics;

public class Player : Teleportable
{
    public PortalGun portalGun;

    private GameObject cam;

    [SerializeField]
    private Gradient speedGradient;
    [SerializeField]
    private SpriteRenderer topSprite;
    [SerializeField]
    private Color topChangeColor = Color.white;
    [SerializeField]
    private SpriteRenderer rightSprite;
    [SerializeField]
    private Color rightChangeColor = Color.white;
    [SerializeField]
    private SpriteRenderer leftSprite;
    [SerializeField]
    private Color leftChangeColor = Color.white;
    [SerializeField]
    private SpriteRenderer bottomSprite;
    [SerializeField]
    private Color bottomChangeColor = Color.white;

    private Color rightCurrentColor;
    private Color leftCurrentColor;
    private Color topCurrentColor;
    private Color bottomCurrentColor;

    private int groundContactCount = 0;
    private Collider2D col;
    private ContactPoint2D[] contacts = new ContactPoint2D[10];
    public bool isGrounded = true;
    public int cantReenterIndex = -1;

    #region Movement Fields
    [SerializeField]
    private float initialJumpForce = 4f; // impulse on button down
    [SerializeField]
    private float extraJumpForce = 7f; // continuous “hold” force
    [SerializeField]
    private float jumpFalloffRate = 0.5f;
    [SerializeField]
    private float maxJumpDuration = 0.3f; // how long you can hold

    private bool isJumping; // are we in the “hold” phase?
    private float jumpTimeCounter; // how much “hold time” left
    private int jumpBoostsGiven = 0;
    private bool jumpQueued = false;

    [SerializeField]
    private float maxAccel = 20f; // your horizontal speed
    [SerializeField]
    private float minAccel = 1f;
    [SerializeField]
    private float accelRate = 5f;
    [SerializeField]
    private float accelFalloffRate = 5f;
    private float currLeftAccel = 0f;
    private float currRightAccel = 0f;
    #endregion

    private float timeHoldingR = 0;
    [SerializeField]
    private Gradient resetGradient;

    public int numResets = 0;
    public int numDeaths = 0;

    private Level level;

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
        col = GetComponent<Collider2D>();
        Time.timeScale = 0f;
        currLeftAccel = minAccel;
        currRightAccel = minAccel;
        cam = GameObject.FindGameObjectWithTag("MainCamera");
        cam.transform.position = transform.position;

        rightCurrentColor = rightSprite.color;
        leftCurrentColor = leftSprite.color;
        topCurrentColor = topSprite.color;
        bottomCurrentColor = bottomSprite.color;

        string levelName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (LevelSelect.instance != null)
        {
            level = LevelSelect.instance.GetLevelByName(levelName);
        }
        else Debug.LogWarning("LevelSelect isn't instantiated");

        if (Timer.instance != null)
        {
            Timer.instance.levelTimer = 0;
            Timer.instance.unresetLevelTimer = 0;
            Timer.instance.lastLevelPlayed = level;
        }
        else Debug.LogWarning("Timer isn't enabled");
        portalGun = GetComponent<PortalGun>();
        if (portalGun == null)
        {
            Debug.LogError("Player does not have a PortalGun component.");
        }

        RecordLevelStartEvent();
    }

    // Update is called once per frame
    protected override void Update()
    {
        CheckForInputs();
        base.Update();
        if (Timer.instance != null) Timer.instance.UpdateTimer();
        // If escape is pressed, pause the game by stopping time
        if (Input.GetButtonDown("Pause"))
        {
            PauseMenuController.instance.ToggleMenu();
            Timer.instance.ResetInactivityTimer();
        }

        if (Input.GetButton("Reset"))
        {
            if (Timer.instance != null) Timer.instance.ResetInactivityTimer();
            timeHoldingR += Time.deltaTime;
            if (timeHoldingR > .75)
            {
                timeHoldingR = 0;

                RecordResetEvent();

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

        bool rotateCameraWithGravity;
        if (Settings.instance != null) rotateCameraWithGravity = Settings.instance.rotateCameraWithGravity;
        else rotateCameraWithGravity = false;
        
        if ((rotateCameraWithGravity || gravityDirection == Vector2.down) && Input.GetButton("Up"))
        {
            jumpQueued = true;
        }
        else if (!rotateCameraWithGravity && 
            (Input.GetButton("Left") && gravityDirection == Vector2.right) ||
            (Input.GetButton("Down") && gravityDirection == Vector2.up) ||
            (Input.GetButton("Right") && gravityDirection == Vector2.left))
        {
            jumpQueued = true;
        }
        else
        {
            isJumping = false;
            jumpTimeCounter = maxJumpDuration;
        }

        if (Vector3.Distance(cameraBounds.ClosestPoint(transform.position), transform.position) > 5)
        {
            numDeaths++;

            RecordDeathEvent();

            // Reset the player position if they fall off the screen
            ResetPlayer();
            ResetPortals();
            ResetWorld();
        }
        UpdateSpriteColors();
        RotateWithGravity();
    }

    void UpdateSpriteColors()
    {
        if (timeHoldingR > 0)
        {
            Color c = resetGradient.Evaluate(timeHoldingR / .75f);
            rightSprite.color = c;
            leftSprite.color = c;
            bottomSprite.color = c;
            topSprite.color = c;
            return;
        }
        Vector2 velocity = rb.velocity; // You can tweak or dynamically compute this if needed

        float lerpSpeed = Time.deltaTime * 2f; // speed of color smoothing

        // RIGHT
        float rightSpeed = Mathf.Clamp01(velocity.x / terminalVelocity);
        Color rightTarget = speedGradient.Evaluate(rightSpeed);
        rightCurrentColor = Color.Lerp(rightCurrentColor, rightTarget, lerpSpeed);
        rightSprite.color = rightCurrentColor;

        // LEFT
        float leftSpeed = Mathf.Clamp01(-velocity.x / terminalVelocity);
        Color leftTarget = speedGradient.Evaluate(leftSpeed);
        leftCurrentColor = Color.Lerp(leftCurrentColor, leftTarget, lerpSpeed);
        leftSprite.color = leftCurrentColor;

        // TOP
        float upSpeed = Mathf.Clamp01(velocity.y / terminalVelocity);
        Color topTarget = speedGradient.Evaluate(upSpeed);
        topCurrentColor = Color.Lerp(topCurrentColor, topTarget, lerpSpeed);
        topSprite.color = topCurrentColor;

        // BOTTOM
        float downSpeed = Mathf.Clamp01(-velocity.y / terminalVelocity);
        Color bottomTarget = speedGradient.Evaluate(downSpeed);
        bottomCurrentColor = Color.Lerp(bottomCurrentColor, bottomTarget, lerpSpeed);
        bottomSprite.color = bottomCurrentColor;
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

        foreach (MovingBlock block in FindObjectsOfType<MovingBlock>())
        {
            if (block != null)
            {
                block.Reset();
            }
        }
    }

    /// <summary>Sends player back to start</summary>
    public void ResetPlayer()
    {
        Time.timeScale = 0f;
        jumpQueued = false;
        currLeftAccel = minAccel;
        currRightAccel = minAccel;
        rb.velocity = Vector2.zero;
        transform.position = Vector3.up;
        rb.angularVelocity = 0;
        transform.rotation = Quaternion.identity;
        gravityDirection = defaultGravityDirection;
        cam.transform.position = transform.position;
        if (Timer.instance != null) Timer.instance.levelTimer = 0;
    }

    /// <summary>Resets the portals in the scene</summary>
    public void ResetPortals()
    {
        portalGun.ResetPortals();
    }

    protected override void FixedUpdate() 
    {
        if (jumpQueued)
        {
            Jump();
            jumpQueued = false;
        }
        base.FixedUpdate();
        float h = 0;
        bool rotateCameraWithGravity;
        if (Settings.instance != null) rotateCameraWithGravity = Settings.instance.rotateCameraWithGravity;
        else rotateCameraWithGravity = false;
        if (rotateCameraWithGravity || gravityDirection == Vector2.down) h = Input.GetAxisRaw("Horizontal");
        else
        {
            if (gravityDirection == Vector2.left)
            {
                if (Input.GetButton("Up")) h = -1;
                else if (Input.GetButton("Down")) h = 1;
                else h = 0;
            }
            else if (gravityDirection == Vector2.up)
            {
                if (Input.GetButton("Right")) h = -1;
                else if (Input.GetButton("Left")) h = 1;
                else h = 0;
            }
            else if (gravityDirection == Vector2.right)
            {
                if (Input.GetButton("Down")) h = -1;
                else if (Input.GetButton("Up")) h = 1;
                else h = 0;
            }
        }
        Vector2 gravDir = gravityDirection.normalized;
        Vector2 moveAxis = new Vector2(-gravDir.y, gravDir.x); // perpendicular to gravity
        Vector2 hVel = moveAxis;


        if (Settings.instance == null || Settings.instance.movement == PlayerMovementType.Normal)
        {
            if (h != 0) 
            {
                if (h < 0)
                {
                    currLeftAccel = Mathf.Clamp(currLeftAccel + accelRate * Time.deltaTime, minAccel, maxAccel);
                    currRightAccel = Mathf.Clamp(currRightAccel - accelRate * accelFalloffRate * Time.deltaTime, minAccel, maxAccel);
                    hVel *= -currLeftAccel;
                }
                else if (h > 0)
                {
                    currRightAccel = Mathf.Clamp(currRightAccel + accelRate * Time.deltaTime, minAccel, maxAccel);
                    currLeftAccel = Mathf.Clamp(currLeftAccel - accelRate * accelFalloffRate * Time.deltaTime, minAccel, maxAccel);
                    hVel *= currRightAccel;
                }
                if (isGrounded) hVel *= 50;
                else hVel *= 40;
                rb.AddForce(hVel, ForceMode2D.Force);
            }
            else
            {
                currLeftAccel = Mathf.Clamp(currLeftAccel - accelRate * accelFalloffRate * Time.deltaTime, minAccel, maxAccel);
                currRightAccel = Mathf.Clamp(currRightAccel - accelRate * accelFalloffRate * Time.deltaTime, minAccel, maxAccel);
            }
        }
        else
        {
            hVel *= h * Time.deltaTime;
            if (isGrounded) hVel *= 5000;
            else hVel *= 4000;
            rb.AddForce(hVel, ForceMode2D.Force);
        }
    }

    void Jump()
    {
        if (!isJumping && isGrounded) 
        {
            isJumping = true;
            cantReenterIndex = -1;
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

    void CheckForInputs()
    {
        if (Input.GetButtonDown("Left") || Input.GetButtonDown("Up") || Input.GetButtonDown("Right") || 
            Input.GetButtonDown("Down") || Input.GetButtonDown("Fire1") ||
             (Input.GetButtonDown("Fire2") && 
             (Settings.instance == null || !Settings.instance.leftClickForBothPortals)))
        {
            if (PauseMenuController.instance == null || !PauseMenuController.instance.isPaused) Time.timeScale = 1f;
            if (Timer.instance != null) Timer.instance.ResetInactivityTimer();
        }
    }

    /// <summary>
    /// Check if we hit a walkable surface (mostly flat)
    /// </summary>
    void OnCollisionEnter2D(Collision2D col)
    {
        this.col.GetContacts(contacts);
        foreach (ContactPoint2D contact in contacts)
        {
            if (col.gameObject.CompareTag("Portal") && col.gameObject.GetComponent<PortalController>().IsConnected()
                && (Settings.instance == null || !Settings.instance.needToTouchGroundToReenterPortal || 
                col.gameObject.GetComponent<PortalController>().index != cantReenterIndex))
            {
                groundContactCount = 0;
                isGrounded = false;
                break;
            }
            else if (col.gameObject.CompareTag("Portal") && Vector2.Dot(contact.normal, gravityDirection.normalized) < -0.5f)
            {
                groundContactCount++;
                isGrounded = true;
                break;
            }
            else if (col.gameObject.CompareTag("Ground") && Vector2.Dot(contact.normal, gravityDirection.normalized) < -0.5f)
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
            else if (contacts[i].collider.CompareTag("Portal") && 
                    !contacts[i].collider.GetComponent<PortalController>().IsConnected()
                    && Vector2.Dot(contacts[i].normal, gravityDirection.normalized) < -0.5f)
            {
                groundContactCount++;
            }
        }

        isGrounded = groundContactCount > 0;

        // if (!isGrounded)
        //     Debug.Log("Left Ground");
    }


    #region Analytics Events

    /// <summary>Sends a level_start event to Unity Analytics</summary>
    public void RecordLevelStartEvent()
    {
        if (Settings.instance == null || Timer.instance == null) return;

        level_start resetEvent = new level_start
        {
            level = level.ToString(),
            level_beaten = level.beaten,
            session_time = Mathf.RoundToInt(Timer.instance.sessionTimer),
            movement_type = (int)Settings.instance.movement,
        };
        AnalyticsService.Instance.RecordEvent(resetEvent);
    }

    /// <summary>Sends a player_death event to Unity Analytics</summary>
    public void RecordDeathEvent()
    {
        if (Timer.instance == null || Settings.instance == null) return;

        player_death deathEvent = new player_death
        {
            level = level.ToString(),
            level_beaten = level.beaten,
            x_pos = transform.position.x,
            y_pos = transform.position.y,
            timer = Timer.instance.levelTimer,
            unreset_timer = Timer.instance.unresetLevelTimer,
            movement_type = (int)Settings.instance.movement,
        };
        if (PortalGun.portalsInScene.Length > 0 && PortalGun.portalsInScene[0] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[0].transform.position;
            deathEvent.portal1_x = portalPos.x;
            deathEvent.portal1_y = portalPos.y;
        }
        if (PortalGun.portalsInScene.Length > 1 && PortalGun.portalsInScene[1] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[1].transform.position;
            deathEvent.portal2_x = portalPos.x;
            deathEvent.portal2_y = portalPos.y;
        }

        AnalyticsService.Instance.RecordEvent(deathEvent);
    }

    /// <summary>Sends a player_reset event to Unity Analytics</summary>
    public void RecordResetEvent()
    {
        player_reset resetEvent = new player_reset
        {
            level = level.ToString(),
            level_beaten = level.beaten,
            x_pos = transform.position.x,
            y_pos = transform.position.y,
            timer = Timer.instance.levelTimer,
            unreset_timer = Timer.instance.unresetLevelTimer,
            movement_type = (int)Settings.instance.movement
        };
        if (PortalGun.portalsInScene.Length > 0 && PortalGun.portalsInScene[0] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[0].transform.position;
            resetEvent.portal1_x = portalPos.x;
            resetEvent.portal1_y = portalPos.y;
        }
        if (PortalGun.portalsInScene.Length > 1 && PortalGun.portalsInScene[1] != null)
        {
            Vector3 portalPos = PortalGun.portalsInScene[1].transform.position;
            resetEvent.portal2_x = portalPos.x;
            resetEvent.portal2_y = portalPos.y;
        }
        AnalyticsService.Instance.RecordEvent(resetEvent);
    }
    #endregion
}