using UnityEngine;
using System.Linq;

/// <summary>
/// Static storage for currently hovered item information.
/// Used by HealthPanel for previews and TooltipRenderer for display.
/// </summary>
public static class HoverPreview
{
    public static HoverInfo currentHover = null;

    /// <summary>
    /// Set the currently hovered item with all calculated effects.
    /// </summary>
    public static void SetHover(Item item, Player player, Vector2 mousePosition)
    {
        if (item == null || player == null)
        {
            Clear();
            return;
        }

        var effects = EffectCalculator.CalculateEffects(item, player);
        currentHover = new HoverInfo(item, player, mousePosition, effects);
    }

    /// <summary>
    /// Clear hover information.
    /// </summary>
    public static void Clear()
    {
        currentHover = null;
    }

    /// <summary>
    /// Get total HP change from hovered item (for preview bar).
    /// </summary>
    public static int GetHPChange()
    {
        if (currentHover == null) return 0;

        var hpEffect = currentHover.effects.FirstOrDefault(e => e.statName == "Hitpoints");
        return hpEffect?.change ?? 0;
    }

    /// <summary>
    /// Get total Prayer change from hovered item (for preview bar).
    /// </summary>
    public static int GetPrayerChange()
    {
        if (currentHover == null) return 0;

        var prayerEffect = currentHover.effects.FirstOrDefault(e => e.statName == "Prayer");
        return prayerEffect?.change ?? 0;
    }
}