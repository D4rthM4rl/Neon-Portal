using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;

public class MobileControls : MonoBehaviour
{
    public static MobileControls instance;

    [Header("Rotating Controls")]
    [SerializeField] private GameObject rotatingButtonParent;
    [SerializeField] private UpDownButton leftButtonR;
    [SerializeField] private UpDownButton rightButtonR;
    [SerializeField] private UpDownButton jumpButtonR;
    [SerializeField] private Image jumpIcon;
    [Header("Non-Rotating Controls")]
    [SerializeField] private GameObject nonRotatingButtonParent;
    // [SerializeField] private UpDownButton leftButton;
    // [SerializeField] private UpDownButton rightButton;
    // [SerializeField] private UpDownButton upButton;
    // [SerializeField] private UpDownButton downButton;

    [Header("Other Controls")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button restartButton;

    [SerializeField] private PortalJoystick portalJoystick;
    [SerializeField] private Image joystickBG;

    [SerializeField] private Button swapButton;
    [SerializeField] private Image swapIcon;
    private Color portal1Color;
    private Color portal2Color;
    
    /// <summary>How much slowdown time is currently left.</summary>
    private float slowdownLeft;
    /// <summary>How many seconds of slowdown you can have.</summary>
    private float slowdownTime = 3f;
    /// <summary>Whether the slowdown is in cooldown.</summary>
    private bool slowdownRegenning = false;

    /// <summary>The way the Player's gravity direction points</summary>
    private Vector2 gravDir = Vector2.down;
    
    
    private void Awake()
    {
        instance = this;
        slowdownLeft = slowdownTime;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Settings.instance.platform != PlatformType.Phone || !Player.instance) return;

        Player p = Player.instance;
        if (Settings.instance.rotateCameraWithGravity)
        {
            leftButtonR.transform.rotation = Quaternion.Euler(0, 0, 90);
            rightButtonR.transform.rotation = Quaternion.Euler(0, 0, -90);
            jumpIcon.transform.rotation = Quaternion.Euler(0, 0, 270);
        }
        else
        {
            gravDir = p.gravityDirection.normalized;
            RotateFromOriginal(90, leftButtonR.transform);
            RotateFromOriginal(-90, rightButtonR.transform);
            RotateFromOriginal(270, jumpIcon.transform);
            if (gravDir.y > 0) // Scuffed solution to going to > < instead of switching the buttons in space
            {
                leftButtonR.transform.localScale = new Vector3(1, -1, 1);
                rightButtonR.transform.localScale = new Vector3(1, -1, 1);
            }
            else
            {
                leftButtonR.transform.localScale = new Vector3(1, 1, 1);
                rightButtonR.transform.localScale = new Vector3(1, 1, 1);
            }
        }
        
        joystickBG.fillAmount = slowdownLeft / slowdownTime;
        if (!portalJoystick.isDragging || portalJoystick.transform.localPosition.sqrMagnitude < portalJoystick.sensitivityMagnitude)
        {
            p.portalGun.SetLinesActive(false);
            if (!Player.instance.hasStarted || PauseMenuController.instance.isPaused) return;
            slowdownRegenning = slowdownLeft < slowdownTime;
            slowdownLeft = Mathf.Min(slowdownLeft + 2.5f * Time.unscaledDeltaTime, slowdownTime);
            Time.timeScale = 1f;
            return;
        }

        Vector3 aim = portalJoystick.transform.localPosition;
        if (Settings.instance.rotateCameraWithGravity)
            aim = Camera.main.transform.rotation * aim;
        
        p.portalGun.AimPortal(aim, ShootOption.None);
        if (slowdownLeft > 0 && !slowdownRegenning) 
        {
            if (!Player.instance.hasStarted) return;
            Time.timeScale = .333f;
            slowdownLeft = Mathf.Max(slowdownLeft - Time.unscaledDeltaTime, 0);
            Debug.Log(Time.timeScale);
        }
        else
        {
            if (Player.instance.hasStarted && !PauseMenuController.instance.isPaused) Time.timeScale = 1f;
            slowdownRegenning = true;
            slowdownLeft = Mathf.Min(slowdownLeft + 2.5f * Time.unscaledDeltaTime, slowdownTime);
        }
    }

    /// <summary>Tries to spawn a portal in the direction that the joystick is aiming in.</summary>
    /// <param name="aim">Direction in which to aim.</param>
    public void ShootPortal(Vector3 aim)
    {
        if (Settings.instance.rotateCameraWithGravity)
            aim = Camera.main.transform.rotation * aim;
        Player.instance.portalGun.AimPortal(aim, ShootOption.Portal1);
    }

    public void SetColors(Color portal1Color, Color portal2Color)
    {
        this.portal1Color = portal1Color;
        this.portal2Color = portal2Color;

        Color c1 = new Color(portal1Color.r, portal1Color.g, portal1Color.b, .4f);
        Color c2 = new Color(portal1Color.r, portal1Color.g, portal1Color.b, .8f);
        
        portalJoystick.SetColors(c1, c2);
        swapIcon.color = c2;
        Color c3 = new Color(portal2Color.r, portal2Color.g, portal2Color.b, .8f);
        swapButton.GetComponent<Image>().color = c3;
    }

    public void SwitchPortals()
    {
        int index = Player.instance.portalGun.IncrementPortalIndex();
        Color c1;
        Color c11;
        Color c2;
        if (index == 0)
        {
            c1 = new Color(portal1Color.r, portal1Color.g, portal1Color.b, .4f);
            c11 = new Color(portal1Color.r, portal1Color.g, portal1Color.b, .8f);
        
            portalJoystick.SetColors(c1, c11);
            swapIcon.color = c11;
            c2 = new Color(portal2Color.r, portal2Color.g, portal2Color.b, .8f);
            swapButton.GetComponent<Image>().color = c2;
        }
        else
        {
            c1 = new Color(portal2Color.r, portal2Color.g, portal2Color.b, .4f);
            c11 = new Color(portal2Color.r, portal2Color.g, portal2Color.b, .8f);
        
            portalJoystick.SetColors(c1, c11);
            swapIcon.color = c11;
            c2 = new Color(portal1Color.r, portal1Color.g, portal1Color.b, .8f);
            swapButton.GetComponent<Image>().color = c2;
        }
    }

    /// <summary>Rotates a transform based on the gravity direction.</summary>
    /// <param name="baseZ">The Z that it is naturally at.</param>
    /// <param name="transform">Transform of thing to rotate.</param>
    private void RotateFromOriginal(float baseZ, Transform transform)
    {
        float targetAngle = Mathf.Atan2(gravDir.y, gravDir.x) * Mathf.Rad2Deg + baseZ+90;

        Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            720f * Time.unscaledDeltaTime // Rotation speed in degrees/sec
        );
    }

    public void EnableCorrectControls()
    {
        rotatingButtonParent.SetActive(true);
        nonRotatingButtonParent.SetActive(false);
    }

    public bool HoldingLeft()
    {
        if (Settings.instance.rotateCameraWithGravity) 
            return leftButtonR.buttonPressed;

        if (gravDir == Vector2.down) 
            return leftButtonR.buttonPressed;
        if (gravDir == Vector2.right)
            return jumpButtonR.buttonPressed;
        if (gravDir == Vector2.up)
            return leftButtonR.buttonPressed; // Double switch button
        else return false;
    }
    
    public bool HoldingRight() 
    {
        if (Settings.instance.rotateCameraWithGravity) 
            return rightButtonR.buttonPressed;

        if (gravDir == Vector2.down)
            return rightButtonR.buttonPressed;
        if (gravDir == Vector2.left)
            return jumpButtonR.buttonPressed;
        if (gravDir == Vector2.up)
            return rightButtonR.buttonPressed; // Double switch button
        else return false;
    }

    public bool HoldingUp() 
    {
        if (Settings.instance.rotateCameraWithGravity) 
            return jumpButtonR.buttonPressed;

        if (gravDir == Vector2.down) 
            return jumpButtonR.buttonPressed;
        if (gravDir == Vector2.left)
            return leftButtonR.buttonPressed; // Double switch button
        if (gravDir == Vector2.right)
            return rightButtonR.buttonPressed; // Double switch button
        else return false;
    }

    public bool HoldingDown() 
    {

        // if (Settings.instance.rotateCameraWithGravity || !Settings.instance.rotateMobileControls) 
        //     return downButton.buttonPressed;

        if (gravDir == Vector2.down) 
            return false;
        if (gravDir == Vector2.left)
            return rightButtonR.buttonPressed; // Double switch button
        if (gravDir == Vector2.up)
            return jumpButtonR.buttonPressed;
        if (gravDir == Vector2.right)
            return leftButtonR.buttonPressed; // Double switch button
        return false; // Shouldn't reach here
    }

    public void Restart()
    {
        Player.instance.Restart();
    }

    public void ResetPortalTime() {slowdownLeft = slowdownTime;}

    public void RemovePortals()
    {
        Player.instance.ResetPortals();
    }

    public void Enable() { gameObject.GetComponent<Canvas>().enabled = true; }
    public void Disable() { gameObject.GetComponent<Canvas>().enabled = false; }
}
