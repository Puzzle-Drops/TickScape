using UnityEngine;

/// <summary>
/// Super restore - restores prayer and drained stats.
/// SDK Reference: SuperRestore.ts
/// 
/// RESTORE FORMULAS:
/// Prayer: floor(basePrayer * 0.27) + 8
/// Other stats (if below base): floor(baseStat * 0.25) + 8
/// </summary>
public class SuperRestore : Potion
{
    public SuperRestore()
    {
        doses = 4;
        itemName = ItemName.SUPER_RESTORE;
        defaultAction = "Drink";
    }

    /// <summary>
    /// Restore prayer and drained stats.
    /// SDK Reference: SuperRestore.drink() in SuperRestore.ts lines 33-60
    /// </summary>
    public override void Drink(Player player)
    {
        base.Drink(player);

        PlayerStats pStats = player.currentStats as PlayerStats;
        PlayerStats baseStats = player.stats as PlayerStats;

        if (pStats == null || baseStats == null)
        {
            Debug.LogError("[POTION] SuperRestore requires PlayerStats!");
            return;
        }

        // Restore prayer (always)
        int prayerBonus = Mathf.FloorToInt(baseStats.prayer * 0.27f) + 8;
        pStats.prayer += prayerBonus;
        pStats.prayer = Mathf.Min(pStats.prayer, baseStats.prayer);

        // Restore attack (if below base)
        if (pStats.attack < baseStats.attack)
        {
            int attackBonus = Mathf.FloorToInt(baseStats.attack * 0.25f) + 8;
            pStats.attack += attackBonus;
            pStats.attack = Mathf.Min(pStats.attack, baseStats.attack);
        }

        // Restore strength (if below base)
        if (pStats.strength < baseStats.strength)
        {
            int strengthBonus = Mathf.FloorToInt(baseStats.strength * 0.25f) + 8;
            pStats.strength += strengthBonus;
            pStats.strength = Mathf.Min(pStats.strength, baseStats.strength);
        }

        // Restore range (if below base)
        if (pStats.range < baseStats.range)
        {
            int rangeBonus = Mathf.FloorToInt(baseStats.range * 0.25f) + 8;
            pStats.range += rangeBonus;
            pStats.range = Mathf.Min(pStats.range, baseStats.range);
        }

        // Restore magic (if below base)
        if (pStats.magic < baseStats.magic)
        {
            int magicBonus = Mathf.FloorToInt(baseStats.magic * 0.25f) + 8;
            pStats.magic += magicBonus;
            pStats.magic = Mathf.Min(pStats.magic, baseStats.magic);
        }

        Debug.Log($"[POTION] Drank super restore: Prayer restored, drained stats restored");
    }
}