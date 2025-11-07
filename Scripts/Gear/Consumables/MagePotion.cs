using UnityEngine;

/// <summary>
/// Mage potion - boosts magic and defence.
/// 
/// BOOST FORMULAS:
/// Magic: floor(baseStat * 0.10) + 4
/// Defence: floor(baseStat * 0.15) + 5
/// </summary>
public class MagePotion : Potion
{
    public MagePotion()
    {
        doses = 4;
        itemName = ItemName.MAGE_POTION;
        defaultAction = "Drink";
    }

    /// <summary>
    /// Boost magic and defence.
    /// </summary>
    public override void Drink(Player player)
    {
        base.Drink(player);

        // Boost magic (calculated from base, applied to current, capped, never lowered)
        int magicBoost = Mathf.FloorToInt(player.stats.magic * 0.1f) + 4;
        int oldMagic = player.currentStats.magic;
        player.currentStats.magic = oldMagic + magicBoost;
        player.currentStats.magic = Mathf.Min(player.currentStats.magic, player.stats.magic + magicBoost);
        player.currentStats.magic = Mathf.Max(player.currentStats.magic, oldMagic);
        int actualMagicBoost = player.currentStats.magic - oldMagic;

        // Boost defence (calculated from base, applied to current, capped, never lowered)
        int defenceBoost = Mathf.FloorToInt(player.stats.defence * 0.15f) + 5;
        int oldDefence = player.currentStats.defence;
        player.currentStats.defence = oldDefence + defenceBoost;
        player.currentStats.defence = Mathf.Min(player.currentStats.defence, player.stats.defence + defenceBoost);
        player.currentStats.defence = Mathf.Max(player.currentStats.defence, oldDefence);
        int actualDefenceBoost = player.currentStats.defence - oldDefence;

        Debug.Log($"[POTION] Drank mage: MAG +{actualMagicBoost}, DEF +{actualDefenceBoost}");
    }

    public override float Weight
    {
        get
        {
            if (doses == 4)
                return 0.134f;
            return 0.03f;
        }
    }
}
