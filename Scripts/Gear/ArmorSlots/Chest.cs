using UnityEngine;

/// <summary>
/// Chest/Body slot equipment (Bandos chestplate, Armadyl chestplate, etc.).
/// SDK Reference: Chest.ts
/// </summary>
public class Chest : Equipment
{
    public Chest()
    {
        slot = EquipmentSlot.CHEST;
    }

    /// <summary>
    /// Assign chest armor to player's equipment.
    /// SDK Reference: Chest.assignToPlayer() in Chest.ts lines 7-9
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.chest = this;
    }

    /// <summary>
    /// Remove chest armor from player's equipment.
    /// SDK Reference: Chest.unassignToPlayer() in Chest.ts lines 11-13
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.chest = null;
    }

    /// <summary>
    /// Get currently equipped chest armor.
    /// SDK Reference: Chest.currentEquipment() in Chest.ts lines 14-16
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.chest;
    }
}
