using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Justiciar armor set effect.
/// SDK Reference: Referenced in Weapon.attack() in Weapon.ts lines 112-120
/// 
/// SET BONUS:
/// - Requires: Faceguard, Chestguard, Legguards
/// - Effect: Reduces incoming damage by (defence bonus / 3000)
/// - Max reduction: ~30% with high defence bonus
/// 
/// CALCULATION:
/// damageReduction = min(defenceBonus / 3000, 1.0)
/// finalDamage = originalDamage * (1 - damageReduction)
/// 
/// Example: With 900 stab defence:
/// damageReduction = 900 / 3000 = 0.30 (30%)
/// 50 damage → 35 damage
/// </summary>
public class JusticiarSetEffect : SetEffect
{
    public override SetEffectType GetEffectName()
    {
        return SetEffectType.JUSTICIAR;
    }

    public override List<ItemName> GetItemsInSet()
    {
        return new List<ItemName>
        {
            ItemName.JUSTICIAR_FACEGUARD,
            ItemName.JUSTICIAR_CHESTGUARD,
            ItemName.JUSTICIAR_LEGGUARDS
        };
    }

    /// <summary>
    /// Apply Justiciar damage reduction.
    /// SDK Reference: Weapon.attack() in Weapon.ts lines 112-120
    /// </summary>
    public override int ModifyIncomingDamage(int damage, Unit attacker, Unit defender, string attackStyle)
    {
        // Get defender's defence bonus for this attack style
        int defenceBonus = GetDefenceBonusForStyle(defender, attackStyle);

        if (defenceBonus <= 0)
        {
            return damage; // No reduction if no defence bonus
        }

        // Calculate damage reduction
        float reductionPercent = Mathf.Max(defenceBonus / 3000f, 0f);
        reductionPercent = Mathf.Min(reductionPercent, 1.0f); // Cap at 100%

        int damageReduction = Mathf.CeilToInt(reductionPercent * damage);
        int finalDamage = damage - damageReduction;

        Debug.Log($"[JUSTICIAR] Reduced damage: {damage} → {finalDamage} ({reductionPercent * 100f:F1}% reduction)");

        return finalDamage;
    }

    /// <summary>
    /// Get defence bonus for specific attack style.
    /// SDK Reference: Weapon.attack() in Weapon.ts line 114
    /// </summary>
    private int GetDefenceBonusForStyle(Unit unit, string attackStyle)
    {
        switch (attackStyle)
        {
            case "stab": return unit.bonuses.defence.stab;
            case "slash": return unit.bonuses.defence.slash;
            case "crush": return unit.bonuses.defence.crush;
            case "magic": return unit.bonuses.defence.magic;
            case "range": return unit.bonuses.defence.range;
            default: return 0;
        }
    }
}
