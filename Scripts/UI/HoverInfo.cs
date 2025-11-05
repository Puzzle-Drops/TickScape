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

    public HoverInfo(Item item, Player player, Vector2 mousePosition, List<StatEffect> effects)
    {
        this.item = item;
        this.player = player;
        this.mousePosition = mousePosition;
        this.effects = effects;
    }
}