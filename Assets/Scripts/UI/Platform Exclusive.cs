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
        
        gameObject.SetActive(Settings.instance.platform == platform);
    }
}
