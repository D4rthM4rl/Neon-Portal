using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instance;

    /// <summary>The player GameObject the camera should follow.</summary>
    private GameObject player;
    /// <summary>The currently active Cinemachine virtual camera.</summary>
    [System.NonSerialized] public ICinemachineCamera virtualCamera;
    /// <summary>The Cinemachine Brain which drives the main camera.</summary>
    [System.NonSerialized] private CinemachineBrain brain;
    /// <summary>
    /// The speed at which the camera rotates to match the player's gravity direction
    /// in degrees per second.
    /// </summary>
    public float cameraRotateSpeed = 360f;

    private Coroutine returnToPlayerCoroutine;
    private float savedConfinerDamping;
    private float savedXDamping;
    private float savedYDamping;
    private float savedZDamping;
    private bool hasSavedDamping;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        brain = GetComponent<CinemachineBrain>();
        if (brain == null)
        {
            Debug.LogError("CinemachineBrain component not found on the camera.");
            return;
        }
        brain.m_IgnoreTimeScale = true;
        StartCoroutine(GetCamera(brain));
    }

    /// <summary>
    /// Sets the virtual camera to follow the player once the player and camera are available.
    /// </summary>
    /// <param name="brain">The Cinemachine Brain which controls the camera.</param>
    private IEnumerator GetCamera(CinemachineBrain brain)
    {
        player = Player.instance.gameObject;

        if (player == null)
        {
            Debug.LogError("Player GameObject with tag 'Player' not found.");
            yield break;
        }
        
        // Wait until the active virtual camera is a CinemachineVirtualCamera
        while (brain.ActiveVirtualCamera == null)
        {
            yield return null;
        }

        virtualCamera = brain.ActiveVirtualCamera;
        
        if (!virtualCamera.Follow) virtualCamera.Follow = player.transform;


        float targetAspect = 16f / 9f; // your original design aspect
        float windowAspect = (float)Screen.width / (float)Screen.height;
        float scaleHeight = windowAspect / targetAspect;
        CameraState state = virtualCamera.State;
        CinemachineVirtualCamera realVC = virtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
        

        if (scaleHeight > 1.0f)
        {
            realVC.m_Lens.OrthographicSize /= scaleHeight;
        }
    }

    void Update()
    {
        if (player != null && virtualCamera != null && Settings.instance != null && Settings.instance.rotateCameraWithGravity)
        {
            Transform virtualCameraTransform = virtualCamera.VirtualCameraGameObject.transform;
            Vector2 grav = player.GetComponent<Player>().gravityDirection.normalized;
            float targetAngle = Vector2.SignedAngle(Vector2.down, grav);

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

            virtualCameraTransform.rotation = Quaternion.RotateTowards(
                virtualCameraTransform.rotation,
                targetRotation,
                cameraRotateSpeed * Time.unscaledDeltaTime
            );
            // Quaternion backgroundRotation = Quaternion.Euler(0f, 0f, -targetAngle);
            // bg.transform.rotation = backgroundRotation;
        }
    }

    /// <summary>
    /// Smoothly returns the camera to the player after a death or reset while gameplay time is paused.
    /// </summary>
    /// <param name="duration">How long the return animation should take in real time.</param>
    public void BeginReturnToPlayer(float duration = 0.5f)
    {
        if (returnToPlayerCoroutine != null)
        {
            StopCoroutine(returnToPlayerCoroutine);
            RestoreFollowDamping();
        }
        returnToPlayerCoroutine = StartCoroutine(ReturnToPlayerRoutine(duration));
    }

    private IEnumerator ReturnToPlayerRoutine(float duration)
    {
        SetFollowDamping(0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (brain != null)
                brain.ManualUpdate();
            yield return null;
        }

        RestoreFollowDamping();
        returnToPlayerCoroutine = null;
    }

    private void SetFollowDamping(float damping)
    {
        if (virtualCamera == null)
            return;

        CinemachineVirtualCamera realVC =
            virtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
        if (realVC == null)
            return;

        CinemachineConfiner2D confiner = realVC.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
        {
            if (!hasSavedDamping)
                savedConfinerDamping = confiner.m_Damping;
            confiner.m_Damping = damping;
        }

        CinemachineFramingTransposer transposer =
            realVC.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
        {
            if (!hasSavedDamping)
            {
                savedXDamping = transposer.m_XDamping;
                savedYDamping = transposer.m_YDamping;
                savedZDamping = transposer.m_ZDamping;
                hasSavedDamping = true;
            }
            transposer.m_XDamping = damping;
            transposer.m_YDamping = damping;
            transposer.m_ZDamping = damping;
        }
    }

    private void RestoreFollowDamping()
    {
        if (virtualCamera == null)
            return;

        CinemachineVirtualCamera realVC =
            virtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();
        if (realVC == null)
            return;

        CinemachineConfiner2D confiner = realVC.GetComponent<CinemachineConfiner2D>();
        if (confiner != null)
            confiner.m_Damping = savedConfinerDamping;

        CinemachineFramingTransposer transposer =
            realVC.GetCinemachineComponent<CinemachineFramingTransposer>();
        if (transposer != null)
        {
            transposer.m_XDamping = savedXDamping;
            transposer.m_YDamping = savedYDamping;
            transposer.m_ZDamping = savedZDamping;
        }

        hasSavedDamping = false;
    }
}
