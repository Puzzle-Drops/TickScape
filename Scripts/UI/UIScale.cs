using UnityEngine;

/// <summary>
/// Calculates UI scaling factors based on screen size.
/// SDK Reference: ControlPanelController.getTabScale() in ControlPanelController.ts
/// </summary>
public static class UIScale
{
    // Base panel dimensions (from SDK)
    private const float BASE_WIDTH = 33 * 7;   // 231px
    private const float BASE_HEIGHT = 36 * 2 + 275; // 347px
    private const float TAB_SIZE = 33;
    private const float TAB_HEIGHT = 36;

    /// <summary>
    /// Calculate tab scale based on screen size.
    /// SDK Reference: ControlPanelController.getTabScale() in ControlPanelController.ts lines 111-128
    /// </summary>
    public static float CalculateTabScale()
    {
        float screenHeight = Screen.height;
        
        // TODO: When MapController is implemented, use actual map height
        float mapHeight = 170 * GetMinimapScale();
        
        float controlAreaHeight = screenHeight - mapHeight;
        float scaleRatio = controlAreaHeight / 7 / TAB_HEIGHT;

        // Get max scale from settings
        float maxScale = UISettings.Instance != null 
            ? UISettings.Instance.maxUiScale 
            : 1.0f;

        // Mobile would have different max scale (1.1x)
        if (UISettings.IsMobile() && Screen.width > 600)
        {
            maxScale *= 1.1f;
        }

        // Clamp to max scale
        if (scaleRatio > maxScale)
        {
            scaleRatio = maxScale;
        }

        // Store calculated scale
        if (UISettings.Instance != null)
        {
            UISettings.Instance.controlPanelScale = scaleRatio * 0.915f;
        }

        return scaleRatio;
    }

    /// <summary>
    /// Get minimap scale.
    /// TODO: Make this dynamic when MapController exists
    /// </summary>
    private static float GetMinimapScale()
    {
        return UISettings.Instance != null 
            ? UISettings.Instance.minimapScale 
            : 1.0f;
    }

    /// <summary>
    /// Get panel scale (slightly smaller than tab scale).
    /// SDK Reference: Uses controlPanelScale which is tabScale * 0.915
    /// </summary>
    public static float GetPanelScale()
    {
        return UISettings.Instance != null 
            ? UISettings.Instance.controlPanelScale 
            : CalculateTabScale() * 0.915f;
    }
}