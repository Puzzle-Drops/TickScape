using UnityEngine;

/// <summary>
/// Octbwan - Special food that can be "combo'd" with other food.
/// SDK Reference: Karambwan.ts (referenced in Eating.ts)
/// 
/// UNIQUE MECHANICS:
/// - Can be eaten on same tick as other food ("combo eating")
/// - Uses separate "comboDelay" timer
/// - Heals 18 HP
/// - Critical for high-level PvM (tick-perfect healing)
/// 
/// OSRS COMBO EATING:
/// Tick 0: Eat shark (22 HP) + Octbwan (18 HP) = 40 HP in one tick
/// Tick 1-2: Both food delays active
/// Tick 3: Can eat again
/// </summary>
public class Octbwan : Food
{
    public Octbwan()
    {
        healAmount = 18;
        Weight = 0.226f;
        defaultAction = "Eat";
        itemName = ItemName.OCTBWAN;
    }

    /// <summary>
    /// Octbwan uses combo eating logic.
    /// SDK Reference: Eating.eatComboFood() in Eating.ts lines 69-79
    /// </summary>
    public override void InventoryLeftClick(Player player)
    {
        // Use combo eating instead of regular eating
        player.eats.EatComboFood(this);
    }

    /// <summary>
    /// Apply healing effect (same as regular food).
    /// SDK Reference: Karambwan extends Food, uses Food.eat()
    /// </summary>
    public new void Eat(Player player)
    {
        // Same healing logic as regular food
        base.Eat(player);
    }
}
