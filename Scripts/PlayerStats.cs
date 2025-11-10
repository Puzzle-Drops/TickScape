using UnityEngine;

/// <summary>
/// Player-specific stats that extend UnitStats.
/// Includes agility, run energy, and special attack.
/// Matches SDK's PlayerStats.ts interface.
/// SDK Reference: PlayerStats.ts
/// </summary>
[System.Serializable]
public class PlayerStats : UnitStats
{
    [Header("Player-Only Stats")]
    [Tooltip("Affects run energy regeneration rate")]
    public int agility = 99;

    [Tooltip("Run energy (0-10000, displayed as 0-100%)")]
    [Range(0, 10000)]
    public int run = 10000;

    [Tooltip("Special attack energy (0-100%)")]
    [Range(0, 100)]
    public int specialAttack = 100;

    /// <summary>
    /// Create a deep copy of player stats.
    /// </summary>
    public new PlayerStats Clone()
    {
        return new PlayerStats
        {
            attack = this.attack,
            strength = this.strength,
            defence = this.defence,
            range = this.range,
            magic = this.magic,
            hitpoint = this.hitpoint,
            prayer = this.prayer,
            agility = this.agility,
            run = this.run,
            specialAttack = this.specialAttack
        };
    }
}
