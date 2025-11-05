using UnityEngine;

/// <summary>
/// Equipment bonuses for attack, defence, and other combat modifiers.
/// Matches SDK's UnitBonuses interface from Unit.ts.
/// SDK Reference: Unit.ts lines 21-44
/// </summary>
[System.Serializable]
public class UnitBonuses
{
    public UnitStyleBonuses attack = new UnitStyleBonuses();
    public UnitStyleBonuses defence = new UnitStyleBonuses();
    public UnitOtherBonuses other = new UnitOtherBonuses();
    public UnitTargetBonuses targetSpecific = new UnitTargetBonuses();

    /// <summary>
    /// Create empty bonuses (all zeros).
    /// Used as starting point for equipment bonus calculations.
    /// </summary>
    public static UnitBonuses Empty()
    {
        return new UnitBonuses
        {
            attack = new UnitStyleBonuses(),
            defence = new UnitStyleBonuses(),
            other = new UnitOtherBonuses(),
            targetSpecific = new UnitTargetBonuses()
        };
    }

    /// <summary>
    /// Merge two sets of bonuses (used for combining equipment bonuses).
    /// SDK Reference: Unit.mergeEquipmentBonuses() in Unit.ts
    /// </summary>
    public static UnitBonuses Merge(UnitBonuses first, UnitBonuses second)
    {
        return new UnitBonuses
        {
            attack = new UnitStyleBonuses
            {
                stab = first.attack.stab + second.attack.stab,
                slash = first.attack.slash + second.attack.slash,
                crush = first.attack.crush + second.attack.crush,
                magic = first.attack.magic + second.attack.magic,
                range = first.attack.range + second.attack.range
            },
            defence = new UnitStyleBonuses
            {
                stab = first.defence.stab + second.defence.stab,
                slash = first.defence.slash + second.defence.slash,
                crush = first.defence.crush + second.defence.crush,
                magic = first.defence.magic + second.defence.magic,
                range = first.defence.range + second.defence.range
            },
            other = new UnitOtherBonuses
            {
                meleeStrength = first.other.meleeStrength + second.other.meleeStrength,
                rangedStrength = first.other.rangedStrength + second.other.rangedStrength,
                magicDamage = first.other.magicDamage + second.other.magicDamage,
                prayer = first.other.prayer + second.other.prayer,
                crystalAccuracy = first.other.crystalAccuracy + second.other.crystalAccuracy,
                crystalDamage = first.other.crystalDamage + second.other.crystalDamage
            },
            targetSpecific = new UnitTargetBonuses
            {
                undead = first.targetSpecific.undead + second.targetSpecific.undead,
                slayer = first.targetSpecific.slayer + second.targetSpecific.slayer
            }
        };
    }
}

/// <summary>
/// Attack and defence bonuses for each combat style.
/// SDK Reference: UnitStyleBonuses interface in Unit.ts
/// </summary>
[System.Serializable]
public class UnitStyleBonuses
{
    public int stab = 0;
    public int slash = 0;
    public int crush = 0;
    public int magic = 0;
    public int range = 0;
}

/// <summary>
/// Other combat bonuses (strength, damage multipliers, prayer).
/// SDK Reference: UnitOtherBonuses interface in Unit.ts
/// </summary>
[System.Serializable]
public class UnitOtherBonuses
{
    [Tooltip("Melee strength bonus (increases max hit)")]
    public int meleeStrength = 0;

    [Tooltip("Ranged strength bonus (increases max hit)")]
    public int rangedStrength = 0;

    [Tooltip("Magic damage multiplier (1.0 = no bonus)")]
    public float magicDamage = 1.0f;

    [Tooltip("Prayer bonus (reduces drain rate)")]
    public int prayer = 0;

    [Tooltip("Crystal accuracy multiplier (Bowfa, Crystal armor)")]
    public float crystalAccuracy = 1.0f;

    [Tooltip("Crystal damage multiplier (Bowfa, Crystal armor)")]
    public float crystalDamage = 1.0f;
}

/// <summary>
/// Target-specific bonuses (undead, slayer tasks).
/// SDK Reference: UnitTargetBonuses interface in Unit.ts
/// </summary>
[System.Serializable]
public class UnitTargetBonuses
{
    [Tooltip("Bonus damage vs undead (Salve amulet)")]
    public int undead = 0;

    [Tooltip("Bonus damage vs slayer task (Slayer helm)")]
    public int slayer = 0;
}
