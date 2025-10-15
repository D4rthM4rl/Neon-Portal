using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.OnScreen;

public class MobileControls : MonoBehaviour
{
    public static MobileControls instance;

    [SerializeField] private UpDownButton leftButton;
    [SerializeField] private UpDownButton rightButton;
    [SerializeField] private UpDownButton jumpButton;
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
            leftButton.transform.rotation = Quaternion.Euler(0, 0, 90);
            rightButton.transform.rotation = Quaternion.Euler(0, 0, -90);
            jumpButton.transform.rotation = Quaternion.identity;
        }
        else
        {
            gravDir = p.gravityDirection.normalized;
            RotateFromOriginal(90, leftButton.transform);
            RotateFromOriginal(-90, rightButton.transform);
            RotateFromOriginal(0, jumpButton.transform);
            if (gravDir.y > 0) // Scuffed solution to going to > < instead of switching the buttons in space
            {
                leftButton.transform.localScale = new Vector3(1, -1, 1);
                rightButton.transform.localScale = new Vector3(1, -1, 1);
            }
            else
            {
                leftButton.transform.localScale = new Vector3(1, 1, 1);
                rightButton.transform.localScale = new Vector3(1, 1, 1);
            }
        }
        
        joystickBG.fillAmount = slowdownLeft / slowdownTime;
        if (!portalJoystick.isDragging || portalJoystick.transform.localPosition.sqrMagnitude < portalJoystick.sensitivityMagnitude)
        {
            p.portalGun.SetLinesActive(false);
            slowdownRegenning = slowdownLeft < slowdownTime;
            slowdownLeft = Mathf.Min(slowdownLeft + Time.deltaTime, slowdownTime);
            if (Player.instance.hasStarted && !PauseMenuController.instance.isPaused) Time.timeScale = 1f;
            return;
        }

        p.portalGun.SetLinesActive(true);
        p.portalGun.AimPortal(portalJoystick.transform.localPosition, ShootOption.None);
        if (slowdownLeft > 0 && !slowdownRegenning) 
        {
            Time.timeScale = .5f;
            slowdownLeft = Mathf.Max(slowdownLeft - Time.unscaledDeltaTime, 0);
        }
        else
        {
            if (Player.instance.hasStarted && !PauseMenuController.instance.isPaused) Time.timeScale = 1f;
            slowdownRegenning = true;
            slowdownLeft = Mathf.Min(slowdownLeft + Time.deltaTime, slowdownTime);
        }
    }

    /// <summary>Tries to spawn a portal in the direction that the joystick is aiming in.</summary>
    /// <param name="aim">Direction in which to aim.</param>
    public void ShootPortal(Vector3 aim)
    {
        if (Settings.instance.leftClickForBothPortals)
        {
            Player.instance.portalGun.AimPortal(aim, ShootOption.Portal1);
        }
        else
        {
            
        }
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
        // Debug.Log("Swtichting to "  +index);
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

    public bool HoldingLeft()
    {
        if (Settings.instance.rotateCameraWithGravity || gravDir == Vector2.down) 
            return leftButton.buttonPressed;
        if (gravDir == Vector2.right)
            return jumpButton.buttonPressed;
        if (gravDir == Vector2.up)
            return leftButton.buttonPressed; // Double switch button
        else return false;
    }
    
    public bool HoldingRight() 
    {
        if (Settings.instance.rotateCameraWithGravity || gravDir == Vector2.down) 
            return rightButton.buttonPressed;
        if (gravDir == Vector2.left)
            return jumpButton.buttonPressed;
        if (gravDir == Vector2.up)
            return rightButton.buttonPressed; // Double switch button
        else return false;
    }

    public bool HoldingUp() 
    {
        if (Settings.instance.rotateCameraWithGravity || gravDir == Vector2.down) 
            return jumpButton.buttonPressed;
        if (gravDir == Vector2.left)
            return leftButton.buttonPressed; // Double switch button
        if (gravDir == Vector2.right)
            return rightButton.buttonPressed; // Double switch button
        else return false;
    }

    public bool HoldingDown() 
    {
        if (Settings.instance.rotateCameraWithGravity || gravDir == Vector2.down) 
            return false;
        if (gravDir == Vector2.left)
            return rightButton.buttonPressed; // Double switch button
        if (gravDir == Vector2.up)
            return jumpButton.buttonPressed;
        if (gravDir == Vector2.right)
            return leftButton.buttonPressed; // Double switch button
        return false; // Shouldn't reach here
    }

    public void Restart()
    {
        Player.instance.Restart();
    }

    public void RotateWithGravity(Vector2 gravDir)
    {

    }

    public void Enable() { gameObject.GetComponent<Canvas>().enabled = true; }
    public void Disable() { gameObject.GetComponent<Canvas>().enabled = false; }
}
