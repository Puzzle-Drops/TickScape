using UnityEngine;

/// <summary>
/// Saradomin brew - heals HP, boosts defence, but drains combat stats.
/// SDK Reference: SaradominBrew.ts
/// 
/// EFFECTS:
/// - Heals: floor(maxHP * 0.15) + 2
/// - Defence boost: floor(currentDef * 0.20) + 2 (capped at floor(baseDef * 0.20) + 2)
/// - Drains: floor(currentStat * 0.10) + 2 for attack/strength/range/magic
/// </summary>
public class SaradominBrew : Potion
{
    public SaradominBrew()
    {
        doses = 4;
        itemName = ItemName.SARADOMIN_BREW;
        defaultAction = "Drink";
    }

    /// <summary>
    /// Heal, boost defence, drain combat stats.
    /// SDK Reference: SaradominBrew.drink() in SaradominBrew.ts lines 33-66
    /// </summary>
    public override void Drink(Player player)
    {
        base.Drink(player);

        // Heal hitpoints
        int healAmount = Mathf.FloorToInt(player.stats.hitpoint * 0.15f) + 2;
        player.currentStats.hitpoint += healAmount;
        player.currentStats.hitpoint = Mathf.Max(1,
            Mathf.Min(player.currentStats.hitpoint, player.stats.hitpoint + healAmount));

        // Boost defence (calculated from current, capped, never lowered)
        int defenceBoost = Mathf.FloorToInt(player.currentStats.defence * 0.2f) + 2;
        int maxDefenceBoost = Mathf.FloorToInt(player.stats.defence * 0.2f) + 2;
        int oldDefence = player.currentStats.defence;
        player.currentStats.defence = oldDefence + defenceBoost;
        player.currentStats.defence = Mathf.Max(1, Mathf.Min(player.currentStats.defence, player.stats.defence + maxDefenceBoost));
        player.currentStats.defence = Mathf.Max(player.currentStats.defence, oldDefence);
        int actualDefenceBoost = player.currentStats.defence - oldDefence;

        // Drain attack
        int attackNerf = Mathf.FloorToInt(player.currentStats.attack * 0.1f) + 2;
        player.currentStats.attack -= attackNerf;
        player.currentStats.attack = Mathf.Max(1,
            Mathf.Min(player.currentStats.attack, player.stats.attack));

        // Drain strength
        int strengthNerf = Mathf.FloorToInt(player.currentStats.strength * 0.1f) + 2;
        player.currentStats.strength -= strengthNerf;
        player.currentStats.strength = Mathf.Max(1,
            Mathf.Min(player.currentStats.strength, player.stats.strength));

        // Drain range
        int rangeNerf = Mathf.FloorToInt(player.currentStats.range * 0.1f) + 2;
        player.currentStats.range -= rangeNerf;
        player.currentStats.range = Mathf.Max(1,
            Mathf.Min(player.currentStats.range, player.stats.range));

        // Drain magic
        int magicNerf = Mathf.FloorToInt(player.currentStats.magic * 0.1f) + 2;
        player.currentStats.magic -= magicNerf;
        player.currentStats.magic = Mathf.Max(1,
            Mathf.Min(player.currentStats.magic, player.stats.magic));

        Debug.Log($"[POTION] Drank brew: Healed {healAmount}, DEF +{actualDefenceBoost}, combat stats drained");
    }
}