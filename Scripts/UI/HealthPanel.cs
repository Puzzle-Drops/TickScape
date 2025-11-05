using UnityEngine;

/// <summary>
/// Displays vertical health and prayer bars.
/// 
/// VISUAL DESIGN:
/// - Two vertical bars side-by-side (HP left, Prayer right)
/// - Each bar: Icon at top, current value below, filled bar
/// - HP: Red foreground, black background (50% opacity)
/// - Prayer: Cyan foreground, black background (50% opacity)
/// - Bars fill from bottom to top based on percentage
/// - White text with shadow for current values
/// - When panels minimized, moves to right side of screen
/// </summary>
public static class HealthPanel
{
    // Bar dimensions
    private const float BAR_WIDTH = 24f;
    private const float BAR_HEIGHT = 120f;
    private const float ICON_SIZE = 20f;
    private const float BAR_SPACING = 4f; // Space between HP and Prayer bars

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
    /// Draw health and prayer bars.
    /// </summary>
    public static void Draw(float scale, Player player, bool panelsMinimized)
    {
        if (player == null || player.currentStats == null || player.stats == null)
            return;

        // Load icons if needed
        LoadIcons();

        // Calculate positions
        Vector2 basePosition = UILayout.GetBoostPanelPosition();
        float xLeft = basePosition.x;
        float yBottom = basePosition.y;

        // If panels are minimized, move to right side
        if (panelsMinimized)
        {
            // Position: Right edge - prayer bar - hp bar - boost panel
            // We need to account for boost panel width first
            int boostedStatCount = CountBoostedStats(player);
            float boostPanelWidth = boostedStatCount > 0 ? (24f * scale) : 0;

            xLeft = Screen.width - (10f * scale) - (BAR_WIDTH * scale * 2) - (BAR_SPACING * scale) - boostPanelWidth;
        }
        else
        {
            // Position to the right of boost panel (30px left of normal position + boost panel width)
            int boostedStatCount = CountBoostedStats(player);
            float boostPanelWidth = boostedStatCount > 0 ? (24f * scale) : 0;
            xLeft = xLeft - (30f * scale) + boostPanelWidth + (BAR_SPACING * scale);
        }

        // Draw HP bar
        DrawHealthBar(xLeft, yBottom, scale, player);

        // Draw Prayer bar (to the right of HP bar)
        DrawPrayerBar(xLeft + (BAR_WIDTH * scale) + (BAR_SPACING * scale), yBottom, scale, player);
    }

    /// <summary>
    /// Draw the health bar (red).
    /// </summary>
    private static void DrawHealthBar(float x, float y, float scale, Player player)
    {
        PlayerStats currentStats = player.currentStats as PlayerStats;
        PlayerStats baseStats = player.stats as PlayerStats;

        if (currentStats == null || baseStats == null) return;

        int currentHP = currentStats.hitpoint;
        int maxHP = baseStats.hitpoint;

        // Calculate fill percentage (cap at 100%)
        float fillPercent = Mathf.Clamp01((float)currentHP / (float)maxHP);

        // Bar dimensions
        float barWidth = BAR_WIDTH * scale;
        float barHeight = BAR_HEIGHT * scale;
        float barTop = y - barHeight;

        // Draw background (semi-transparent black)
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(x, barTop, barWidth, barHeight), Texture2D.whiteTexture);

        // Draw red foreground (filled from bottom)
        float fillHeight = barHeight * fillPercent;
        GUI.color = new Color(0.8f, 0, 0); // Dark red
        GUI.DrawTexture(new Rect(x, barTop + (barHeight - fillHeight), barWidth, fillHeight), Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw icon at top (maintain aspect ratio)
        float iconSize = ICON_SIZE * scale;
        float iconX = x + (barWidth - iconSize) / 2f; // Center horizontally
        float iconY = barTop + (4f * scale); // 4px from top
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
            iconY + iconSize + (2f * scale), // 2px below icon
            barWidth,
            20 * scale
        );

        UIFonts.DrawShadowedText(textRect, hpText, textStyle, scale);
    }

    /// <summary>
    /// Draw the prayer bar (cyan).
    /// </summary>
    private static void DrawPrayerBar(float x, float y, float scale, Player player)
    {
        PlayerStats currentStats = player.currentStats as PlayerStats;
        PlayerStats baseStats = player.stats as PlayerStats;

        if (currentStats == null || baseStats == null) return;

        int currentPrayer = currentStats.prayer;
        int maxPrayer = baseStats.prayer;

        // Calculate fill percentage (cap at 100%)
        float fillPercent = Mathf.Clamp01((float)currentPrayer / (float)maxPrayer);

        // Bar dimensions
        float barWidth = BAR_WIDTH * scale;
        float barHeight = BAR_HEIGHT * scale;
        float barTop = y - barHeight;

        // Draw background (semi-transparent black)
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(x, barTop, barWidth, barHeight), Texture2D.whiteTexture);

        // Draw cyan foreground (filled from bottom)
        float fillHeight = barHeight * fillPercent;
        GUI.color = new Color(0, 0.8f, 0.8f); // Cyan
        GUI.DrawTexture(new Rect(x, barTop + (barHeight - fillHeight), barWidth, fillHeight), Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw icon at top (maintain aspect ratio)
        float iconSize = ICON_SIZE * scale;
        float iconX = x + (barWidth - iconSize) / 2f; // Center horizontally
        float iconY = barTop + (4f * scale); // 4px from top
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
            iconY + iconSize + (2f * scale), // 2px below icon
            barWidth,
            20 * scale
        );

        UIFonts.DrawShadowedText(textRect, prayerText, textStyle, scale);
    }

    /// <summary>
    /// Count how many stats are currently boosted (for spacing calculation).
    /// </summary>
    private static int CountBoostedStats(Player player)
    {
        if (player == null || player.currentStats == null || player.stats == null)
            return 0;

        int count = 0;

        if (player.currentStats.attack != player.stats.attack) count++;
        if (player.currentStats.strength != player.stats.strength) count++;
        if (player.currentStats.defence != player.stats.defence) count++;
        if (player.currentStats.range != player.stats.range) count++;
        if (player.currentStats.magic != player.stats.magic) count++;

        return count;
    }
}