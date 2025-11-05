using UnityEngine;

/// <summary>
/// Displays vertical health and prayer bars attached to panel sides.
/// 
/// VISUAL DESIGN:
/// - Two vertical bars: HP left of panel, Prayer right of panel
/// - Full panel height (275 pixels * scale)
/// - Each bar: Icon at top, current value below, filled bar
/// - HP: Red foreground, black background (50% opacity)
/// - Prayer: Cyan foreground, black background (50% opacity)
/// - Bars fill from bottom to top based on percentage
/// - White text with shadow for current values
/// - When panels minimized, bars move together to right side
/// </summary>
public static class HealthPanel
{
    // Bar dimensions
    private const float BAR_WIDTH = 18f;
    private const float ICON_SIZE = 12f;

    // Cached icons
    private static Texture2D hitpointsIcon;
    private static Texture2D prayerIcon;
    private static bool iconsLoaded = false;

    /// <summary>
    /// Load skill icons from Resources.
    /// </summary>
    private static void LoadIcons()
    {
        if (iconsLoaded) return;

        hitpointsIcon = TextureLoader.LoadTexture("UI/Skills/hitpoints_icon", Color.red, 20, 20);
        prayerIcon = TextureLoader.LoadTexture("UI/Skills/prayer_icon", Color.cyan, 20, 20);

        iconsLoaded = true;
    }

    /// <summary>
    /// Draw health and prayer bars at panel height.
    /// </summary>
    public static void Draw(float scale, Player player, bool panelsMinimized)
    {
        if (player == null || player.currentStats == null || player.stats == null)
            return;

        // Load icons if needed
        LoadIcons();

        // Get positions from UILayout
        float panelScale = UIScale.GetPanelScale();
        float panelHeight = 275 * panelScale;
        float barHeight = panelHeight;
        float barWidth = BAR_WIDTH * scale;  // 16px

        // Prayer bar always at same position
        Vector2 prayerPos = UILayout.GetPrayerBarPosition();
        DrawPrayerBar(prayerPos.x, prayerPos.y, barWidth, barHeight, scale, player);

        if (panelsMinimized)
        {
            // When minimized: HP bar moves to where panel was
            float hpX = prayerPos.x - barWidth - (4f * scale);
            DrawHealthBar(hpX, prayerPos.y, barWidth, barHeight, scale, player);
        }
        else
        {
            // Normal layout: HP bar attached to panel left
            Vector2 hpPos = UILayout.GetHPBarPosition();
            DrawHealthBar(hpPos.x, hpPos.y, barWidth, barHeight, scale, player);
        }
    }

    /// <summary>
    /// Draw the health bar (red).
    /// </summary>
    private static void DrawHealthBar(float x, float y, float barWidth, float barHeight, float scale, Player player)
    {
        PlayerStats currentStats = player.currentStats as PlayerStats;
        PlayerStats baseStats = player.stats as PlayerStats;

        if (currentStats == null || baseStats == null) return;

        int currentHP = currentStats.hitpoint;
        int maxHP = baseStats.hitpoint;

        // Calculate fill percentage (cap at 100%)
        float fillPercent = Mathf.Clamp01((float)currentHP / (float)maxHP);

        // Draw background (semi-transparent black)
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

        // Draw red foreground (filled from bottom)
        float fillHeight = barHeight * fillPercent;
        GUI.color = new Color(0.8f, 0, 0); // Dark red
        GUI.DrawTexture(new Rect(x, y + (barHeight - fillHeight), barWidth, fillHeight), Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw icon at top
        float iconSize = ICON_SIZE * scale;
        float iconX = x + (barWidth - iconSize) / 2f;
        float iconY = y + (4f * scale);
        GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), hitpointsIcon);

        // Draw current HP text below icon
        string hpText = currentHP.ToString();
        GUIStyle textStyle = UIFonts.CreateTextStyle(
            Mathf.RoundToInt(12 * scale),
            Color.white,
            TextAnchor.MiddleCenter
        );

        Rect textRect = new Rect(
            x,
            iconY + iconSize + (2f * scale),
            barWidth,
            20 * scale
        );

        UIFonts.DrawShadowedText(textRect, hpText, textStyle, scale);
    }

    /// <summary>
    /// Draw the prayer bar (cyan).
    /// </summary>
    private static void DrawPrayerBar(float x, float y, float barWidth, float barHeight, float scale, Player player)
    {
        PlayerStats currentStats = player.currentStats as PlayerStats;
        PlayerStats baseStats = player.stats as PlayerStats;

        if (currentStats == null || baseStats == null) return;

        int currentPrayer = currentStats.prayer;
        int maxPrayer = baseStats.prayer;

        // Calculate fill percentage (cap at 100%)
        float fillPercent = Mathf.Clamp01((float)currentPrayer / (float)maxPrayer);

        // Draw background (semi-transparent black)
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

        // Draw cyan foreground (filled from bottom)
        float fillHeight = barHeight * fillPercent;
        GUI.color = new Color(0, 0.8f, 0.8f); // Cyan
        GUI.DrawTexture(new Rect(x, y + (barHeight - fillHeight), barWidth, fillHeight), Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw icon at top
        float iconSize = ICON_SIZE * scale;
        float iconX = x + (barWidth - iconSize) / 2f;
        float iconY = y + (4f * scale);
        GUI.DrawTexture(new Rect(iconX, iconY, iconSize, iconSize), prayerIcon);

        // Draw current prayer text below icon
        string prayerText = currentPrayer.ToString();
        GUIStyle textStyle = UIFonts.CreateTextStyle(
            Mathf.RoundToInt(12 * scale),
            Color.white,
            TextAnchor.MiddleCenter
        );

        Rect textRect = new Rect(
            x,
            iconY + iconSize + (2f * scale),
            barWidth,
            20 * scale
        );

        UIFonts.DrawShadowedText(textRect, prayerText, textStyle, scale);
    }
}