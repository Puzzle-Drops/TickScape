using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Displays boosted/drained stats as a vertical overlay.
/// SDK Reference: BoostPanel.ts
/// 
/// VISUAL DESIGN:
/// - Shows only stats that differ from base (boosted or drained)
/// - Each stat is 24x24 pixel cell (scaled)
/// - Stacks vertically from bottom to top
/// - Semi-transparent black background (alpha 0.5)
/// - Green text (+X) if boosted, red text (-X) if drained
/// - Icon + boost amount for each stat
/// - ALWAYS displays in order: Attack, Strength, Defence, Range, Magic
/// </summary>
public static class BoostPanel
{
    // Cell size in pixels (SDK constant)
    private const float CELL_SIZE = 24f;

    // Stats in CORRECT display order
    // SDK Reference: BOOST_VISIBLE_STATS in BoostPanel.ts lines 12-18
    private static readonly string[] BOOST_VISIBLE_STATS = new string[]
    {
        "attack",
        "strength",
        "defence",
        "range",
        "magic"
    };

    // Cached skill icons
    private static Dictionary<string, Texture2D> skillIcons = new Dictionary<string, Texture2D>();
    private static bool iconsLoaded = false;

    /// <summary>
    /// Load skill icons from Resources.
    /// </summary>
    private static void LoadIcons()
    {
        if (iconsLoaded) return;

        skillIcons["attack"] = TextureLoader.LoadTexture("UI/Skills/attack_icon", Color.red, 24, 24);
        skillIcons["strength"] = TextureLoader.LoadTexture("UI/Skills/strength_icon", Color.green, 24, 24);
        skillIcons["defence"] = TextureLoader.LoadTexture("UI/Skills/defence_icon", Color.blue, 24, 24);
        skillIcons["range"] = TextureLoader.LoadTexture("UI/Skills/range_icon", Color.yellow, 24, 24);
        skillIcons["magic"] = TextureLoader.LoadTexture("UI/Skills/magic_icon", Color.cyan, 24, 24);

        iconsLoaded = true;
    }

    /// <summary>
    /// Draw boost panel overlay.
    /// SDK Reference: BoostPanel.draw() in BoostPanel.ts lines 30-60
    /// </summary>
    public static void Draw(float scale, Player player, bool panelsMinimized)
    {
        if (player == null || player.currentStats == null || player.stats == null)
            return;

        // Load icons if needed
        LoadIcons();

        // Find boosted stats IN CORRECT ORDER
        List<BoostedStat> boostedStats = GetBoostedStats(player);

        if (boostedStats.Count == 0)
            return; // Nothing to display

        // REVERSE THE LIST so it draws top-to-bottom correctly
        // Since we draw from bottom-up, reversing makes attack appear at top
        boostedStats.Reverse();

        // Get position based on minimized state
        Vector2 boostPos;
        if (panelsMinimized)
        {
            boostPos = UILayout.GetBoostPanelPositionMinimized();
        }
        else
        {
            boostPos = UILayout.GetBoostPanelPosition();
        }

        float panelScale = UIScale.GetPanelScale();
        float panelHeight = 275f * panelScale;
        float xLeft = boostPos.x;
        float yBottom = boostPos.y + panelHeight;

        // Calculate dimensions for wider layout (icon + text side by side)
        float iconSize = CELL_SIZE * scale;
        float rowHeight = CELL_SIZE * scale;
        float panelWidth = iconSize * 2.5f; // Wider to accommodate icon + text
        float totalHeight = rowHeight * boostedStats.Count;

        // Draw semi-transparent black background
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(xLeft, yBottom - totalHeight, panelWidth, totalHeight), Texture2D.whiteTexture);
        GUI.color = oldColor;

        // Draw each boosted stat (from bottom to top, but list is reversed)
        float y = yBottom - rowHeight;

        GUIStyle textStyle = UIFonts.CreateTextStyle(
            Mathf.RoundToInt(14 * scale),
            Color.white,
            TextAnchor.MiddleLeft  // Left align for text next to icon
        );

        foreach (BoostedStat boostedStat in boostedStats)
        {
            // Draw icon on the left
            Texture2D icon = skillIcons[boostedStat.statName];
            Rect iconRect = new Rect(xLeft + (2 * scale), y, iconSize, iconSize);
            GUI.DrawTexture(iconRect, icon);

            // Draw boost amount text to the right of icon
            string boostText = boostedStat.GetBoostText();
            Color textColor = boostedStat.boostAmount < 0 ? Color.red : Color.green;
            textStyle.normal.textColor = textColor;

            Rect textRect = new Rect(
                xLeft + iconSize + (4 * scale),  // Position to right of icon with small gap
                y,
                panelWidth - iconSize - (6 * scale),  // Remaining width
                rowHeight
            );

            UIFonts.DrawShadowedText(textRect, boostText, textStyle, scale);

            // Move up for next stat
            y -= rowHeight;
        }
    }

    /// <summary>
    /// Get list of stats that are boosted or drained, IN DISPLAY ORDER.
    /// CRITICAL: Always returns stats in order: Attack, Strength, Defence, Range, Magic
    /// </summary>
    private static List<BoostedStat> GetBoostedStats(Player player)
    {
        List<BoostedStat> result = new List<BoostedStat>();

        // Iterate in DISPLAY ORDER
        foreach (string statName in BOOST_VISIBLE_STATS)
        {
            int baseValue = GetStatValue(player.stats, statName);
            int currentValue = GetStatValue(player.currentStats, statName);

            if (currentValue != baseValue)
            {
                result.Add(new BoostedStat
                {
                    statName = statName,
                    boostAmount = currentValue - baseValue
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Get stat value by name using reflection-free approach.
    /// </summary>
    private static int GetStatValue(UnitStats stats, string statName)
    {
        switch (statName)
        {
            case "attack": return stats.attack;
            case "strength": return stats.strength;
            case "defence": return stats.defence;
            case "range": return stats.range;
            case "magic": return stats.magic;
            default: return 0;
        }
    }

    /// <summary>
    /// Helper class for boosted stat display.
    /// </summary>
    private class BoostedStat
    {
        public string statName;
        public int boostAmount;

        public string GetBoostText()
        {
            if (boostAmount > 0)
                return $"+{boostAmount}";
            else
                return boostAmount.ToString(); // Already has minus sign
        }
    }
}