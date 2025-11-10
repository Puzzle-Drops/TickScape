using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attack bonuses passed to weapon calculations.
/// SDK Reference: AttackBonuses interface in Weapon.ts
/// </summary>
public class AttackBonuses
{
    public int styleBonus = 0;
    public bool isAccurate = false;
    public int styleStrengthBonus = 0;
    public float voidMultiplier = 1.0f;
    public float gearMeleeMultiplier = 1.0f;
    public float gearMageMultiplier = 1.0f;
    public float gearRangeMultiplier = 1.0f;
    public string attackStyle = null;
    public float magicBaseSpellDamage = 0f;
    public float overallMultiplier = 1.0f;
    public EffectivePrayers effectivePrayers = null;
    public bool isSpecialAttack = false;
}

/// <summary>
/// Effective prayers for attack calculations.
/// SDK Reference: EffectivePrayers interface in Weapon.ts
/// </summary>
public class EffectivePrayers
{
    public Prayer magic;
    public Prayer range;
    public Prayer attack;
    public Prayer strength;
    public Prayer defence;
    public Prayer overhead;
}

/// <summary>
/// Base weapon class for all combat weapons.
/// SDK Reference: Weapon.ts
/// 
/// CRITICAL ATTACK FLOW:
/// 1. attack() - Main entry point
/// 2. _calculatePrayerEffects() - Get active prayers
/// 3. rollDamage() - Roll if attack hits and for how much
///    - _hitChance() - Calculate hit probability
///    - _attackRoll() vs _defenceRoll() - Accuracy check
///    - _maxHit() - Calculate damage if hit
/// 4. Apply modifiers (Justiciar, protection prayers, etc)
/// 5. grantXp() - Give experience
/// 6. registerProjectile() - Create projectile to deliver damage
/// </summary>
public abstract class Weapon : Equipment
{
    [Header("Weapon Properties")]
    [Tooltip("Attack range in tiles")]
    public int attackRange = 1;

    [Tooltip("Attack speed in ticks (4 = 2.4s)")]
    public int attackSpeed = 4;

    [Tooltip("Weapon type category")]
    public AttackStyleType weaponType = AttackStyleType.MELEE;

    [Header("Audio")]
    [Tooltip("Sound played when attacking")]
    public AudioClip attackSound;

    [Tooltip("Sound played when projectile launches")]
    public AudioClip projectileSound;

    [Tooltip("Sound played when attack lands")]
    public AudioClip attackLandingSound;

    [Header("Visual")]
    [Tooltip("Projectile prefab (for ranged/magic)")]
    public GameObject projectilePrefab;

    [Header("Two-Hander Settings")]
    [Tooltip("Is this weapon two-handed? (Godswords, bows, etc.)")]
    public bool isTwoHander = false;

    [Header("Special Attack")]
    [Tooltip("Does this weapon have a special attack?")]
    public bool hasSpecialAttack = false;

    [Tooltip("Special attack drain (0-100%)")]
    public int specialAttackDrain = 50;

    [Header("Ammo Compatibility (Ranged Weapons)")]
    [Tooltip("Compatible ammo types for ranged weapons")]
    public ItemName[] compatibleAmmo = new ItemName[0];

    [Header("Animation")]
    [Tooltip("Idle animation ID when this weapon is equipped")]
    public int idleAnimationId = 0; // 0 = default idle

    // Combat state
    protected int damageRoll;
    protected bool lastHitHit = false;

    // Projectile customization
    protected ProjectileOptions projectileOptions = new ProjectileOptions();

    public Weapon()
    {
        slot = EquipmentSlot.WEAPON;
        defaultAction = "Wield";
    }

    /// <summary>
/// Get compatible ammo for this weapon.
/// SDK Reference: Weapon.compatibleAmmo() in Weapon.ts line 58
/// 
/// For ranged weapons only:
/// - Twisted bow: Dragon arrows, Amethyst arrows
/// - Rune crossbow: Ruby bolts (e), Diamond bolts (e)
/// - Blowpipe: No ammo needed (uses scales)
/// </summary>
public ItemName[] GetCompatibleAmmo()
{
    return compatibleAmmo;
}

/// <summary>
/// Does this weapon have a special attack?
/// SDK Reference: Weapon.hasSpecialAttack() in Weapon.ts lines 75-77
/// </summary>
public bool HasSpecialAttack()
{
    return hasSpecialAttack;
}

/// <summary>
/// Get special attack drain amount.
/// SDK Reference: Weapon.specialAttackDrain() in Weapon.ts lines 78-80
/// </summary>
public int GetSpecialAttackDrain()
{
    return specialAttackDrain;
}

/// <summary>
/// Execute special attack.
/// Override in weapon subclasses to implement specific special attacks.
/// SDK Reference: Weapon.specialAttack() in Weapon.ts lines 82-84
/// 
/// EXAMPLES:
/// - Dragon dagger: 2x 4-hit attacks at 115% accuracy
/// - Armadyl godsword: 1 hit with 37.5% increased damage + drain stats
/// - Dragon claws: Complex 4-hit pattern
/// </summary>
public virtual void SpecialAttack(Unit from, Unit to, AttackBonuses bonuses = null, ProjectileOptions options = null)
{
    // Override in subclasses
    Debug.LogWarning($"[WEAPON] {itemName} has no special attack implementation!");
}

/// <summary>
/// Cast spell (for magic weapons).
/// SDK Reference: Weapon.cast() in Weapon.ts lines 86-88
/// 
/// Used for manual spell casting (Ice Barrage, Blood Barrage, etc.)
/// Override in magic weapon subclasses.
/// </summary>
public virtual void Cast(Unit from, Unit to)
{
    // Override in magic weapon subclasses
    Debug.LogWarning($"[WEAPON] {itemName} is not a magic weapon!");
}

/// <summary>
/// Get idle animation ID for this weapon.
/// SDK Reference: Weapon.idleAnimationId getter in Weapon.ts line 238
/// 
/// Different weapons have different idle stances:
/// - Godswords: Two-handed sword idle
/// - Daggers: One-handed dagger idle
/// - Bows: Bow idle
/// </summary>
public int GetIdleAnimationId()
{
    return idleAnimationId;
}

/// <summary>
/// Override inventory left-click to handle two-hander logic.
/// SDK Reference: Weapon.inventoryLeftClick() in Weapon.ts lines 90-121
/// 
/// COMPLEX LOGIC:
/// 1. Check if two-hander + offhand equipped → need 2 inventory slots
/// 2. Check if weapon already equipped → need 1 slot
/// 3. Equip weapon
/// 4. Unequip old weapon to inventory
/// 5. Unequip offhand if equipping two-hander
/// </summary>
public override void InventoryLeftClick(Player player)
{
    Weapon currentWeapon = player.equipment.weapon;
    Offhand currentOffhand = player.equipment.offhand as Offhand;

    // Get open inventory slots
    int[] openInventorySlots = player.GetOpenInventorySlots();

    // Find this weapon's current inventory position and add it to open slots
    int currentSlot = InventoryPosition(player);
    if (currentSlot >= 0)
    {
        int[] extendedSlots = new int[openInventorySlots.Length + 1];
        extendedSlots[0] = currentSlot;
        System.Array.Copy(openInventorySlots, 0, extendedSlots, 1, openInventorySlots.Length);
        openInventorySlots = extendedSlots;
    }

    // Calculate needed inventory slots
    int neededInventorySlots = 0;

    // Need slot for current weapon
    if (currentWeapon != null)
    {
        neededInventorySlots++;
    }

    // Need slot for offhand if equipping two-hander
    if (isTwoHander && currentOffhand != null)
    {
        neededInventorySlots++;
    }

    // Subtract slot for current weapon (if this IS the current weapon)
    if (currentWeapon == this)
    {
        neededInventorySlots--;
    }

    // Check if enough space
    if (neededInventorySlots > openInventorySlots.Length)
    {
        Debug.LogWarning($"[WEAPON] Not enough inventory space! Need {neededInventorySlots} slots, have {openInventorySlots.Length}");
        return;
    }

    // Equip the weapon
    AssignToPlayer(player);

    // Return old weapon to inventory
    if (currentWeapon != null && currentWeapon != this)
    {
        player.inventory[openInventorySlots[0]] = currentWeapon;
        openInventorySlots = player.GetOpenInventorySlots(); // Refresh
    }
    else
    {
        // No weapon equipped or re-equipping same weapon, clear the slot
        player.inventory[openInventorySlots[0]] = null;
        openInventorySlots = player.GetOpenInventorySlots(); // Refresh
    }

    // Unequip offhand if two-hander
    if (isTwoHander && currentOffhand != null)
    {
        player.inventory[openInventorySlots[0]] = currentOffhand;
        player.equipment.offhand = null;
    }

    // Notify equipment changed
    player.EquipmentChanged();
}

    /// <summary>
    /// Get current attack style for this weapon.
    /// SDK Reference: Weapon.attackStyle() in Weapon.ts
    /// </summary>
    public AttackStyle GetAttackStyle()
    {
        return AttackStylesController.Instance.GetAttackStyle(weaponType);
    }

    /// <summary>
    /// Main attack method. Returns true if attack was performed.
    /// SDK Reference: Weapon.attack() in Weapon.ts lines 95-134
    /// 
    /// ATTACK SEQUENCE:
    /// 1. Calculate prayer effects
    /// 2. Set default bonuses
    /// 3. Roll damage (hit chance + damage amount)
    /// 4. Apply set effect modifiers (e.g., Justiciar)
    /// 5. Check protection prayers
    /// 6. Apply recoil (Ring of Suffering)
    /// 7. Grant XP to attacker
    /// 8. Register projectile for damage delivery
    /// </summary>
    public virtual bool Attack(Unit from, Unit to, AttackBonuses bonuses = null, ProjectileOptions options = null)
    {
        if (bonuses == null)
            bonuses = new AttackBonuses();

        if (options == null)
            options = new ProjectileOptions();

        // Calculate prayer effects
        CalculatePrayerEffects(from, to, bonuses);

        // Set default bonus values
        bonuses.styleBonus = bonuses.styleBonus != 0 ? bonuses.styleBonus : 0;
        bonuses.voidMultiplier = bonuses.voidMultiplier != 1.0f ? bonuses.voidMultiplier : 1.0f;
        bonuses.gearMeleeMultiplier = bonuses.gearMeleeMultiplier != 1.0f ? bonuses.gearMeleeMultiplier : 1.0f;
        bonuses.gearMageMultiplier = bonuses.gearMageMultiplier != 1.0f ? bonuses.gearMageMultiplier : 1.0f;
        bonuses.gearRangeMultiplier = bonuses.gearRangeMultiplier != 1.0f ? bonuses.gearRangeMultiplier : 1.0f;
        bonuses.overallMultiplier = bonuses.overallMultiplier != 1.0f ? bonuses.overallMultiplier : 1.0f;

        // Roll damage
        RollDamage(from, to, bonuses);

        // Return false if damage is invalid
        if (damageRoll == -1)
        {
            return false;
        }

        // TODO: Apply set effects (Justiciar, etc.)
        // SDK Reference: Weapon.ts lines 112-120

        // Check protection prayers with configurable block percentage
        // SDK Reference: Weapon.ts lines 121-123 (modified for percentage)
        if (IsBlockable(from, to, bonuses.attackStyle))
        {
            float blockPercentage = GetBlockPercentage(from, to, bonuses);
            int blockedDamage = Mathf.FloorToInt(damageRoll * blockPercentage);
            damageRoll = damageRoll - blockedDamage;

            // TODO: Future hitsplat system - use different color/type for blocked hits
            // if (blockPercentage >= 1.0f) use blue hitsplat (fully blocked)
            // else use purple/different hitsplat (partially blocked)
        }

        // Sanitize damage output
        damageRoll = Mathf.FloorToInt(Mathf.Max(0, Mathf.Min(to.currentStats.hitpoint, damageRoll, 100)));

        // TODO: Ring of Suffering recoil
        // SDK Reference: Weapon.ts lines 125-129

        // Grant XP
        GrantXp(from, to);

        // Register projectile
        RegisterProjectile(from, to, bonuses, options);

        return true;
    }

    /// <summary>
    /// Get prayer block percentage for this attack.
    /// Default: 100% block (1.0f) matching OSRS PvM mechanics.
    /// Override in specific weapons/attacks for partial blocking.
    /// 
    /// EXAMPLES:
    /// - Return 1.0f = 100% damage blocked (default OSRS)
    /// - Return 0.5f = 50% damage blocked (Verzik P1)
    /// - Return 0.0f = No damage blocked (prayer doesn't work)
    /// 
    /// SDK Reference: Modified from binary blocking in Weapon.ts line 122
    /// </summary>
    protected virtual float GetBlockPercentage(Unit from, Unit to, AttackBonuses bonuses)
    {
        // Default OSRS PvM behavior: 100% protection
        return 1.0f;
    }

    /// <summary>
    /// Roll damage for this attack.
    /// SDK Reference: Weapon.rollDamage() in Weapon.ts
    /// </summary>
    public void RollDamage(Unit from, Unit to, AttackBonuses bonuses)
    {
        damageRoll = Mathf.FloorToInt(RollAttackInternal(from, to, bonuses));
    }

    /// <summary>
    /// Internal attack roll logic.
    /// SDK Reference: Weapon._rollAttack() in Weapon.ts
    /// </summary>
    protected float RollAttackInternal(Unit from, Unit to, AttackBonuses bonuses)
    {
        lastHitHit = false;
        float hitChance = CalculateHitChance(from, to, bonuses);

        if (RandomHelper.Get() > hitChance)
        {
            return 0; // Miss
        }

        return CalculateHitDamage(from, to, bonuses);
    }

    /// <summary>
    /// Calculate damage if attack hits.
    /// SDK Reference: Weapon._calculateHitDamage() in Weapon.ts
    /// </summary>
    protected float CalculateHitDamage(Unit from, Unit to, AttackBonuses bonuses)
    {
        lastHitHit = true;
        float maxHit = CalculateMaxHit(from, to, bonuses);
        return Mathf.Floor(RandomHelper.Get() * (maxHit + 1));
    }

    /// <summary>
    /// Calculate hit chance (0.0 to 1.0).
    /// SDK Reference: Weapon._hitChance() in Weapon.ts lines 154-160
    /// 
    /// OSRS Hit Chance Formula:
    /// If attackRoll > defenceRoll:
    ///     hitChance = 1 - (defenceRoll + 2) / (2 * attackRoll + 1)
    /// Else:
    ///     hitChance = attackRoll / (2 * defenceRoll + 1)
    /// </summary>
    protected float CalculateHitChance(Unit from, Unit to, AttackBonuses bonuses)
    {
        float attackRoll = CalculateAttackRoll(from, to, bonuses);
        float defenceRoll = CalculateDefenceRoll(from, to, bonuses);

        float hitChance;
        if (attackRoll > defenceRoll)
        {
            hitChance = 1f - (defenceRoll + 2f) / (2f * attackRoll + 1f);
        }
        else
        {
            hitChance = attackRoll / (2f * defenceRoll + 1f);
        }

        return hitChance;
    }

    /// <summary>
    /// Calculate attack roll. Override in subclasses.
    /// SDK Reference: Weapon._attackRoll() in Weapon.ts
    /// </summary>
    protected abstract float CalculateAttackRoll(Unit from, Unit to, AttackBonuses bonuses);

    /// <summary>
    /// Calculate defence roll. Override in subclasses.
    /// SDK Reference: Weapon._defenceRoll() in Weapon.ts
    /// </summary>
    protected abstract float CalculateDefenceRoll(Unit from, Unit to, AttackBonuses bonuses);

    /// <summary>
    /// Calculate max hit. Override in subclasses.
    /// SDK Reference: Weapon._maxHit() in Weapon.ts
    /// </summary>
    protected abstract float CalculateMaxHit(Unit from, Unit to, AttackBonuses bonuses);

    /// <summary>
    /// Calculate prayer effects for this attack. Override in subclasses.
    /// SDK Reference: Weapon._calculatePrayerEffects() in Weapon.ts
    /// </summary>
    protected abstract void CalculatePrayerEffects(Unit from, Unit to, AttackBonuses bonuses);

    /// <summary>
    /// Check if this attack can be blocked by protection prayers.
    /// SDK Reference: Weapon.isBlockable() in Weapon.ts
    /// </summary>
    public abstract bool IsBlockable(Unit from, Unit to, string attackStyle);

    /// <summary>
    /// Calculate hit delay based on distance.
    /// Override in subclasses.
    /// SDK Reference: Weapon.calculateHitDelay() in Weapon.ts
    /// </summary>
    public virtual int CalculateHitDelay(int distance)
    {
        return 999; // Override in subclasses
    }

    /// <summary>
    /// Grant experience to attacker.
    /// SDK Reference: Weapon.grantXp() in Weapon.ts lines 167-175
    /// </summary>
    protected void GrantXp(Unit from, Unit to)
    {
        if (from is Player && damageRoll > 0)
        {
            AttackStyle style = GetAttackStyle();
            List<XpDrop> xpDrops = AttackStylesController.Instance.GetXpDrops(
                style, weaponType, damageRoll, to.xpBonusMultiplier);

            foreach (XpDrop drop in xpDrops)
            {
                from.GrantXp(drop);
            }
        }
    }

    /// <summary>
    /// Register projectile to deliver damage.
    /// SDK Reference: Weapon.registerProjectile() in Weapon.ts lines 177-186
    /// </summary>
    protected virtual void RegisterProjectile(Unit from, Unit to, AttackBonuses bonuses, ProjectileOptions options)
    {
        // Merge weapon's default options with passed options
        ProjectileOptions finalOptions = new ProjectileOptions
        {
            hidden = options.hidden != false ? options.hidden : projectileOptions.hidden,
            attackSound = attackSound,
            projectileSound = projectileSound,
            hitSound = attackLandingSound,
            modelPrefab = projectilePrefab,
            // Copy other options...
        };

        // Create projectile
        Projectile projectile = new Projectile(
            this, damageRoll, from, to, bonuses.attackStyle, finalOptions);

        // CRITICAL FIX: Actually add the projectile to the target!
        to.AddProjectile(projectile);

        Debug.Log($"[COMBAT] Projectile created: {damageRoll} damage, {projectile.totalDelay} tick delay");
    }

    /// <summary>
    /// Check if attack style is melee.
    /// SDK Reference: Weapon.isMeleeAttackStyle() in Weapon.ts
    /// </summary>
    public static bool IsMeleeAttackStyle(string style)
    {
        return style == "crush" || style == "slash" || style == "stab";
    }
}

