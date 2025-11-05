using UnityEngine;

/// <summary>
/// Stamina potion - restores run energy and provides stamina effect.
/// SDK Reference: StaminaPotion.ts
/// 
/// EFFECTS:
/// - Restores 2000 run energy (20%)
/// - Stamina effect for 200 ticks (2 minutes)
/// - Stamina effect reduces run drain to 30% of normal
/// </summary>
public class StaminaPotion : Potion
{
    public StaminaPotion()
    {
        doses = 4;
        itemName = ItemName.STAMINA_POTION;
        defaultAction = "Drink";
    }

    /// <summary>
    /// Restore run energy and apply stamina effect.
    /// SDK Reference: StaminaPotion.drink() in StaminaPotion.ts lines 33-38
    /// </summary>
    public override void Drink(Player player)
    {
        base.Drink(player);

        PlayerStats pStats = player.currentStats as PlayerStats;
        if (pStats == null)
        {
            Debug.LogError("[POTION] StaminaPotion requires PlayerStats!");
            return;
        }

        // Apply stamina effect (200 ticks = 2 minutes)
        player.effects.stamina = 200;

        // Restore run energy
        pStats.run += 2000;
        pStats.run = Mathf.Clamp(pStats.run, 0, 10000);

        Debug.Log($"[POTION] Drank stamina: Run energy +2000, stamina effect active for 200 ticks");
    }
}