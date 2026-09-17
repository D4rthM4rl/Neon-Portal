using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Player : Teleportable
{
    [HideInInspector] public PortalGun portalGun;
    public static Player instance;
    public bool hasStarted;

    [HideInInspector] private Camera cam;

    [SerializeField] private GameObject ground;

    [SerializeField] private Gradient speedGradient;
    private Light2D speedLight;
    [SerializeField] private SpriteRenderer topSprite;
    [SerializeField] private Color topChangeColor = Color.white;
    [SerializeField] private SpriteRenderer rightSprite;
    [SerializeField] private Color rightChangeColor = Color.white;
    [SerializeField] private SpriteRenderer leftSprite;
    [SerializeField] private Color leftChangeColor = Color.white;
    [SerializeField] private SpriteRenderer bottomSprite;
    [SerializeField] private Color bottomChangeColor = Color.white;

    [HideInInspector] private Color rightCurrentColor;
    [HideInInspector] private Color leftCurrentColor;
    [HideInInspector] private Color topCurrentColor;
    [HideInInspector] private Color bottomCurrentColor;

    [HideInInspector] private Collider2D col;
    /// <summary>How long to check for ground beneath me.</summary>
    [SerializeField] private float rayLength = 0.1f;
    public bool isGrounded = true;
    /// <summary>Index of the portal which I can't reenter because I just came out.</summary>
    [HideInInspector] public int cantReenterIndex = -1;

    #region Movement Fields
    /// <summary>How much force is given on initial button down.</summary>
    [Header("Movement Settings")]
    [SerializeField] private float initialJumpForce = 4f;
    /// <summary>How much force is added while continuously holding jump.</summary>
    [SerializeField] private float extraJumpForce = 7f;
    /// <summary>How much less force is added per.</summary>
    [SerializeField] private float jumpFalloffRate = 0.5f;
    /// <summary>How long can jump be held for maximum height.</summary>
    [SerializeField] private float maxJumpDuration = 0.3f;

    private bool isJumping; // are we in the “hold” phase?
    private float jumpTimeCounter; // how much “hold time” left
    private int jumpBoostsGiven = 0;
    private bool jumpQueued = false;

    [SerializeField] private float maxAccel = 20f; // your horizontal speed
    [SerializeField] private float minAccel = 1f;
    [SerializeField] private float accelRate = 5f;
    [SerializeField] private float accelFalloffRate = 5f;
    private float currLeftAccel = 0f;
    private float currRightAccel = 0f;
    #endregion

    /// <summary>How long I've been holding R to reset.</summary>
    private float timeHoldingR = 0;
    /// <summary>True while the player is respawning and should not trigger another death.</summary>
    private bool isResetting = false;
    private Coroutine resetPlayerCoroutine;
    /// <summary>The gradient that it goes through when resetting.</summary>
    [SerializeField] private Gradient resetGradient;

    public int numResets = 0;
    public int numDeaths = 0;

    /// <summary>Current Level I'm on.</summary>
    private Level level;

    protected override void Awake()
    {
        base.Awake();
        if (instance != null) Debug.LogError("Player should be null");
        instance = this;
        col = GetComponent<Collider2D>();
        Time.timeScale = 0f;
        hasStarted = false;
        // Timer.instance.levelTimer = 0;
        currLeftAccel = minAccel;
        currRightAccel = minAccel;
        cam = Camera.main;
        cam.transform.position = transform.position;

        speedLight = GetComponent<Light2D>();
        rightCurrentColor = rightSprite.color;
        leftCurrentColor = leftSprite.color;
        topCurrentColor = topSprite.color;
        bottomCurrentColor = bottomSprite.color;

        portalGun = GetComponent<PortalGun>();
        if (portalGun == null)
        {
            Debug.LogError("Player does not have a PortalGun component.");
        }
    }

    protected void Start()
    {
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

        RecordLevelStartEvent();
        if (MobileControls.instance)
            MobileControls.instance.SetColors(Settings.instance.portal1Color, Settings.instance.portal2Color);
    }

    // Update is called once per frame
    protected override void Update()
    {
        CheckForInputs();
        UpdateGroundedStatus();
        base.Update();
        // UpdateGroundedStatus();
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
                Restart();
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
        
        if ((rotateCameraWithGravity || gravityDirection == Vector2.down) && PressingUp())
        {
            jumpQueued = true;
        }
        else if (!rotateCameraWithGravity && 
            ((PressingLeft() && gravityDirection == Vector2.right) ||
            (PressingDown() && gravityDirection == Vector2.up) ||
            (PressingRight() && gravityDirection == Vector2.left)))
        {
            jumpQueued = true;
        }
        else
        {
            isJumping = false;
            jumpTimeCounter = maxJumpDuration;
        }

        if (!isResetting && Vector3.Distance(cameraBounds.ClosestPoint(transform.position), transform.position) > 5)
        {
            isResetting = true;
            numDeaths++;

            if (Timer.instance) 
            {
                level.SaveDeath(Timer.instance.levelTimer);
                RecordDeathEvent();
            }
            
            // Reset the player position if they fall off the screen
            ResetWorld();
            ResetPortals();
            BeginResetPlayer();
        }
        UpdateSpriteColors();
        RotateWithGravity();
    }

    /// <summary>
    /// Whether the player is pressing left, accounting for platform and gravity switching mobile controls.
    /// </summary>
    /// <returns>True if holding left.</returns>
    private bool PressingLeft()
    {
        if (Settings.instance && Settings.instance.platform == PlatformType.Phone)
            return MobileControls.instance.HoldingLeft();
        else
            return Input.GetButton("Left");
    }
    
    /// <summary>
    /// Whether the player is pressing up, accounting for platform and gravity switching mobile controls.
    /// </summary>
    /// <returns>True if holding up.</returns>
    private bool PressingUp()
    {
        if (Settings.instance && Settings.instance.platform == PlatformType.Phone)
            return MobileControls.instance.HoldingUp();
        else
            return Input.GetButton("Up");
    }

    /// <summary>
    /// Whether the player is pressing right, accounting for platform and gravity switching mobile controls.
    /// </summary>
    /// <returns>True if holding right.</returns>
    private bool PressingRight()
    {
        if (Settings.instance && Settings.instance.platform == PlatformType.Phone)
            return MobileControls.instance.HoldingRight();
        else
            return Input.GetButton("Right");
    }

    /// <summary>
    /// Whether the player is pressing down, accounting for platform and gravity switching mobile controls.
    /// </summary>
    /// <returns>True if holding down.</returns>
    private bool PressingDown()
    {
        if (Settings.instance && Settings.instance.platform == PlatformType.Phone)
            return MobileControls.instance.HoldingDown();
        else
            return Input.GetButton("Down");
    }

    void UpdateSpriteColors()
    {
        if (timeHoldingR > 0)
        {
            float percentReset = timeHoldingR / .75f;
            Color c = resetGradient.Evaluate(percentReset);
            rightSprite.color = c;
            leftSprite.color = c;
            bottomSprite.color = c;
            topSprite.color = c;
            speedLight.color = Color.red;
            speedLight.intensity = percentReset * 10;
            speedLight.pointLightOuterRadius = percentReset * 10f * percentReset;
            return;
        }
        Vector2 velocity = rb.linearVelocity; // You can tweak or dynamically compute this if needed

        float lerpSpeed = Time.deltaTime * 2f; // speed of color smoothing

        // RIGHT
        float rightSpeed = Mathf.Clamp01(velocity.x / (terminalVelocity * .75f));
        Color rightTarget = speedGradient.Evaluate(rightSpeed);
        rightCurrentColor = Color.Lerp(rightCurrentColor, rightTarget, lerpSpeed);
        rightSprite.color = rightCurrentColor;

        // LEFT
        float leftSpeed = Mathf.Clamp01(-velocity.x / (terminalVelocity * .75f));
        Color leftTarget = speedGradient.Evaluate(leftSpeed);
        leftCurrentColor = Color.Lerp(leftCurrentColor, leftTarget, lerpSpeed);
        leftSprite.color = leftCurrentColor;

        // TOP
        float upSpeed = Mathf.Clamp01(velocity.y / (terminalVelocity / 2));
        Color topTarget = speedGradient.Evaluate(upSpeed);
        topCurrentColor = Color.Lerp(topCurrentColor, topTarget, lerpSpeed);
        topSprite.color = topCurrentColor;

        // BOTTOM
        float downSpeed = Mathf.Clamp01(-velocity.y / terminalVelocity);
        Color bottomTarget = speedGradient.Evaluate(downSpeed);
        bottomCurrentColor = Color.Lerp(bottomCurrentColor, bottomTarget, lerpSpeed);
        bottomSprite.color = bottomCurrentColor;

        // LIGHT
        Color speedColorTarget = speedGradient.Evaluate(Mathf.Clamp01(velocity.magnitude / (terminalVelocity * .8f)));
        speedLight.color = Color.Lerp(speedLight.color, speedColorTarget, lerpSpeed);
        float percentTerminal = (velocity.magnitude / terminalVelocity);
        speedLight.intensity = percentTerminal + 1f;
        speedLight.pointLightOuterRadius = 1.2f + percentTerminal;
    }

    /// <summary>Restarts the level</summary>
    public void Restart()
    {
        timeHoldingR = 0;

        level.SaveReset(Timer.instance.levelTimer);
        RecordResetEvent();

        numResets++;
        isResetting = true;
        ResetPortals();
        ResetWorld();
        BeginResetPlayer();
    }

    private void BeginResetPlayer()
    {
        if (resetPlayerCoroutine != null)
            StopCoroutine(resetPlayerCoroutine);
        resetPlayerCoroutine = StartCoroutine(ResetPlayer());
    }

    /// <summary>Rotates the player to align with the current gravity direction.</summary>
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

    /// <summary>Resets the entire level except for the player.</summary>
    public void ResetWorld()
    {
        // Reset the world state
        // This can be used to reset any other game objects or states as needed
        // reset enemies, collectibles, etc.

        foreach (GravityAffected obj in FindObjectsByType<GravityAffected>(FindObjectsSortMode.None))
        {
            if (obj != null && obj.GetComponent<Player>() == null)
            {
                obj.transform.position = obj.respawnPosition;
                obj.rb.linearVelocity = Vector2.zero;
                obj.gravityDirection = obj.defaultGravityDirection;
            }
        }

        foreach (MovingBlock block in FindObjectsByType<MovingBlock>(FindObjectsSortMode.None))
        {
            if (block != null)
            {
                block.Reset();
            }
        }
    }

    /// <summary>Sends player back to start.</summary>
    public IEnumerator ResetPlayer()
    {
        hasStarted = false;
        jumpQueued = false;
        portalGun.hasSpawnedPortal = false;
        currLeftAccel = minAccel;
        currRightAccel = minAccel;
        rb.linearVelocity = Vector2.zero;
        transform.position = respawnPosition;
        rb.angularVelocity = 0;
        transform.rotation = Quaternion.identity;
        gravityDirection = defaultGravityDirection;
        if (MobileControls.instance) MobileControls.instance.ResetPortalTime();

        Time.timeScale = 0f;
        if (Timer.instance != null) Timer.instance.levelTimer = 0;

        CameraController cameraController = CameraController.instance;
        if (cameraController == null && cam != null)
            cameraController = cam.GetComponent<CameraController>();
        if (cameraController != null)
            cameraController.BeginReturnToPlayer(0.5f);

        yield return new WaitForSecondsRealtime(0.5f);

        if (Timer.instance != null) Timer.instance.levelTimer = 0;
        isResetting = false;
        resetPlayerCoroutine = null;
    }

    /// <summary>Resets the portals in the scene.</summary>
    public void ResetPortals()
    {
        portalGun.ResetPortals();
    }

    protected override void FixedUpdate() 
    {
        // UpdateGroundedStatus();
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

        if (rotateCameraWithGravity || gravityDirection == Vector2.down)
        {
            if (PressingLeft()) h = -1;
            else if (PressingRight()) h = 1;
            else h = 0;
        }
        else if (gravityDirection == Vector2.left)
        {
            if (PressingUp()) h = -1;
            else if (PressingDown()) h = 1;
            else h = 0;
        }
        else if (gravityDirection == Vector2.up)
        {
            if (PressingRight()) h = -1;
            else if (PressingLeft()) h = 1;
            else h = 0;
        }
        else if (gravityDirection == Vector2.right)
        {
            if (PressingDown()) h = -1;
            else if (PressingUp()) h = 1;
            else h = 0;
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
            rb.linearVelocity *= Vector2.right; // Zero out vertical velocity
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
                Vector2 jumpForce = Mathf.Max(extraJumpForce - jumpFalloffRate * jumpTimeCounter, 0f) *
                    -gravityDirection * Time.fixedDeltaTime;
                // Apply small extra lift each frame
                rb.AddForce(jumpForce, ForceMode2D.Force);
                
                jumpTimeCounter -= Time.fixedDeltaTime;
                jumpBoostsGiven++;
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
        if ((PressingLeft() || PressingUp() || PressingRight() || PressingDown()) || 
            (portalGun != null && (portalGun.hasSpawnedPortal && !hasStarted)))
        {
            if (PauseMenuController.instance == null || !PauseMenuController.instance.isPaused) 
            {
                Time.timeScale = 1f;
                hasStarted = true;
            }
            if (Timer.instance != null) Timer.instance.ResetInactivityTimer();
        }
    }

    private void UpdateGroundedStatus()
    {
        Vector2 gravDir = gravityDirection.normalized;
        Vector2 perp = new Vector2(-gravDir.y, gravDir.x);

        float width = col.bounds.extents.magnitude * 1.41f; // adjust for better coverage

        Vector2 originCenter = (Vector2)transform.position + (gravDir * width * 0.5f);
        Vector2 originLeft = originCenter - perp * width * 0.5f;
        Vector2 originRight = originCenter + perp * width * 0.5f;
        

        RaycastHit2D hitCenter = Physics2D.Raycast(originCenter, gravDir, rayLength, LayerMask.GetMask("Ground"));
        RaycastHit2D hitLeft = Physics2D.Raycast(originLeft, gravDir, rayLength, LayerMask.GetMask("Ground"));
        RaycastHit2D hitRight = Physics2D.Raycast(originRight, gravDir, rayLength, LayerMask.GetMask("Ground"));

        RaycastHit2D across = Physics2D.Raycast(originRight, -perp, width, LayerMask.GetMask("Ground"));
        RaycastHit2D wallLeft = Physics2D.Raycast(originLeft, -gravDir, width, LayerMask.GetMask("Ground"));
        RaycastHit2D wallRight = Physics2D.Raycast(originRight, -gravDir, width, LayerMask.GetMask("Ground"));

        Debug.DrawRay(originCenter, gravDir * rayLength, Color.red);
        Debug.DrawRay(originLeft, gravDir * rayLength, Color.red);
        Debug.DrawRay(originRight, gravDir * rayLength, Color.red);

        if (hitCenter.collider != null)
        {
            if (hitCenter.collider.tag == "Portal" && hitCenter.collider.GetComponent<PortalController>().IsConnected()
            && cantReenterIndex != hitCenter.collider.GetComponent<PortalController>().index)
            {
                isGrounded = false;
                ground = null;
                hitCenter.collider.GetComponent<PortalController>().OnTriggerEnter2D(col);
            }
            else
            {
                isGrounded = true;
                ground = hitCenter.collider?.gameObject;
            }
        }
        else if (hitLeft.collider != null && !across)
        {
            if (hitLeft.collider.tag == "Portal" && hitLeft.collider.GetComponent<PortalController>().IsConnected()
            && cantReenterIndex != hitLeft.collider.GetComponent<PortalController>().index)
            {
                isGrounded = false;
                ground = null;
                hitLeft.collider.GetComponent<PortalController>().OnTriggerEnter2D(col);
            }
            else
            {
                isGrounded = true;
                ground = hitLeft.collider?.gameObject;
            }
        } 
        else if (hitRight.collider != null && !across)
        {
            if (hitRight.collider.tag == "Portal" && hitRight.collider.GetComponent<PortalController>().IsConnected()
            && cantReenterIndex != hitRight.collider.GetComponent<PortalController>().index)
            {
                isGrounded = false;
                ground = null;
                hitRight.collider.GetComponent<PortalController>().OnTriggerEnter2D(col);
            }
            else
            {
                isGrounded = true;
                ground = hitRight.collider?.gameObject;
            }
        }
        else
        {
            isGrounded = false;
            ground = null;
        }
    }



    #region Analytics Events

    /// <summary>Sends a level_start event to Unity Analytics</summary>
    public void RecordLevelStartEvent()
    {
        if (Settings.instance == null || Timer.instance == null || !OnlineServices.online) return;

        level_start resetEvent = new level_start
        {
            level = level.ToString(),
            level_beaten = level.beaten,
            session_time = Mathf.RoundToInt(Timer.instance.sessionTimer),
            movement_type = (int)Settings.instance.movement,
        };
        OnlineServices.RecordEvent(resetEvent);
    }

    /// <summary>Sends a player_death event to Unity Analytics</summary>
    public void RecordDeathEvent()
    {
        if (Timer.instance == null || Settings.instance == null || !OnlineServices.online) return;

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

        OnlineServices.RecordEvent(deathEvent);
    }

    /// <summary>Sends a player_reset event to Unity Analytics</summary>
    public void RecordResetEvent()
    {
        if (OnlineServices.online == false) return;
        
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
        OnlineServices.RecordEvent(resetEvent);
    }
    #endregion
}