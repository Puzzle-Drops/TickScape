using UnityEngine;

/// <summary>
/// Ranged weapon implementation.
/// SDK Reference: RangedWeapon.ts
/// 
/// RANGED COMBAT FORMULAS:
/// - Attack Roll = rangedLevel * (equipmentBonus + 64) * gearMultiplier * accuracyMultiplier
/// - Defence Roll = (defence + 9) * (defenceBonus + 64)
/// - Max Hit = floor(0.5 + ((rangedLevel * (equipmentBonus + 64)) / 640) * gearMultiplier) * damageMultiplier
/// </summary>
public class RangedWeapon : Weapon
{
    public RangedWeapon()
    {
        weaponType = AttackStyleType.RANGED;
        attackRange = 7;
        attackSpeed = 5;

        // Ranged projectiles are visible
        projectileOptions.hidden = false;
    }

    /// <summary>
    /// Calculate hit delay for ranged weapons.
    /// SDK Reference: RangedWeapon.calculateHitDelay() in RangedWeapon.ts
    /// 
    /// Formula: floor((3 + distance) / 6) + 1
    /// </summary>
    public override int CalculateHitDelay(int distance)
    {
        return Mathf.FloorToInt((3 + distance) / 6f) + 1;
    }

    /// <summary>
    /// Register ranged projectile with "range" style.
    /// SDK Reference: RangedWeapon.registerProjectile() in RangedWeapon.ts
    /// </summary>
    protected override void RegisterProjectile(Unit from, Unit to, AttackBonuses bonuses, ProjectileOptions options)
    {
        // Override attack style to "range"
        bonuses.attackStyle = "range";
        base.RegisterProjectile(from, to, bonuses, options);
    }

    /// <summary>
    /// Calculate prayer effects for ranged combat.
    /// SDK Reference: RangedWeapon._calculatePrayerEffects() in RangedWeapon.ts lines 23-41
    /// </summary>
    protected override void CalculatePrayerEffects(Unit from, Unit to, AttackBonuses bonuses)
    {
        bonuses.effectivePrayers = new EffectivePrayers();

        // Attacker's offensive prayers (works for both players and mobs)
        if (from.prayerController != null)
        {
            Prayer offensiveRange = from.prayerController.MatchGroup(PrayerGroup.ACCURACY);
            if (offensiveRange != null)
            {
                bonuses.effectivePrayers.range = offensiveRange;
            }
        }

        // Defender's prayers (works for both players and mobs)
        if (to.prayerController != null)
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
    /// Check if attack is blockable by Protect from Missiles.
    /// SDK Reference: RangedWeapon.isBlockable() in RangedWeapon.ts lines 43-51
    /// </summary>
    public override bool IsBlockable(Unit from, Unit to, string attackStyle)
    {
        // Create bonuses object for prayer calculation
        AttackBonuses bonuses = new AttackBonuses { attackStyle = attackStyle };
        CalculatePrayerEffects(from, to, bonuses);

        if (bonuses.effectivePrayers?.overhead != null &&
            bonuses.effectivePrayers.overhead.GetFeature() == "range")
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate ranged attack level with prayers.
    /// SDK Reference: RangedWeapon._rangedAttack() in RangedWeapon.ts lines 52-67
    /// 
    /// Prayer Multipliers:
    /// - No prayer: 1.0
    /// - Rigour: 1.20
    /// </summary>
    protected float CalculateRangedAttackLevel(Unit from, Unit to, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        Prayer rangePrayer = bonuses.effectivePrayers?.range;

        if (rangePrayer != null)
        {
            if (rangePrayer.type == PrayerType.RIGOUR)
                prayerMultiplier = 1.20f;
        }

        int styleBonus = bonuses.isAccurate ? 3 : 0;

        return (Mathf.Floor(Mathf.Floor(from.currentStats.range) * prayerMultiplier + styleBonus + 8)) *
               bonuses.voidMultiplier;
    }

    /// <summary>
    /// Calculate max hit for ranged attack.
    /// SDK Reference: RangedWeapon._maxHit() in RangedWeapon.ts lines 69-84
    /// 
    /// Formula:
    /// rangedStrength = floor(floor(range * prayerMult) + styleBonus + 8) * void
    /// maxHit = floor(0.5 + ((rangedStrength * (equipmentBonus + 64)) / 640) * gearMult) * damageMult
    /// </summary>
    protected override float CalculateMaxHit(Unit from, Unit to, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        Prayer rangePrayer = bonuses.effectivePrayers?.range;

        if (rangePrayer != null)
        {
            if (rangePrayer.type == PrayerType.RIGOUR)
                prayerMultiplier = 1.23f; // Strength component
        }

        int styleBonus = bonuses.isAccurate ? 3 : 0;

        float rangedStrength =
            Mathf.Floor(Mathf.Floor(from.currentStats.range) * prayerMultiplier + styleBonus + 8) *
            bonuses.voidMultiplier;

        float max = Mathf.Floor(
            Mathf.Floor(0.5f + ((rangedStrength * (from.bonuses.other.rangedStrength + 64)) / 640f) *
                        bonuses.gearRangeMultiplier) *
            CalculateDamageMultiplier(from, to, bonuses));

        return max;
    }

    /// <summary>
    /// Calculate attack roll.
    /// SDK Reference: RangedWeapon._attackRoll() in RangedWeapon.ts lines 86-92
    /// 
    /// Formula:
    /// attackRoll = floor(rangedAttackLevel * (attackBonus + 64) * gearMultiplier) * accuracyMultiplier
    /// </summary>
    protected override float CalculateAttackRoll(Unit from, Unit to, AttackBonuses bonuses)
    {
        float rangedAttack = CalculateRangedAttackLevel(from, to, bonuses);

        return Mathf.Floor(
            Mathf.Floor(rangedAttack * (from.bonuses.attack.range + 64) * bonuses.gearRangeMultiplier) *
            CalculateAccuracyMultiplier(from, to, bonuses));
    }

    /// <summary>
    /// Calculate defence roll.
    /// SDK Reference: RangedWeapon._defenceRoll() in RangedWeapon.ts lines 94-126
    /// </summary>
    protected override float CalculateDefenceRoll(Unit from, Unit to, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        Prayer defencePrayer = bonuses.effectivePrayers?.defence;

        if (defencePrayer != null)
        {
            if (defencePrayer.type == PrayerType.PIETY)
                prayerMultiplier = 1.25f;
            else if (defencePrayer.type == PrayerType.RIGOUR)
                prayerMultiplier = 1.25f;
            else if (defencePrayer.type == PrayerType.AUGURY)
                prayerMultiplier = 1.25f;
        }

        return (to.currentStats.defence * prayerMultiplier + 9) *
               (to.bonuses.defence.range + 64);
    }

    /// <summary>
    /// Calculate accuracy multiplier (used for special weapons like T-Bow).
    /// SDK Reference: RangedWeapon._accuracyMultiplier() in RangedWeapon.ts
    /// Override in specific weapons.
    /// </summary>
    protected virtual float CalculateAccuracyMultiplier(Unit from, Unit to, AttackBonuses bonuses)
    {
        return 1f;
    }

    /// <summary>
    /// Calculate damage multiplier (used for special weapons like T-Bow).
    /// SDK Reference: RangedWeapon._damageMultiplier() in RangedWeapon.ts
    /// Override in specific weapons.
    /// </summary>
    protected virtual float CalculateDamageMultiplier(Unit from, Unit to, AttackBonuses bonuses)
    {
        return 1f;
    }
}