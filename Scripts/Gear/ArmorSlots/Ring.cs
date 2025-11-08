using UnityEngine;

/// <summary>
/// Ring slot equipment (Ring of suffering, Ultor ring, etc.).
/// SDK Reference: Ring.ts
/// </summary>
public class Ring : Equipment
{
    public Ring()
    {
        slot = EquipmentSlot.RING;
    }

    /// <summary>
    /// Assign ring to player's equipment.
    /// SDK Reference: Ring.assignToPlayer() in Ring.ts lines 9-11
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.ring = this;
    }

    /// <summary>
    /// Remove ring from player's equipment.
    /// SDK Reference: Ring.unassignToPlayer() in Ring.ts lines 12-14
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.ring = null;
    }

    /// <summary>
    /// Get currently equipped ring.
    /// SDK Reference: Ring.currentEquipment() in Ring.ts lines 16-18
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.ring;
    }
}
