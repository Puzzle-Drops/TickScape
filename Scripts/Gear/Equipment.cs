using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Equipment slot types.
/// SDK Reference: EquipmentTypes enum in Equipment.ts
/// </summary>
public enum EquipmentSlot
{
    WEAPON,
    OFFHAND,
    HELMET,
    NECKLACE,
    CHEST,
    LEGS,
    FEET,
    GLOVES,
    RING,
    CAPE,
    AMMO
}

/// <summary>
/// Base class for all equippable items.
/// SDK Reference: Equipment.ts
/// </summary>
public class Equipment : Item
{
    [Header("Equipment Properties")]
    public EquipmentSlot slot;
    public UnitBonuses bonuses;
    public AudioClip equipSound;

    [Header("Set Effects")]
    [Tooltip("Set effect this item belongs to (if any)")]
    public SetEffect setEffect = null;

    [Header("3D Model")]
    [Tooltip("Model name/prefab for visual equipment")]
    public GameObject modelPrefab = null;

    [Tooltip("Attack animation override (for weapons)")]
    public int attackAnimationId = -1; // -1 = use default

    public Equipment()
    {
        defaultAction = "Equip";
        bonuses = UnitBonuses.Empty();
    }

    public override bool HasInventoryLeftClick
    {
        get { return true; }
    }

    /// <summary>
    /// Equip this item. Swaps with currently equipped item.
    /// SDK Reference: Equipment.inventoryLeftClick() in Equipment.ts
    /// </summary>
    public override void InventoryLeftClick(Player player)
    {
        Equipment currentItem = GetCurrentEquipment(player);
        int[] openSlots = player.GetOpenInventorySlots();

        // Find current slot in inventory
        int currentSlot = InventoryPosition(player);
        if (currentSlot >= 0)
        {
            // Remove from inventory temporarily
            player.inventory[currentSlot] = null;
        }

        // Equip this item
        AssignToPlayer(player);

        // Put old item back in inventory (if there was one)
        if (currentItem != null && openSlots.Length > 0)
        {
            player.inventory[openSlots[0]] = currentItem;
        }
        else if (currentSlot >= 0)
        {
            // Put in original slot if nothing was equipped
            player.inventory[currentSlot] = null;
        }

        // Notify player equipment changed
        player.EquipmentChanged();

        // Play equip sound if available
        if (equipSound != null)
        {
            AudioSource.PlayClipAtPoint(equipSound, player.transform.position, 0.5f);
        }
    }

    /// <summary>
    /// Unequip this item to inventory.
    /// SDK Reference: Equipment.unequip()
    /// </summary>
    public void Unequip(Player player)
    {
        int[] openSlots = player.GetOpenInventorySlots();
        if (openSlots.Length == 0)
        {
            Debug.LogWarning("Cannot unequip - inventory full!");
            return;
        }

        UnassignFromPlayer(player);
        player.inventory[openSlots[0]] = this;
        player.EquipmentChanged();
    }

    /// <summary>
    /// Get currently equipped item in this slot.
    /// Override in subclasses.
    /// SDK Reference: Equipment.currentEquipment()
    /// </summary>
    public virtual Equipment GetCurrentEquipment(Player player)
    {
        switch (slot)
        {
            case EquipmentSlot.WEAPON: return player.equipment.weapon;
            case EquipmentSlot.OFFHAND: return player.equipment.offhand;
            case EquipmentSlot.HELMET: return player.equipment.helmet;
            case EquipmentSlot.NECKLACE: return player.equipment.necklace;
            case EquipmentSlot.CHEST: return player.equipment.chest;
            case EquipmentSlot.LEGS: return player.equipment.legs;
            case EquipmentSlot.FEET: return player.equipment.feet;
            case EquipmentSlot.GLOVES: return player.equipment.gloves;
            case EquipmentSlot.RING: return player.equipment.ring;
            case EquipmentSlot.CAPE: return player.equipment.cape;
            case EquipmentSlot.AMMO: return player.equipment.ammo;
            default: return null;
        }
    }

    /// <summary>
    /// Assign this equipment to player.
    /// Override in subclasses.
    /// SDK Reference: Equipment.assignToPlayer()
    /// </summary>
    public virtual void AssignToPlayer(Player player)
    {
        switch (slot)
        {
            case EquipmentSlot.WEAPON: player.equipment.weapon = this as Weapon; break;
            case EquipmentSlot.HELMET: player.equipment.helmet = this; break;
            case EquipmentSlot.NECKLACE: player.equipment.necklace = this; break;
            case EquipmentSlot.CHEST: player.equipment.chest = this; break;
            case EquipmentSlot.LEGS: player.equipment.legs = this; break;
            case EquipmentSlot.FEET: player.equipment.feet = this; break;
            case EquipmentSlot.GLOVES: player.equipment.gloves = this; break;
            case EquipmentSlot.RING: player.equipment.ring = this; break;
            case EquipmentSlot.CAPE: player.equipment.cape = this; break;
            case EquipmentSlot.AMMO: player.equipment.ammo = this; break;
        }
    }

    /// <summary>
    /// Unassign this equipment from player.
    /// SDK Reference: Equipment.unassignToPlayer()
    /// </summary>
    public virtual void UnassignFromPlayer(Player player)
    {
        switch (slot)
        {
            case EquipmentSlot.WEAPON: player.equipment.weapon = null; break;
            case EquipmentSlot.HELMET: player.equipment.helmet = null; break;
            case EquipmentSlot.NECKLACE: player.equipment.necklace = null; break;
            case EquipmentSlot.CHEST: player.equipment.chest = null; break;
            case EquipmentSlot.LEGS: player.equipment.legs = null; break;
            case EquipmentSlot.FEET: player.equipment.feet = null; break;
            case EquipmentSlot.GLOVES: player.equipment.gloves = null; break;
            case EquipmentSlot.RING: player.equipment.ring = null; break;
            case EquipmentSlot.CAPE: player.equipment.cape = null; break;
            case EquipmentSlot.AMMO: player.equipment.ammo = null; break;
        }
    }

    /// <summary>
    /// Set stat bonuses for this equipment.
    /// Override in subclasses.
    /// SDK Reference: Equipment.setStats()
    /// </summary>
    public virtual void SetStats()
    {
        // Override in subclasses
    }

    /// <summary>
    /// Get set effect for this equipment piece.
    /// SDK Reference: Equipment.equipmentSetEffect getter in Equipment.ts line 80
    /// 
    /// Returns set effect class if this item is part of a set.
    /// Used by Player.equipmentChanged() to check for complete sets.
    /// </summary>
    public SetEffect GetEquipmentSetEffect()
    {
        return setEffect;
    }

    /// <summary>
    /// Update bonuses based on other equipped items.
    /// SDK Reference: Equipment.updateBonuses() in Equipment.ts line 94
    /// 
    /// EXAMPLES OF SYNERGISTIC BONUSES:
    /// - Crystal armor pieces boost Bow of Faerdhinen accuracy/damage
    /// - Elite void pieces boost weapon accuracy based on helmet worn
    /// - Amulet of the damned boosts Barrows set effects
    /// 
    /// This is called during Player.equipmentChanged() BEFORE calculating
    /// final bonuses, allowing items to modify each other's stats.
    /// </summary>
    public virtual void UpdateBonuses(List<Equipment> allEquippedGear)
    {
        // Override in subclasses for synergistic bonuses
        // Example: Crystal Body checks if Crystal Legs + Bowfa equipped
    }

    /// <summary>
    /// Get 3D model for this equipment piece.
    /// SDK Reference: Equipment.model getter in Equipment.ts line 104
    /// 
    /// Returns model name/prefab for visual rendering.
    /// Used by rendering system to show equipped items on player.
    /// </summary>
    public GameObject GetModel()
    {
        return modelPrefab;
    }

    /// <summary>
    /// Get attack animation ID override.
    /// SDK Reference: Equipment.attackAnimationId getter in Equipment.ts line 111
    /// 
    /// Returns -1 if using weapon's default animation.
    /// Used by weapons to override attack animations (e.g., special attacks).
    /// </summary>
    public int GetAttackAnimationId()
    {
        return attackAnimationId;
    }
}
