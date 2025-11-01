using UnityEngine;

/// <summary>
/// Feet/Boots slot equipment (Primordial boots, Pegasian boots, etc.).
/// SDK Reference: Feet.ts
/// </summary>
public class Feet : Equipment
{
    public Feet()
    {
        slot = EquipmentSlot.FEET;
    }

    /// <summary>
    /// Assign boots to player's equipment.
    /// SDK Reference: Feet.assignToPlayer() in Feet.ts lines 9-11
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.feet = this;
    }

    /// <summary>
    /// Remove boots from player's equipment.
    /// SDK Reference: Feet.unassignToPlayer() in Feet.ts lines 12-14
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.feet = null;
    }

    /// <summary>
    /// Get currently equipped boots.
    /// SDK Reference: Feet.currentEquipment() in Feet.ts lines 16-18
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.feet;
    }
}
