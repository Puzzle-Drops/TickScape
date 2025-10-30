using UnityEngine;

/// <summary>
/// Cape/Back slot equipment (Infernal cape, Fire cape, etc.).
/// SDK Reference: Cape.ts
/// 
/// NOTE: SDK has a bug - uses EquipmentTypes.AMMO instead of BACK
/// We fix this in Unity by using correct CAPE slot
/// SDK Reference: Cape.ts line 6 (bug), Equipment.ts line 21 (correct enum)
/// </summary>
public class Cape : Equipment
{
    public Cape()
    {
        slot = EquipmentSlot.CAPE;
    }

    /// <summary>
    /// Assign cape to player's equipment.
    /// SDK Reference: Cape.assignToPlayer() in Cape.ts lines 9-11
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.cape = this;
    }

    /// <summary>
    /// Remove cape from player's equipment.
    /// SDK Reference: Cape.unassignToPlayer() in Cape.ts lines 12-14
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.cape = null;
    }

    /// <summary>
    /// Get currently equipped cape.
    /// SDK Reference: Cape.currentEquipment() in Cape.ts lines 16-18
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.cape;
    }
}
