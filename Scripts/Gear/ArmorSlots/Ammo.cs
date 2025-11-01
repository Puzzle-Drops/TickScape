using UnityEngine;

/// <summary>
/// Ammunition slot equipment (arrows, bolts, blessings).
/// SDK Reference: Ammo.ts
/// 
/// IMPORTANT DISTINCTION:
/// - BLESSING type: Never consumed, provides stat bonuses (Holy blessing)
/// - AMMO type: Consumed on ranged attacks (Dragon arrows, Diamond bolts)
/// </summary>
public class Ammo : Equipment
{
    [Header("Ammo Properties")]
    [Tooltip("Type of ammo (BLESSING or AMMO)")]
    public AmmoType ammoType = AmmoType.AMMO;
    
    [Tooltip("Stack size for consumable ammo")]
    public int quantity = 1;

    public Ammo()
    {
        slot = EquipmentSlot.AMMO;
    }

    /// <summary>
    /// Get ammo type classification.
    /// SDK Reference: Ammo.ammoType() in Ammo.ts line 12
    /// </summary>
    public AmmoType GetAmmoType()
    {
        return ammoType;
    }

    /// <summary>
    /// Assign ammo to player's equipment.
    /// SDK Reference: Ammo.assignToPlayer() in Ammo.ts lines 15-17
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.ammo = this;
    }

    /// <summary>
    /// Remove ammo from player's equipment.
    /// SDK Reference: Ammo.unassignToPlayer() in Ammo.ts lines 19-21
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.ammo = null;
    }

    /// <summary>
    /// Get currently equipped ammo.
    /// SDK Reference: Ammo.currentEquipment() in Ammo.ts lines 23-25
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.ammo;
    }

    /// <summary>
    /// Consume one unit of ammo (if consumable type).
    /// Called by ranged weapons on attack.
    /// SDK Reference: Implied by AMMO vs BLESSING distinction
    /// </summary>
    public void ConsumeAmmo(Player player)
    {
        // Blessings are never consumed
        if (ammoType == AmmoType.BLESSING)
        {
            return;
        }

        quantity--;
        
        if (quantity <= 0)
        {
            // Remove from equipment and inventory
            UnassignFromPlayer(player);
            ConsumeItem(player);
        }
    }
}
