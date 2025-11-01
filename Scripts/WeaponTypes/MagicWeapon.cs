using UnityEngine;

/// <summary>
/// Magic weapon implementation.
/// SDK Reference: MagicWeapon.ts
/// 
/// MAGIC COMBAT FORMULAS:
/// - Attack Roll = magicLevel * (equipmentBonus + 64) * gearMultiplier
/// - Defence Roll = (magic + 9) * (defenceBonus + 64)
/// - Max Hit = floor(baseSpellDamage * magicDamageMultiplier)
/// </summary>
public class MagicWeapon : Weapon
{
    [Header("Magic Properties")]
    [Tooltip("Base spell damage (e.g., 30 for Ice Barrage)")]
    public int baseSpellDamage = 20;

    public MagicWeapon()
    {
        weaponType = AttackStyleType.MAGIC;
        attackRange = 10;
        attackSpeed = 5;

        // Magic projectiles are visible
        projectileOptions.hidden = false;
    }

    public override bool Attack(Unit from, Unit to, AttackBonuses bonuses = null, ProjectileOptions options = null)
    {
        if (bonuses == null)
            bonuses = new AttackBonuses();

        // Set base spell damage
        bonuses.magicBaseSpellDamage = baseSpellDamage;

        return base.Attack(from, to, bonuses, options);
    }

    /// <summary>
    /// Calculate hit delay for magic weapons.
    /// SDK Reference: MagicWeapon.calculateHitDelay() in MagicWeapon.ts
    /// 
    /// Formula: floor((1 + distance) / 3) + 1
    /// </summary>
    public override int CalculateHitDelay(int distance)
    {
        return Mathf.FloorToInt((1 + distance) / 3f) + 1;
    }

    /// <summary>
    /// Check if attack is blockable by Protect from Magic.
    /// SDK Reference: MagicWeapon.isBlockable() in MagicWeapon.ts lines 27-35
    /// </summary>
    public override bool IsBlockable(Unit from, Unit to, string attackStyle)
    {
        // Create bonuses object for prayer calculation
        AttackBonuses bonuses = new AttackBonuses { attackStyle = attackStyle };
        CalculatePrayerEffects(from, to, bonuses);

        if (bonuses.effectivePrayers?.overhead != null &&
            bonuses.effectivePrayers.overhead.GetFeature() == "magic")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate prayer effects for magic combat.
    /// SDK Reference: MagicWeapon._calculatePrayerEffects() in MagicWeapon.ts lines 37-54
    /// </summary>
    protected override void CalculatePrayerEffects(Unit from, Unit to, AttackBonuses bonuses)
    {
        bonuses.effectivePrayers = new EffectivePrayers();

        // Attacker's offensive prayers
        if (from is Player && from.prayerController != null)
        {
            Prayer offensiveMagic = from.prayerController.MatchGroup(PrayerGroup.ACCURACY);
            if (offensiveMagic != null)
            {
                bonuses.effectivePrayers.magic = offensiveMagic;
            }
        }

        // Defender's prayers
        if (to is Player && to.prayerController != null)
        {
            Prayer defence = to.prayerController.MatchGroup(PrayerGroup.DEFENCE);
            if (defence != null)
            {
                bonuses.effectivePrayers.defence = defence;
            }

            Prayer overhead = to.prayerController.GetOverhead();
            if (overhead != null)
            {
                bonuses.effectivePrayers.overhead = overhead;
            }
        }
    }

    /// <summary>
    /// Calculate magic level with prayers.
    /// SDK Reference: MagicWeapon._magicLevel() in MagicWeapon.ts lines 56-72
    /// 
    /// Prayer Multipliers:
    /// - No prayer: 1.0
    /// - Augury: 1.25
    /// </summary>
    protected float CalculateMagicLevel(Unit from, Unit to, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        Prayer magicPrayer = bonuses.effectivePrayers.magic;

        if (magicPrayer != null)
        {
            if (magicPrayer.type == PrayerType.AUGURY)
                prayerMultiplier = 1.25f;
        }

        int styleBonus = bonuses.isAccurate ? 2 : 0;

        return Mathf.Floor(
            Mathf.Floor(from.currentStats.magic * prayerMultiplier) * bonuses.voidMultiplier +
            styleBonus + 9);
    }

    /// <summary>
    /// Get equipment magic attack bonus.
    /// SDK Reference: MagicWeapon._equipmentBonus() in MagicWeapon.ts
    /// </summary>
    protected int GetEquipmentBonus(Unit from, Unit to, AttackBonuses bonuses)
    {
        return from.bonuses.attack.magic;
    }

    /// <summary>
    /// Get magic damage bonus multiplier.
    /// SDK Reference: MagicWeapon._magicDamageBonusMultiplier() in MagicWeapon.ts
    /// </summary>
    protected float GetMagicDamageMultiplier(Unit from, Unit to, AttackBonuses bonuses)
    {
        return from.bonuses.other.magicDamage;
    }

    /// <summary>
    /// Calculate attack roll.
    /// SDK Reference: MagicWeapon._attackRoll() in MagicWeapon.ts lines 82-86
    /// 
    /// Formula:
    /// attackRoll = floor(magicLevel * (equipmentBonus + 64) * gearMultiplier)
    /// </summary>
    protected override float CalculateAttackRoll(Unit from, Unit to, AttackBonuses bonuses)
    {
        float magicLevel = CalculateMagicLevel(from, to, bonuses);
        int equipmentBonus = GetEquipmentBonus(from, to, bonuses);

        return Mathf.Floor(
            magicLevel * (equipmentBonus + 64) * bonuses.gearMageMultiplier);
    }

    /// <summary>
    /// Calculate defence roll.
    /// SDK Reference: MagicWeapon._defenceRoll() in MagicWeapon.ts lines 88-114
    /// </summary>
    protected override float CalculateDefenceRoll(Unit from, Unit to, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        Prayer defencePrayer = bonuses.effectivePrayers.defence;

        if (defencePrayer != null)
        {
            if (defencePrayer.type == PrayerType.PIETY)
                prayerMultiplier = 1.25f;
            else if (defencePrayer.type == PrayerType.RIGOUR)
                prayerMultiplier = 1.25f;
            else if (defencePrayer.type == PrayerType.AUGURY)
                prayerMultiplier = 1.25f;
        }

        return (9 + to.currentStats.magic * prayerMultiplier) *
               (to.bonuses.defence.magic + 64);
    }

    /// <summary>
    /// Calculate max hit for magic attack.
    /// SDK Reference: MagicWeapon._maxHit() in MagicWeapon.ts lines 116-118
    /// 
    /// Formula:
    /// maxHit = floor(baseSpellDamage * magicDamageMultiplier)
    /// </summary>
    protected override float CalculateMaxHit(Unit from, Unit to, AttackBonuses bonuses)
    {
        float baseSpellDamage = bonuses.magicBaseSpellDamage;
        float damageMultiplier = GetMagicDamageMultiplier(from, to, bonuses);

        return Mathf.Floor(baseSpellDamage * damageMultiplier);
    }

    /// <summary>
    /// Get base spell damage. Override in spell subclasses.
    /// SDK Reference: MagicWeapon._baseSpellDamage() in MagicWeapon.ts
    /// </summary>
    protected virtual float GetBaseSpellDamage(Unit from, Unit to, AttackBonuses bonuses)
    {
        return bonuses.magicBaseSpellDamage;
    }
}
