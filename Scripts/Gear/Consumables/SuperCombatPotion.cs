using UnityEngine;

/// <summary>
/// Super combat potion - boosts attack, strength, and defence.
/// SDK Reference: SuperCombatPotion.ts
/// 
/// BOOST FORMULA: floor(baseStat * 0.15) + 5
/// Example: 99 attack → floor(99 * 0.15) + 5 = 14 + 5 = +19 levels (to 118)
/// </summary>
public class SuperCombatPotion : Potion
{
    public SuperCombatPotion()
    {
        doses = 4;
        itemName = ItemName.SUPER_COMBAT_POTION;
        defaultAction = "Drink";
    }

    /// <summary>
    /// Boost attack, strength, and defence.
    /// SDK Reference: SuperCombatPotion.drink() in SuperCombatPotion.ts lines 31-41
    /// </summary>
    public override void Drink(Player player)
    {
        base.Drink(player);

        // Boost attack (calculated from base, applied to current, capped, never lowered)
        int attackBoost = Mathf.FloorToInt(player.stats.attack * 0.15f) + 5;
        int oldAttack = player.currentStats.attack;
        player.currentStats.attack = oldAttack + attackBoost;
        player.currentStats.attack = Mathf.Min(player.currentStats.attack, player.stats.attack + attackBoost);
        player.currentStats.attack = Mathf.Max(player.currentStats.attack, oldAttack);
        int actualAttackBoost = player.currentStats.attack - oldAttack;

        // Boost strength (calculated from base, applied to current, capped, never lowered)
        int strengthBoost = Mathf.FloorToInt(player.stats.strength * 0.15f) + 5;
        int oldStrength = player.currentStats.strength;
        player.currentStats.strength = oldStrength + strengthBoost;
        player.currentStats.strength = Mathf.Min(player.currentStats.strength, player.stats.strength + strengthBoost);
        player.currentStats.strength = Mathf.Max(player.currentStats.strength, oldStrength);
        int actualStrengthBoost = player.currentStats.strength - oldStrength;

        // Boost defence (calculated from base, applied to current, capped, never lowered)
        int defenceBoost = Mathf.FloorToInt(player.stats.defence * 0.15f) + 5;
        int oldDefence = player.currentStats.defence;
        player.currentStats.defence = oldDefence + defenceBoost;
        player.currentStats.defence = Mathf.Min(player.currentStats.defence, player.stats.defence + defenceBoost);
        player.currentStats.defence = Mathf.Max(player.currentStats.defence, oldDefence);
        int actualDefenceBoost = player.currentStats.defence - oldDefence;

        Debug.Log($"[POTION] Drank super combat: ATK +{actualAttackBoost}, STR +{actualStrengthBoost}, DEF +{actualDefenceBoost}");
    }

}