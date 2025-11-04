using UnityEngine;

/// <summary>
/// Calculates UI element positions based on screen size and scale.
/// SDK Reference: ControlPanelController positioning methods
/// </summary>
public static class UILayout
{
    private const float TAB_WIDTH = 33;
    private const float TAB_HEIGHT = 36;
    private const float PANEL_WIDTH = 204;
    private const float PANEL_HEIGHT = 275;

    /// <summary>
    /// Get tab position for given index.
    /// SDK Reference: ControlPanelController.tabPosition() in ControlPanelController.ts lines 148-165
    /// 
    /// Desktop: 7 tabs across, 2 rows
    /// Mobile: 7 tabs on each side (not implemented yet)
    /// </summary>
    public static Vector2 GetTabPosition(int index)
    {
        float scale = UIScale.CalculateTabScale();
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (UISettings.IsMobile())
        {
            // TODO: Mobile layout (Phase 5)
            Debug.LogWarning("[UILayout] Mobile layout not yet implemented!");
            return Vector2.zero;
        }
        else
        {
            // Desktop layout: Grid at bottom-right
            int x = index % 7;
            int y = index / 7;

            // Note: Unity's OnGUI uses top-left origin, so we don't need to flip Y
            return new Vector2(
                screenWidth - (231 * scale) + (x * TAB_WIDTH * scale),
                screenHeight - (72 * scale) + (y * TAB_HEIGHT * scale)
            );
        }
    }

    /// <summary>
    /// Get panel position (appears above selected tab).
    /// SDK Reference: ControlPanelController.controlPosition() in ControlPanelController.ts lines 226-247
    /// </summary>
    public static Vector2 GetPanelPosition()
    {
        float scale = UIScale.CalculateTabScale();
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (UISettings.IsMobile())
        {
            // TODO: Mobile layout
            return Vector2.zero;
        }
        else
        {
            // Desktop: Above tabs, right-aligned
            return new Vector2(
                screenWidth - (188 * scale),
                screenHeight - (72 * scale) - (251 * scale)
            );
        }
    }

    /// <summary>
    /// Get boost panel position (small stat boost overlay).
    /// SDK Reference: ControlPanelController.boostPosition() in ControlPanelController.ts lines 130-146
    /// </summary>
    public static Vector2 GetBoostPanelPosition()
    {
        float scale = UIScale.CalculateTabScale();
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (UISettings.IsMobile())
        {
            // TODO: Mobile layout
            return new Vector2(15, 0);
        }
        else
        {
            // Desktop: Above tabs, left of panel
            return new Vector2(
                screenWidth - (231 * scale) + (28 * scale),
                screenHeight - (72 * scale)
            );
        }
    }

    /// <summary>
    /// Check if mouse is over a UI element.
    /// </summary>
    public static bool IsMouseOverRect(Rect rect)
    {
        Vector2 mousePos = Input.mousePosition;
        // Flip Y coordinate for Unity's screen space
        mousePos.y = Screen.height - mousePos.y;
        return rect.Contains(mousePos);
    }
}