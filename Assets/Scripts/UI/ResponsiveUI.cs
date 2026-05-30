using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies responsive scaling to UI canvases for phones, tablets, and desktops.
/// </summary>
public static class ResponsiveUI
{
    public static readonly Vector2 DesignReferenceResolution = new Vector2(3492f, 1964f);

    private static Vector2 lastScreenSize;
    private static PlatformType lastPlatform = PlatformType.Computer;
    private static bool initialized;

    public static float LayoutScale { get; private set; } = 1f;

    public static void Apply(PlatformType platform)
    {
        lastPlatform = platform;
        lastScreenSize = new Vector2(Screen.width, Screen.height);
        LayoutScale = GetLayoutScale(platform);

        CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
        foreach (CanvasScaler scaler in scalers)
        {
            ConfigureCanvasScaler(scaler, platform);
        }

        GridLayoutGroup[] grids = Object.FindObjectsByType<GridLayoutGroup>(FindObjectsSortMode.None);
        foreach (GridLayoutGroup grid in grids)
        {
            ScaleGridLayout(grid, platform);
        }

        ApplySafeAreaToMenuRoots();
        initialized = true;
    }

    public static void RefreshIfNeeded(PlatformType platform)
    {
        if (!initialized || platform != lastPlatform || Screen.width != (int)lastScreenSize.x
            || Screen.height != (int)lastScreenSize.y)
        {
            Apply(platform);
        }
    }

    public static float GetMatchWidthOrHeight(Vector2 referenceResolution)
    {
        float logWidth = Mathf.Log(Screen.width / referenceResolution.x, 2f);
        float logHeight = Mathf.Log(Screen.height / referenceResolution.y, 2f);
        if (Mathf.Approximately(logWidth + logHeight, 0f))
            return 0.5f;

        float match = logWidth / (logWidth + logHeight);

        float aspect = (float)Screen.width / Screen.height;
        if (aspect < 0.62f)
            match = Mathf.Min(match, 0.42f);

        return Mathf.Clamp01(match);
    }

    private static float GetLayoutScale(PlatformType platform)
    {
        switch (platform)
        {
            case PlatformType.Phone:
                return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 430f, 0.82f, 1.05f);
            case PlatformType.Tablet:
                return Mathf.Clamp(Mathf.Min(Screen.width, Screen.height) / 820f, 1f, 1.35f);
            default:
                return 1f;
        }
    }

    private static void ConfigureCanvasScaler(CanvasScaler scaler, PlatformType platform)
    {
        if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return;

        scaler.referenceResolution = DesignReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = GetMatchWidthOrHeight(DesignReferenceResolution);

        if (platform == PlatformType.Tablet)
            scaler.referencePixelsPerUnit = 82f;
        else if (platform == PlatformType.Phone)
            scaler.referencePixelsPerUnit = 96f;
        else
            scaler.referencePixelsPerUnit = 100f;
    }

    private static void ScaleGridLayout(GridLayoutGroup grid, PlatformType platform)
    {
        if (platform == PlatformType.Computer)
            return;

        RectTransform parent = grid.transform.parent as RectTransform;
        if (parent == null)
            return;

        Canvas.ForceUpdateCanvases();
        float parentWidth = parent.rect.width;
        if (parentWidth <= 0f)
            return;

        int columns = grid.constraint == GridLayoutGroup.Constraint.FixedColumnCount
            ? grid.constraintCount
            : Mathf.Max(1, Mathf.FloorToInt((parentWidth + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));

        float spacing = grid.spacing.x * (columns - 1);
        float cellWidth = (parentWidth - spacing - grid.padding.horizontal) / columns;

        float minCell = platform == PlatformType.Tablet ? 170f : 145f;
        float maxCell = platform == PlatformType.Tablet ? 280f : 220f;
        cellWidth = Mathf.Clamp(cellWidth, minCell, maxCell);

        grid.cellSize = new Vector2(cellWidth, cellWidth);
    }

    private static void ApplySafeAreaToMenuRoots()
    {
        if (lastPlatform == PlatformType.Computer)
            return;

        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode == RenderMode.WorldSpace)
                continue;

            RectTransform canvasRect = canvas.transform as RectTransform;
            if (canvasRect == null)
                continue;

            for (int i = 0; i < canvasRect.childCount; i++)
            {
                RectTransform child = canvasRect.GetChild(i) as RectTransform;
                if (child == null || !IsFullScreenStretch(child))
                    continue;

                if (child.GetComponent<Background>() != null)
                    continue;

                ApplySafeArea(child);
            }
        }
    }

    private static bool IsFullScreenStretch(RectTransform rectTransform)
    {
        return rectTransform.anchorMin.x <= 0.01f && rectTransform.anchorMin.y <= 0.01f
            && rectTransform.anchorMax.x >= 0.99f && rectTransform.anchorMax.y >= 0.99f;
    }

    public static void ApplySafeArea(RectTransform rectTransform)
    {
        Rect safeArea = Screen.safeArea;
        Vector2 screenSize = new Vector2(Screen.width, Screen.height);

        if (screenSize.x <= 0f || screenSize.y <= 0f)
            return;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= screenSize.x;
        anchorMin.y /= screenSize.y;
        anchorMax.x /= screenSize.x;
        anchorMax.y /= screenSize.y;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
