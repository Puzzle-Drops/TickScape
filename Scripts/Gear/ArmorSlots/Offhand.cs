using UnityEngine;

/// <summary>
/// Offhand/Shield slot equipment (Defenders, shields, off-hand weapons).
/// SDK Reference: Offhand.ts
/// 
/// CRITICAL COMPLEXITY:
/// Equipping a shield/offhand has special logic:
/// 1. If weapon is two-handed → unequip weapon first
/// 2. Requires 2 inventory slots if unequipping both weapon + offhand
/// 3. Must check inventory space BEFORE equipping
/// 
/// This is one of the most complex equipment interactions in OSRS.
/// SDK Reference: Offhand.ts lines 10-37 for full logic
/// </summary>
public class Offhand : Equipment
{
    public Offhand()
    {
        slot = EquipmentSlot.OFFHAND;
    }

    /// <summary>
    /// Complex equip logic for shields.
    /// Handles two-handed weapon unequipping.
    /// SDK Reference: Offhand.inventoryLeftClick() in Offhand.ts lines 10-37
    /// 
    /// LOGIC FLOW:
    /// 1. Get current weapon and offhand
    /// 2. Calculate needed inventory slots:
    ///    - Need slot for current offhand (if exists)
    ///    - Need slot for two-handed weapon (if exists)
    /// 3. Check if enough space
    /// 4. Equip shield
    /// 5. Unequip items into inventory
    /// 6. Notify equipment changed
    /// </summary>
    public override void InventoryLeftClick(Player player)
    {
        Weapon currentWeapon = player.equipment.weapon;
        Offhand currentOffhand = player.equipment.offhand as Offhand;

        // Get open inventory slots
        int[] openInventorySlots = player.GetOpenInventorySlots();
        
        // Find this item's current inventory position and add it to open slots
        int currentSlot = InventoryPosition(player);
        if (currentSlot >= 0)
        {
            // This slot will be freed when we equip
            int[] extendedSlots = new int[openInventorySlots.Length + 1];
            extendedSlots[0] = currentSlot;
            System.Array.Copy(openInventorySlots, 0, extendedSlots, 1, openInventorySlots.Length);
            openInventorySlots = extendedSlots;
        }

        // Calculate how many inventory slots we need
        int neededInventorySlots = 0;

        // Need slot for current offhand
        if (currentOffhand != null)
        {
            neededInventorySlots++;
        }

        // Need slot for two-handed weapon
        if (currentWeapon != null && currentWeapon.isTwoHander)
        {
            neededInventorySlots++;
        }

        // Check if we have enough space
        if (neededInventorySlots > openInventorySlots.Length)
        {
            Debug.LogWarning("[OFFHAND] Not enough inventory space! Need " + neededInventorySlots + " slots, have " + openInventorySlots.Length);
            return;
        }

        // Equip the shield
        AssignToPlayer(player);

        // Return current offhand to inventory
        if (currentOffhand != null)
        {
            player.inventory[openInventorySlots[0]] = currentOffhand;
            openInventorySlots = player.GetOpenInventorySlots(); // Refresh open slots
        }
        else
        {
            // No offhand equipped, clear the slot we took this from
            player.inventory[openInventorySlots[0]] = null;
            openInventorySlots = player.GetOpenInventorySlots(); // Refresh open slots
        }

        // Unequip two-handed weapon if equipped
        if (currentWeapon != null && currentWeapon.isTwoHander)
        {
            player.inventory[openInventorySlots[0]] = currentWeapon;
            player.equipment.weapon = null;
        }

        // Notify equipment changed
        player.EquipmentChanged();
    }

    /// <summary>
    /// Assign offhand to player's equipment.
    /// SDK Reference: Offhand.assignToPlayer() in Offhand.ts lines 39-41
    /// </summary>
    public override void AssignToPlayer(Player player)
    {
        player.equipment.offhand = this;
    }

    /// <summary>
    /// Remove offhand from player's equipment.
    /// SDK Reference: Offhand.unassignToPlayer() in Offhand.ts lines 42-44
    /// </summary>
    public override void UnassignFromPlayer(Player player)
    {
        player.equipment.offhand = null;
    }

    /// <summary>
    /// Get currently equipped offhand.
    /// SDK Reference: Offhand.currentEquipment() in Offhand.ts lines 46-48
    /// </summary>
    public override Equipment GetCurrentEquipment(Player player)
    {
        return player.equipment.offhand;
    }
}
