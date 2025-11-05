using UnityEngine;

/// <summary>
/// Necklace/Neck slot equipment (Amulet of torture, Occult necklace, etc.).
/// SDK Reference: Necklace.ts
/// </summary>
public class Necklace : Equipment
{
    public Necklace()
    {
        slot = EquipmentSlot.NECKLACE;
    }

    /// <summary>
    /// Assign necklace to player's equipment.
    /// SDK Reference: Necklace.assignToPlayer() in Necklace.ts lines 9-11
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.necklace = this;
    }

    /// <summary>
    /// Remove necklace from player's equipment.
    /// SDK Reference: Necklace.unassignToPlayer() in Necklace.ts lines 13-15
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.necklace = null;
    }

    /// <summary>
    /// Get currently equipped necklace.
    /// SDK Reference: Necklace.currentEquipment() in Necklace.ts lines 16-18
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.necklace;
    }
}
