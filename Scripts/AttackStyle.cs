using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Attack styles available to weapons.
/// SDK Reference: AttackStyle enum in AttackStylesController.ts
/// </summary>
public enum AttackStyle
{
    ACCURATE,
    AGGRESSIVE,
    DEFENSIVE,
    RAPID,
    LONGRANGE
}

/// <summary>
/// Weapon type categories for attack style selection.
/// SDK Reference: AttackStyleTypes enum (simplified)
/// </summary>
public enum AttackStyleType
{
    MELEE,
    RANGED,
    MAGIC
}

/// <summary>
/// Manages attack styles and XP calculations.
/// SDK Reference: AttackStylesController.ts lines 1-200
/// </summary>
public class AttackStylesController
{
    private static AttackStylesController instance;
    public static AttackStylesController Instance
    {
        get
        {
            if (instance == null)
                instance = new AttackStylesController();
            return instance;
        }
    }

    // Maps weapon types to their current attack style
    private Dictionary<AttackStyleType, AttackStyle> stylesMap = new Dictionary<AttackStyleType, AttackStyle>();

    /// <summary>
    /// Get current attack style for weapon type.
    /// SDK Reference: getAttackStyleForType() in AttackStylesController.ts
    /// </summary>
    public AttackStyle GetAttackStyle(AttackStyleType type)
    {
        if (!stylesMap.ContainsKey(type))
        {
            // Default styles
            if (type == AttackStyleType.RANGED)
                stylesMap[type] = AttackStyle.RAPID;
            else
                stylesMap[type] = AttackStyle.ACCURATE;
        }
        return stylesMap[type];
    }

    /// <summary>
    /// Set attack style for weapon type.
    /// SDK Reference: setWeaponAttackStyle() in AttackStylesController.ts
    /// </summary>
    public void SetAttackStyle(AttackStyleType type, AttackStyle style)
    {
        stylesMap[type] = style;
    }

    /// <summary>
    /// Get strength bonus for melee attack styles.
    /// SDK Reference: attackStyleStrengthBonus in AttackStylesController.ts
    /// Aggressive = +3, Defensive (controlled) = +1, Others = 0
    /// </summary>
    public int GetStrengthBonus(AttackStyle style)
    {
        switch (style)
        {
            case AttackStyle.AGGRESSIVE:
                return 3;
            case AttackStyle.DEFENSIVE:
                return 1; // Controlled style
            default:
                return 0;
        }
    }

    /// <summary>
    /// Calculate XP drops for given attack style and damage.
    /// SDK Reference: getWeaponXpDrops() in AttackStylesController.ts lines 150-155
    /// 
    /// XP Multipliers (from SDK):
    /// - Accurate (Melee): 4x attack, 1.33x HP
    /// - Accurate (Ranged): 4x range, 1.33x HP  
    /// - Aggressive: 4x strength, 1.33x HP
    /// - Defensive (Controlled): 1.33x attack/str/def, 1.33x HP
    /// - Rapid: 4x range, 1.33x HP
    /// - Longrange: 2x range, 2x defence, 1.33x HP
    /// </summary>
    public List<XpDrop> GetXpDrops(AttackStyle style, AttackStyleType weaponType, int damage, float npcMultiplier)
    {
        List<XpDrop> drops = new List<XpDrop>();

        switch (style)
        {
            case AttackStyle.ACCURATE:
                if (weaponType == AttackStyleType.RANGED)
                {
                    drops.Add(new XpDrop("range", damage * 4 * npcMultiplier));
                }
                else if (weaponType == AttackStyleType.MAGIC)
                {
                    drops.Add(new XpDrop("magic", damage * 4 * npcMultiplier));
                }
                else // Melee
                {
                    drops.Add(new XpDrop("attack", damage * 4 * npcMultiplier));
                }
                drops.Add(new XpDrop("hitpoint", damage * 1.33f * npcMultiplier));
                break;

            case AttackStyle.AGGRESSIVE:
                drops.Add(new XpDrop("strength", damage * 4 * npcMultiplier));
                drops.Add(new XpDrop("hitpoint", damage * 1.33f * npcMultiplier));
                break;

            case AttackStyle.DEFENSIVE:
                // Controlled style - 1.33x to attack/strength/defence
                drops.Add(new XpDrop("attack", damage * 1.33f * npcMultiplier));
                drops.Add(new XpDrop("strength", damage * 1.33f * npcMultiplier));
                drops.Add(new XpDrop("defence", damage * 1.33f * npcMultiplier));
                drops.Add(new XpDrop("hitpoint", damage * 1.33f * npcMultiplier));
                break;

            case AttackStyle.RAPID:
                drops.Add(new XpDrop("range", damage * 4 * npcMultiplier));
                drops.Add(new XpDrop("hitpoint", damage * 1.33f * npcMultiplier));
                break;

            case AttackStyle.LONGRANGE:
                drops.Add(new XpDrop("range", damage * 2 * npcMultiplier));
                drops.Add(new XpDrop("defence", damage * 2 * npcMultiplier));
                drops.Add(new XpDrop("hitpoint", damage * 1.33f * npcMultiplier));
                break;
        }

        return drops;
    }

}

