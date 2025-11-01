using UnityEngine;

/// <summary>
/// Legs slot equipment (Bandos tassets, Torva platelegs, etc.).
/// SDK Reference: Legs.ts
/// </summary>
public class Legs : Equipment
{
    public Legs()
    {
        slot = EquipmentSlot.LEGS;
    }

    /// <summary>
    /// Assign leg armor to player's equipment.
    /// SDK Reference: Legs.assignToPlayer() in Legs.ts lines 9-11
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.legs = this;
    }

    /// <summary>
    /// Remove leg armor from player's equipment.
    /// SDK Reference: Legs.unassignToPlayer() in Legs.ts lines 13-15
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.legs = null;
    }

    /// <summary>
    /// Get currently equipped leg armor.
    /// SDK Reference: Legs.currentEquipment() in Legs.ts lines 16-18
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.legs;
    }
}
