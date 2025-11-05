using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders item tooltips with action text and stat effects.
/// </summary>
public static class TooltipRenderer
{
    private const float PADDING_X = 10f;
    private const float PADDING_Y = 5f;
    private const float LINE_HEIGHT = 20f;
    private const float LINE_SPACING = 2f;

    /// <summary>
    /// Draw tooltip for currently hovered item.
    /// </summary>
    public static void Draw(HoverInfo hover, float scale)
    {
        if (hover == null || hover.item == null) return;

        // Calculate tooltip position
        Vector2 tooltipPos = CalculateTooltipPosition(hover.mousePosition, scale);

        // Calculate dimensions
        var lines = BuildTooltipLines(hover);
        float width = CalculateWidth(lines, scale);
        float height = CalculateHeight(lines.Count, scale);

        // Clamp to screen bounds and flip if needed
        tooltipPos = ClampToScreen(tooltipPos, width, height);

        // Draw background
        DrawBackground(tooltipPos.x, tooltipPos.y, width, height);

        // Draw lines
        DrawLines(tooltipPos.x, tooltipPos.y, lines, scale);
    }

    /// <summary>
    /// Build all tooltip lines with formatting info.
    /// </summary>
    private static List<TooltipLine> BuildTooltipLines(HoverInfo hover)
    {
        var lines = new List<TooltipLine>();

        // Line 1: Action + Item Name
        string action = GetActionText(hover.item);
        string itemName = GetItemDisplayName(hover.item);
        lines.Add(new TooltipLine(action, itemName, LineType.Header));

        // Skip effects for equipment (just show header)
        if (hover.item is Equipment)
        {
            return lines;
        }

        // Add blank line if we have effects
        if (hover.effects.Count > 0)
        {
            lines.Add(new TooltipLine("", "", LineType.Spacer));
        }

        // Effect lines (only non-zero)
        foreach (var effect in hover.effects)
        {
            if (effect.HasEffect)
            {
                string sign = effect.IsPositive ? "+" : "";
                string text = $"{sign}{effect.change} ({effect.newValue}) {effect.statName}";
                LineType type = effect.IsPositive ? LineType.Positive : LineType.Negative;
                lines.Add(new TooltipLine(text, "", type));
            }
        }

        return lines;
    }

    /// <summary>
    /// Get action text based on item type.
    /// </summary>
    private static string GetActionText(Item item)
    {
        if (item is Potion) return "Drink";
        if (item is Food) return "Eat";
        if (item is Equipment) return "Equip";
        return "Use";
    }

    /// <summary>
    /// Get formatted item display name.
    /// </summary>
    private static string GetItemDisplayName(Item item)
    {
        string name = item.itemName.ToString();

        // Replace underscores with spaces
        name = name.Replace("_", " ");

        // Convert to title case
        name = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());

        // Add dose count for potions
        if (item is Potion potion && potion.doses > 0)
        {
            name += $" ({potion.doses})";
        }

        return name;
    }

    /// <summary>
    /// Calculate tooltip position relative to mouse.
    /// </summary>
    private static Vector2 CalculateTooltipPosition(Vector2 mousePos, float scale)
    {
        // Convert screen space to GUI space
        float guiMouseY = Screen.height - mousePos.y;

        // Position below cursor at same distance as inventory cell height
        float offsetY = 35f * scale; // GRID_CELL_HEIGHT from InventoryPanel

        return new Vector2(mousePos.x, guiMouseY + offsetY);
    }

    /// <summary>
    /// Calculate tooltip width based on content.
    /// </summary>
    private static float CalculateWidth(List<TooltipLine> lines, float scale)
    {
        float maxWidth = 0;
        GUIStyle testStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(16 * scale), Color.white, TextAnchor.MiddleLeft);

        foreach (var line in lines)
        {
            if (line.type == LineType.Spacer) continue;

            string fullText = line.type == LineType.Header ? $"{line.text} {line.secondaryText}" : line.text;
            Vector2 size = testStyle.CalcSize(new GUIContent(fullText));
            maxWidth = Mathf.Max(maxWidth, size.x);
        }

        return maxWidth + (PADDING_X * 2 * scale);
    }

    /// <summary>
    /// Calculate tooltip height based on line count.
    /// </summary>
    private static float CalculateHeight(int lineCount, float scale)
    {
        float totalHeight = 0;

        for (int i = 0; i < lineCount; i++)
        {
            totalHeight += LINE_HEIGHT * scale;
            if (i < lineCount - 1)
            {
                totalHeight += LINE_SPACING * scale;
            }
        }

        return totalHeight + (PADDING_Y * 2 * scale);
    }

    /// <summary>
    /// Clamp tooltip to screen bounds and flip if near bottom.
    /// </summary>
    private static Vector2 ClampToScreen(Vector2 pos, float width, float height)
    {
        // Clamp X to screen
        if (pos.x + width > Screen.width)
        {
            pos.x = Screen.width - width;
        }
        if (pos.x < 0)
        {
            pos.x = 0;
        }

        // Flip to above cursor if too close to bottom
        if (pos.y + height > Screen.height)
        {
            // Move above cursor instead (subtract height + offset)
            float offsetY = 35f * UIScale.CalculateTabScale();
            pos.y = pos.y - offsetY - height - offsetY;
        }

        // Clamp Y to screen
        if (pos.y < 0)
        {
            pos.y = 0;
        }
        if (pos.y + height > Screen.height)
        {
            pos.y = Screen.height - height;
        }

        return pos;
    }

    /// <summary>
    /// Draw semi-transparent background.
    /// </summary>
    private static void DrawBackground(float x, float y, float width, float height)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.25f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);

        // Draw border
        GUI.color = UIFonts.YellowText;
        GUI.Box(new Rect(x, y, width, height), "");
        GUI.color = oldColor;
    }

    /// <summary>
    /// Draw all tooltip lines with proper formatting.
    /// </summary>
    private static void DrawLines(float x, float y, List<TooltipLine> lines, float scale)
    {
        float currentY = y + (PADDING_Y * scale);

        foreach (var line in lines)
        {
            if (line.type == LineType.Spacer)
            {
                currentY += (LINE_HEIGHT * 0.5f * scale); // Half-height spacer
                continue;
            }

            if (line.type == LineType.Header)
            {
                DrawHeaderLine(x + (PADDING_X * scale), currentY, line, scale);
            }
            else
            {
                DrawEffectLine(x + (PADDING_X * scale), currentY, line, scale);
            }

            currentY += (LINE_HEIGHT * scale) + (LINE_SPACING * scale);
        }
    }

    /// <summary>
    /// Draw header line (Action + Item Name).
    /// </summary>
    private static void DrawHeaderLine(float x, float y, TooltipLine line, float scale)
    {
        GUIStyle actionStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(16 * scale), Color.white, TextAnchor.MiddleLeft);
        GUIStyle itemStyle = UIFonts.CreateTextStyle(Mathf.RoundToInt(16 * scale), UIFonts.YellowText, TextAnchor.MiddleLeft);

        // Draw action text (white)
        Vector2 actionSize = actionStyle.CalcSize(new GUIContent(line.text));
        UIFonts.DrawShadowedText(new Rect(x, y, actionSize.x, LINE_HEIGHT * scale), line.text, actionStyle, scale);

        // Draw item name (orange) after action
        float itemX = x + actionSize.x + (4 * scale); // Small space
        UIFonts.DrawShadowedText(new Rect(itemX, y, 300, LINE_HEIGHT * scale), line.secondaryText, itemStyle, scale);
    }

    /// <summary>
    /// Draw effect line (colored by positive/negative).
    /// </summary>
    private static void DrawEffectLine(float x, float y, TooltipLine line, float scale)
    {
        Color color = line.type == LineType.Positive ? new Color(0, 0.8f, 0) : new Color(0.8f, 0, 0);
        GUIStyle style = UIFonts.CreateTextStyle(Mathf.RoundToInt(16 * scale), color, TextAnchor.MiddleLeft);

        UIFonts.DrawShadowedText(new Rect(x, y, 300, LINE_HEIGHT * scale), line.text, style, scale);
    }

    /// <summary>
    /// Helper class for tooltip line data.
    /// </summary>
    private class TooltipLine
    {
        public string text;
        public string secondaryText;
        public LineType type;

        public TooltipLine(string text, string secondaryText, LineType type)
        {
            this.text = text;
            this.secondaryText = secondaryText;
            this.type = type;
        }
    }

    private enum LineType
    {
        Header,
        Positive,
        Negative,
        Spacer
    }
}