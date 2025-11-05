using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Complete information about a hovered item for tooltips and previews.
/// </summary>
public class HoverInfo
{
    public Item item;
    public Player player;
    public Vector2 mousePosition;           // Screen space position
    public List<StatEffect> effects;        // All calculated effects
    public string action;                   // Optional custom action text (e.g., "Unequip")

    public HoverInfo(Item item, Player player, Vector2 mousePosition, List<StatEffect> effects, string action = null)
    {
        this.item = item;
        this.player = player;
        this.mousePosition = mousePosition;
        this.effects = effects;
        this.action = action;
    }
}
