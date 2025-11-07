using UnityEngine;

/// <summary>
/// Gloves/Hands slot equipment (Barrows gloves, Ferocious gloves, etc.).
/// SDK Reference: Gloves.ts
/// </summary>
public class Gloves : Equipment
{
    public Gloves()
    {
        slot = EquipmentSlot.GLOVES;
    }

    /// <summary>
    /// Assign gloves to player's equipment.
    /// SDK Reference: Gloves.assignToPlayer() in Gloves.ts lines 9-11
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.gloves = this;
    }

    /// <summary>
    /// Remove gloves from player's equipment.
    /// SDK Reference: Gloves.unassignToPlayer() in Gloves.ts lines 13-15
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.gloves = null;
    }

    /// <summary>
    /// Get currently equipped gloves.
    /// SDK Reference: Gloves.currentEquipment() in Gloves.ts lines 16-18
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.gloves;
    }
}
