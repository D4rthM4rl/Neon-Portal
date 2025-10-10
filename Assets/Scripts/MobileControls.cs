using UnityEngine;
using UnityEngine.UI;

public class MobileControls : MonoBehaviour
{
    public static MobileControls instance;

    [SerializeField] private UpDownButton leftButton;
    [SerializeField] private UpDownButton rightButton;
    [SerializeField] private UpDownButton jumpButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button restartButton;
    

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool HoldingLeft() { return leftButton.buttonPressed; }
    
    public bool HoldingRight() { return rightButton.buttonPressed; }

    public bool HoldingUp() { return jumpButton.buttonPressed; }

    public void Restart()
    {
        Player.instance.Restart();
    }

    public void Enable() { gameObject.GetComponent<Canvas>().enabled = true; }
    public void Disable() { gameObject.GetComponent<Canvas>().enabled = false; }
}
