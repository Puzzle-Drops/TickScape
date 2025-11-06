using UnityEngine;

/// <summary>
/// Potion items that boost stats or restore hitpoints.
/// SDK Reference: Potion.ts
/// 
/// POTION MECHANICS:
/// - Has 1-4 doses
/// - Each dose consumed reduces dose count
/// - Shares food delay (3 ticks)
/// - Interrupts combat
/// - Leaves empty vial when all doses consumed
/// </summary>
public class Potion : Item
{
    [Header("Potion Properties")]
    [Tooltip("Number of doses remaining (1-4)")]
    public int doses = 4;

    [Header("Visual")]
    [Tooltip("Inventory sprite for this potion")]
    public Sprite inventorySprite;

    [Tooltip("Empty vial sprite (shown when doses = 0)")]
    public Sprite vialSprite;

    [Header("Audio")]
    [Tooltip("Sound played when drinking")]
    public AudioClip drinkSound;

    public Potion()
    {
        defaultAction = "Drink";
    }

    /// <summary>
    /// Calculate weight based on doses.
    /// SDK Reference: Potion.weight getter in Potion.ts lines 26-35
    /// 
    /// OSRS WEIGHTS:
    /// 4-dose: 0.035 kg
    /// 3-dose: 0.030 kg
    /// 2-dose: 0.025 kg
    /// 1-dose: 0.020 kg
    /// </summary>
    public override float Weight
    {
        get
        {
            switch (doses)
            {
                case 4: return 0.035f;
                case 3: return 0.030f;
                case 2: return 0.025f;
                case 1: return 0.020f;
                default: return 0f;
            }
        }
    }

    /// <summary>
    /// Potions can be left-clicked to drink.
    /// SDK Reference: Potion.hasInventoryLeftClick in Potion.ts line 43
    /// </summary>
    public override bool HasInventoryLeftClick
    {
        get { return true; }
    }

    /// <summary>
    /// Left-click to drink potion.
    /// SDK Reference: Potion.inventoryLeftClick() in Potion.ts lines 45-55
    /// 
    /// LOGIC:
    /// 1. Try to drink potion (if delay allows)
    /// 2. Decrement dose count (handled by Eating system)
    /// 3. If no doses left, remove from inventory
    /// 4. Play drink sound
    /// 5. Update inventory sprite
    /// </summary>
    public override void InventoryLeftClick(Player player)
    {
        bool didDrink = false;
        
        if (doses > 0)
        {
            didDrink = player.eats.DrinkPotion(this);
        }

        // Remove empty potion
        if (doses == 0)
        {
            ConsumeItem(player);
        }

        // Play sound if successfully drank
        if (didDrink && drinkSound != null)
        {
            AudioSource.PlayClipAtPoint(drinkSound, player.transform.position, 0.1f);
        }

        // Update sprite for remaining doses
        UpdateInventorySprite();
    }

    /// <summary>
    /// Apply potion effect to player.
    /// Called by Eating.tickFood() when potion is consumed.
    /// SDK Reference: Potion.drink() in Potion.ts lines 18-20
    /// 
    /// Override in subclasses to implement specific potion effects:
    /// - Super combat: boost attack/strength/defence
    /// - Saradomin brew: boost defence/HP, lower attack/strength
    /// - Super restore: restore prayer/stats
    /// etc.
    /// </summary>
    public virtual void Drink(Player player)
    {
        // Interrupt combat
        player.InterruptCombat();

        // Override in subclasses for specific effects
        Debug.Log($"[POTION] Drank {itemName}, {doses} doses remaining");
    }

    /// <summary>
    /// Update inventory sprite based on remaining doses.
    /// Override in subclasses to provide dose-specific sprites.
    /// SDK Reference: Potion.updateInventorySprite() in Potion.ts line 37
    /// </summary>
    public virtual void UpdateInventorySprite()
    {
        // Override in subclasses if using dose-specific sprites
        // For now, just keep the base sprite
    }
}
