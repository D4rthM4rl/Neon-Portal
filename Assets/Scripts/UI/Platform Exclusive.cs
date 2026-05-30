using UnityEngine;

public class PlatformExclusive : MonoBehaviour
{
    [SerializeField] private PlatformType platform;

    private void Awake()
    {
        if (!Settings.instance)
        {
            Debug.LogWarning("No settings instance, can't disable " + gameObject);
            return;
        }

        if (platform == PlatformType.Phone)
            gameObject.SetActive(Settings.UsesTouchControls);
        else if (platform == PlatformType.Computer)
            gameObject.SetActive(!Settings.UsesTouchControls);
        else
            gameObject.SetActive(Settings.instance.platform == platform);
    }
}
