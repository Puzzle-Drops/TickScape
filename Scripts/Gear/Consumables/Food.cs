using UnityEngine;

/// <summary>
/// Food items that restore hitpoints when eaten.
/// SDK Reference: Food.ts
/// 
/// EATING MECHANICS:
/// - Heals specified amount
/// - Interrupts combat (clears aggro)
/// - Has 3-tick delay before can eat again
/// - Adds 3 ticks to attack delay
/// </summary>
public class Food : Item
{
    [Header("Food Properties")]
    [Tooltip("Hitpoints restored when eaten")]
    public int healAmount = 0;

    [Header("Visual")]
    [Tooltip("Inventory sprite for this food")]
    public Sprite inventorySprite;

    public Food()
    {
        defaultAction = "Eat";
        Weight = 0.226f; // ~0.5 lbs in kg (SDK line 29)
    }

    /// <summary>
    /// Food can be left-clicked to eat.
    /// SDK Reference: Food.hasInventoryLeftClick in Food.ts line 36
    /// </summary>
    public override bool HasInventoryLeftClick
    {
        get { return true; }
    }

    /// <summary>
    /// Left-click to eat food.
    /// SDK Reference: Food.inventoryLeftClick() in Food.ts lines 38-40
    /// </summary>
    public override void InventoryLeftClick(Player player)
    {
        // Eating system handles the actual consumption
        player.eats.EatFood(this);
    }

    /// <summary>
    /// Apply healing effect to player.
    /// Called by Eating.tickFood() when food is consumed.
    /// SDK Reference: Food.eat() in Food.ts lines 18-24
    /// 
    /// OSRS MECHANIC:
    /// - Can't overheal beyond max HP
    /// - Interrupts combat (handled by Eating.tickFood)
    /// </summary>
    public void Eat(Player player)
    {
        // Interrupt combat
        player.InterruptCombat();

        // Heal if below max HP
        if (player.currentStats.hitpoint < player.stats.hitpoint)
        {
            player.currentStats.hitpoint += healAmount;
            player.currentStats.hitpoint = Mathf.Min(
                player.currentStats.hitpoint, 
                player.stats.hitpoint
            );
        }

        Debug.Log($"[FOOD] Ate {itemName}, healed {healAmount} HP. Current: {player.currentStats.hitpoint}/{player.stats.hitpoint}");
    }
}
