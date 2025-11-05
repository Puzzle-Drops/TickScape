using UnityEngine;

/// <summary>
/// Bastion potion - boosts range and defence.
/// SDK Reference: BastionPotion.ts
/// 
/// BOOST FORMULAS:
/// Range: floor(baseStat * 0.10) + 4
/// Defence: floor(baseStat * 0.15) + 5
/// </summary>
public class BastionPotion : Potion
{
    public BastionPotion()
    {
        doses = 4;
        itemName = ItemName.BASTION_POTION;
        defaultAction = "Drink";
    }

    /// <summary>
    /// Boost range and defence.
    /// SDK Reference: BastionPotion.drink() in BastionPotion.ts lines 33-42
    /// </summary>
    public override void Drink(Player player)
    {
        base.Drink(player);

        // Boost range (calculated from base, applied to current, capped, never lowered)
        int rangeBoost = Mathf.FloorToInt(player.stats.range * 0.1f) + 4;
        int oldRange = player.currentStats.range;
        player.currentStats.range = oldRange + rangeBoost;
        player.currentStats.range = Mathf.Min(player.currentStats.range, player.stats.range + rangeBoost);
        player.currentStats.range = Mathf.Max(player.currentStats.range, oldRange);
        int actualRangeBoost = player.currentStats.range - oldRange;

        // Boost defence (calculated from base, applied to current, capped, never lowered)
        int defenceBoost = Mathf.FloorToInt(player.stats.defence * 0.15f) + 5;
        int oldDefence = player.currentStats.defence;
        player.currentStats.defence = oldDefence + defenceBoost;
        player.currentStats.defence = Mathf.Min(player.currentStats.defence, player.stats.defence + defenceBoost);
        player.currentStats.defence = Mathf.Max(player.currentStats.defence, oldDefence);
        int actualDefenceBoost = player.currentStats.defence - oldDefence;

        Debug.Log($"[POTION] Drank bastion: RNG +{actualRangeBoost}, DEF +{actualDefenceBoost}");
    }

    public override float Weight
    {
        get
        {
            // SDK Reference: BastionPotion.weight in BastionPotion.ts lines 26-30
            if (doses == 4)
                return 0.134f;
            return 0.03f; // "Yep, lol" - SDK comment
        }
    }
}
