using UnityEngine;

/// <summary>
/// Calculates UI element positions based on screen size and scale.
/// Navigation tabs are the anchor - everything positions relative to them.
/// </summary>
public static class UILayout
{
    private const float TAB_WIDTH = 33;
    private const float TAB_HEIGHT = 36;
    private const float PANEL_WIDTH = 204;
    private const float PANEL_HEIGHT = 275;

    // Bar widths - easily changeable
    private const float HP_BAR_WIDTH = 16;
    private const float PRAYER_BAR_WIDTH = 16;

    /// <summary>
    /// Get tab position for given index.
    /// Tabs are right-aligned with rightmost tab touching screen edge.
    /// </summary>
    public static Vector2 GetTabPosition(int index)
    {
        float scale = UIScale.CalculateTabScale();
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (UISettings.IsMobile())
        {
            Debug.LogWarning("[UILayout] Mobile layout not yet implemented!");
            return Vector2.zero;
        }
        else
        {
            // Desktop: Single row at bottom, right-aligned to screen edge
            int x = index % 7;

            // Right-align tabs flush with screen edge
            float totalTabsWidth = 7 * TAB_WIDTH * scale;
            float tabsStartX = screenWidth - totalTabsWidth;

            return new Vector2(
                tabsStartX + (x * TAB_WIDTH * scale),
                screenHeight - (TAB_HEIGHT * scale)
            );
        }
    }

    /// <summary>
    /// Get panel position (centered above inventory tab - index 3).
    /// </summary>
    public static Vector2 GetPanelPosition()
    {
        float scale = UIScale.CalculateTabScale();
        float panelScale = UIScale.GetPanelScale();
        float screenHeight = Screen.height;

        if (UISettings.IsMobile())
        {
            return Vector2.zero;
        }
        else
        {
            // Get inventory tab (index 3) position
            Vector2 inventoryTabPos = GetTabPosition(3);
            float tabWidth = TAB_WIDTH * scale;

            // Center panel above inventory tab
            float tabCenterX = inventoryTabPos.x + (tabWidth / 2f);
            float panelX = tabCenterX - (PANEL_WIDTH * panelScale / 2f);

            // Panel Y: Directly above tabs
            float panelY = screenHeight - (TAB_HEIGHT * scale) - (PANEL_HEIGHT * panelScale);

            return new Vector2(panelX, panelY);
        }
    }

    /// <summary>
    /// Get HP bar position (attached to left of panel).
    /// </summary>
    public static Vector2 GetHPBarPosition()
    {
        Vector2 panelPos = GetPanelPosition();
        float scale = UIScale.CalculateTabScale();
        float barWidth = HP_BAR_WIDTH * scale;

        return new Vector2(
            panelPos.x - barWidth,  // Directly attached to panel left
            panelPos.y
        );
    }

    /// <summary>
    /// Get Prayer bar position (attached to right of panel).
    /// </summary>
    public static Vector2 GetPrayerBarPosition()
    {
        Vector2 panelPos = GetPanelPosition();
        float panelScale = UIScale.GetPanelScale();
        float panelWidth = PANEL_WIDTH * panelScale;

        return new Vector2(
            panelPos.x + panelWidth,  // Directly attached to panel right
            panelPos.y
        );
    }

    /// <summary>
    /// Get boost panel position (left of HP bar).
    /// </summary>
    public static Vector2 GetBoostPanelPosition()
    {
        Vector2 hpBarPos = GetHPBarPosition();
        float scale = UIScale.CalculateTabScale();
        float boostWidth = 24f * 2.5f * scale;  // Wider panel for icon + text

        return new Vector2(
            hpBarPos.x - boostWidth - (4f * scale),  // Left of HP with small gap
            hpBarPos.y
        );
    }

    /// <summary>
    /// Get boost panel position when minimized.
    /// </summary>
    public static Vector2 GetBoostPanelPositionMinimized()
    {
        // When minimized, boost panel goes where panel was
        Vector2 prayerPos = GetPrayerBarPosition();
        float scale = UIScale.CalculateTabScale();
        float hpBarWidth = HP_BAR_WIDTH * scale;
        float boostWidth = 24f * 2.5f * scale;  // Wider panel for icon + text

        // Position: prayer stays, HP to its left, boost to HP's left
        float hpX = prayerPos.x - hpBarWidth - (4f * scale);
        float boostX = hpX - boostWidth - (4f * scale);

        return new Vector2(boostX, prayerPos.y);
    }

    /// <summary>
    /// Check if mouse is over a UI element.
    /// </summary>
    public static bool IsMouseOverRect(Rect rect)
    {
        Vector2 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y;
        return rect.Contains(mousePos);
    }
}