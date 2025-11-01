using UnityEngine;

/// <summary>
/// Helmet/Head slot equipment (Slayer helm, Torva helm, etc.).
/// SDK Reference: Helmet.ts
/// </summary>
public class Helmet : Equipment
{
    public Helmet()
    {
        slot = EquipmentSlot.HELMET;
    }

    /// <summary>
    /// Assign helmet to player's equipment.
    /// SDK Reference: Helmet.assignToPlayer() in Helmet.ts lines 9-11
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.helmet = this;
    }

    /// <summary>
    /// Remove helmet from player's equipment.
    /// SDK Reference: Helmet.unassignToPlayer() in Helmet.ts lines 12-14
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.helmet = null;
    }

    /// <summary>
    /// Get currently equipped helmet.
    /// SDK Reference: Helmet.currentEquipment() in Helmet.ts lines 16-18
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.helmet;
    }
}
