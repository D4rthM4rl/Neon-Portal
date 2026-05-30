using UnityEngine;

/// <summary>
/// Re-applies responsive UI when the screen size or orientation changes.
/// </summary>
public class ResponsiveUIScreenWatcher : MonoBehaviour
{
    private void Start()
    {
        if (Settings.instance != null)
            ResponsiveUI.Apply(Settings.instance.platform);
    }

    private void Update()
    {
        if (Settings.instance != null)
            ResponsiveUI.RefreshIfNeeded(Settings.instance.platform);
    }
}
