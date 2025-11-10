using UnityEngine;

/// <summary>
/// Represents a single stat effect from consuming an item.
/// Used for tooltip display and preview calculations.
/// </summary>
[System.Serializable]
public class StatEffect
{
    public string statName;      // "Hitpoints", "Defence", etc.
    public int change;           // +16, -13
    public int currentValue;     // 99
    public int newValue;         // 115

    public bool IsPositive => change > 0;
    public bool IsNegative => change < 0;
    public bool HasEffect => change != 0;

    public StatEffect(string statName, int change, int currentValue, int newValue)
    {
        this.statName = statName;
        this.change = change;
        this.currentValue = currentValue;
        this.newValue = newValue;
    }
}