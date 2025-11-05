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

        // Boost attack
        int attackBoost = Mathf.FloorToInt(player.stats.attack * 0.15f) + 5;
        player.currentStats.attack += attackBoost;
        player.currentStats.attack = Mathf.Min(player.currentStats.attack, player.stats.attack + attackBoost);

        // Boost strength
        int strengthBoost = Mathf.FloorToInt(player.stats.strength * 0.15f) + 5;
        player.currentStats.strength += strengthBoost;
        player.currentStats.strength = Mathf.Min(player.currentStats.strength, player.stats.strength + strengthBoost);

        // Boost defence
        int defenceBoost = Mathf.FloorToInt(player.stats.defence * 0.15f) + 5;
        player.currentStats.defence += defenceBoost;
        player.currentStats.defence = Mathf.Min(player.currentStats.defence, player.stats.defence + defenceBoost);

        Debug.Log($"[POTION] Drank super combat: ATK +{attackBoost}, STR +{strengthBoost}, DEF +{defenceBoost}");
    }
}