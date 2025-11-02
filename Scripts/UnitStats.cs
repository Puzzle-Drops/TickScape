using UnityEngine;

/// <summary>
/// Base stats for all units (Players and Mobs).
/// Matches SDK's UnitStats.ts interface.
/// SDK Reference: UnitStats.ts
/// </summary>
[System.Serializable]
public class UnitStats
{
    [Header("Combat Stats")]
    public int attack = 99;
    public int strength = 99;
    public int defence = 99;
    public int range = 99;
    public int magic = 99;

    [Header("Hitpoints")]
    public int hitpoint = 99;

    [Header("Optional Stats")]
    [Tooltip("Used for prayer drain calculations")]
    public int prayer = 99;

    /// <summary>
    /// Create a deep copy of these stats.
    /// Used for creating currentStats from base stats.
    /// </summary>
    public UnitStats Clone()
    {
        return new UnitStats
        {
            attack = this.attack,
            strength = this.strength,
            defence = this.defence,
            range = this.range,
            magic = this.magic,
            hitpoint = this.hitpoint,
            prayer = this.prayer
        };
    }
}
