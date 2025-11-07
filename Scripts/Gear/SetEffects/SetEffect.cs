using System.Collections.Generic;

/// <summary>
/// Base class for equipment set effects.
/// SDK Reference: SetEffect.ts
/// 
/// SET EFFECT SYSTEM:
/// - Equipment pieces reference a SetEffect class
/// - Player.equipmentChanged() checks which sets are complete
/// - Complete sets are stored in player.setEffects
/// - Combat calculations check setEffects for damage modifiers
/// 
/// EXAMPLE SETS:
/// - Justiciar (3pc): Reduces incoming damage
/// - Void Knight (4pc): Increases accuracy/damage
/// - Crystal armor (3pc): Boosts Bow of Faerdhinen
/// - Dharok's (4pc): Damage increases as HP decreases
/// </summary>
public abstract class SetEffect
{
    /// <summary>
    /// Get the name/type of this set effect.
    /// SDK Reference: SetEffect.effectName() in SetEffect.ts line 5
    /// </summary>
    public abstract SetEffectType GetEffectName();

    /// <summary>
    /// Get list of items required for this set.
    /// SDK Reference: SetEffect.itemsInSet() in SetEffect.ts line 8
    /// 
    /// Returns item names that must ALL be equipped for set bonus.
    /// Example: Justiciar = [JUSTICIAR_FACEGUARD, JUSTICIAR_CHESTGUARD, JUSTICIAR_LEGGUARDS]
    /// </summary>
    public abstract List<ItemName> GetItemsInSet();

    /// <summary>
    /// Check if player has complete set equipped.
    /// Called by Player.equipmentChanged() to determine active sets.
    /// SDK Reference: Player.equipmentChanged() in Player.ts lines 211-234
    /// </summary>
    public bool IsComplete(List<Equipment> equippedGear)
    {
        List<ItemName> requiredItems = GetItemsInSet();
        int foundItems = 0;

        foreach (ItemName requiredItem in requiredItems)
        {
            foreach (Equipment equipment in equippedGear)
            {
                if (equipment != null && equipment.itemName == requiredItem)
                {
                    foundItems++;
                    break;
                }
            }
        }

        return foundItems == requiredItems.Count;
    }

    /// <summary>
    /// Apply set effect to incoming damage.
    /// Override in subclasses for specific damage calculations.
    /// SDK Reference: Combat damage calculation in Weapon.attack() lines 112-120
    /// </summary>
    public virtual int ModifyIncomingDamage(int damage, Unit attacker, Unit defender, string attackStyle)
    {
        return damage; // Override in subclasses
    }
}
