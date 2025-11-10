using UnityEngine;

/// <summary>
/// Melee weapon implementation.
/// SDK Reference: MeleeWeapon.ts
/// 
/// MELEE COMBAT FORMULAS:
/// - Attack Roll = attackLevel * (equipmentBonus + 64) * gearMultiplier
/// - Defence Roll = defenceLevel * (equipmentBonus + 64)
/// - Max Hit = floor((strengthLevel * (equipmentBonus + 64) + 320) / 640) * multipliers
/// </summary>
public class MeleeWeapon : Weapon
{
    public MeleeWeapon()
    {
        weaponType = AttackStyleType.MELEE;
        attackRange = 1;
        attackSpeed = 4;

        // Melee projectiles are instant and hidden
        projectileOptions.hidden = true;
    }

    public override bool Attack(Unit from, Unit to, AttackBonuses bonuses = null, ProjectileOptions options = null)
    {
        if (bonuses == null)
            bonuses = new AttackBonuses();

        // Set default attack style for melee
        if (string.IsNullOrEmpty(bonuses.attackStyle))
        {
            bonuses.attackStyle = "slash";
        }

        return base.Attack(from, to, bonuses, options);
    }

    /// <summary>
    /// Calculate prayer effects for melee combat.
    /// SDK Reference: MeleeWeapon._calculatePrayerEffects() in MeleeWeapon.ts lines 20-47
    /// </summary>
    protected override void CalculatePrayerEffects(Unit from, Unit to, AttackBonuses bonuses)
    {
        bonuses.effectivePrayers = new EffectivePrayers();

        // Attacker's offensive prayers (works for both players and mobs)
        if (from.prayerController != null)
        {
            Prayer offensiveAttack = from.prayerController.MatchGroup(PrayerGroup.ACCURACY);
            if (offensiveAttack != null)
            {
                bonuses.effectivePrayers.attack = offensiveAttack;
            }

            Prayer offensiveStrength = from.prayerController.MatchGroup(PrayerGroup.STRENGTH);
            if (offensiveStrength != null)
            {
                bonuses.effectivePrayers.strength = offensiveStrength;
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
    /// Check if attack is blockable by protection prayers.
    /// SDK Reference: MeleeWeapon.isBlockable() in MeleeWeapon.ts lines 49-60
    /// </summary>
    public override bool IsBlockable(Unit from, Unit to, string attackStyle)
    {
        // Create bonuses object for prayer calculation
        AttackBonuses bonuses = new AttackBonuses { attackStyle = attackStyle };
        CalculatePrayerEffects(from, to, bonuses);

        string prayerStyle = attackStyle;

        // Generalize all melee styles to "melee" for prayer checking
        if (Weapon.IsMeleeAttackStyle(prayerStyle))
        {
            prayerStyle = "melee";
        }

        if (bonuses.effectivePrayers?.overhead != null &&
            bonuses.effectivePrayers.overhead.GetFeature() == prayerStyle)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate strength level with prayer and style bonuses.
    /// SDK Reference: MeleeWeapon._strengthLevel() in MeleeWeapon.ts lines 62-85
    /// 
    /// Formula:
    /// strengthLevel = floor((baseStrength * prayerMultiplier) + styleBonus + 8) * voidMultiplier
    /// 
    /// Prayer Multipliers:
    /// - No prayer: 1.0
    /// - Piety: 1.23
    /// 
    /// Style Bonuses:
    /// - Aggressive: +3
    /// - Controlled: +1
    /// - Others: +0
    /// </summary>
    protected float CalculateStrengthLevel(Unit from, Unit to, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        Prayer strengthPrayer = bonuses.effectivePrayers?.strength;

        if (strengthPrayer != null)
        {
            if (strengthPrayer.type == PrayerType.PIETY)
                prayerMultiplier = 1.23f;
        }

        // Get style strength bonus
        if (from is Player)
        {
            AttackStyle style = GetAttackStyle();
            bonuses.styleStrengthBonus = AttackStylesController.Instance.GetStrengthBonus(style);
        }
        else
        {
            bonuses.styleStrengthBonus = 0;
        }

        return Mathf.Floor(
            (Mathf.Floor(from.currentStats.strength * prayerMultiplier) +
             bonuses.styleStrengthBonus + 8) * bonuses.voidMultiplier);
    }

    /// <summary>
    /// Calculate max hit for melee attack.
    /// SDK Reference: MeleeWeapon._maxHit() in MeleeWeapon.ts lines 87-92
    /// 
    /// Formula:
    /// maxHit = floor(floor((strengthLevel * (equipmentBonus + 64) + 320) / 640) * gearMultiplier * overallMultiplier)
    /// </summary>
    protected override float CalculateMaxHit(Unit from, Unit to, AttackBonuses bonuses)
    {
        float strengthLevel = CalculateStrengthLevel(from, to, bonuses);
        int equipmentBonus = from.bonuses.other.meleeStrength;

        return Mathf.Floor(
            Mathf.Floor((strengthLevel * (equipmentBonus + 64) + 320f) / 640f) *
            bonuses.gearMeleeMultiplier *
            bonuses.overallMultiplier);
    }

    /// <summary>
    /// Calculate attack level with prayer and style bonuses.
    /// SDK Reference: MeleeWeapon._attackLevel() in MeleeWeapon.ts lines 94-113
    /// 
    /// Prayer Multipliers:
    /// - Clarity of Thought: 1.05
    /// - Improved Reflexes: 1.10
    /// - Incredible Reflexes: 1.15
    /// - Chivalry: 1.15
    /// - Piety: 1.20
    /// </summary>
    protected float CalculateAttackLevel(Unit from, Unit to, AttackBonuses bonuses)
    {
        float prayerMultiplier = 1.0f;
        Prayer attackPrayer = bonuses.effectivePrayers?.attack;

        if (attackPrayer != null)
        {
            if (attackPrayer.type == PrayerType.PIETY)
                prayerMultiplier = 1.20f;
        }

        return Mathf.Floor(
            (Mathf.Floor(from.currentStats.attack * prayerMultiplier) +
             bonuses.styleBonus + 8) * bonuses.voidMultiplier);
    }

    /// <summary>
    /// Calculate attack roll.
    /// SDK Reference: MeleeWeapon._attackRoll() in MeleeWeapon.ts lines 115-120
    /// 
    /// Formula:
    /// attackRoll = attackLevel * (attackBonus + 64) * gearMultiplier
    /// </summary>
    protected override float CalculateAttackRoll(Unit from, Unit to, AttackBonuses bonuses)
    {
        float attackLevel = CalculateAttackLevel(from, to, bonuses);
        int attackBonus = GetAttackBonus(from, bonuses.attackStyle);

        return Mathf.Floor(
            attackLevel * (attackBonus + 64) * bonuses.gearMeleeMultiplier);
    }

    /// <summary>
    /// Get attack bonus for specific style.
    /// </summary>
    private int GetAttackBonus(Unit from, string style)
    {
        switch (style)
        {
            case "stab": return from.bonuses.attack.stab;
            case "slash": return from.bonuses.attack.slash;
            case "crush": return from.bonuses.attack.crush;
            default: return from.bonuses.attack.slash;
        }
    }

    /// <summary>
    /// Calculate defence roll.
    /// SDK Reference: MeleeWeapon._defenceRoll() in MeleeWeapon.ts lines 122-129
    /// 
    /// For NPCs:
    /// defenceRoll = (defence + 9) * (defenceBonus + 64)
    /// 
    /// For Players:
    /// defenceRoll = defenceLevel * (defenceBonus + 64)
    /// </summary>
    protected override float CalculateDefenceRoll(Unit from, Unit to, AttackBonuses bonuses)
    {
        if (!(to is Player))
        {
            // NPC defence
            int defenceBonus = GetDefenceBonus(to, bonuses.attackStyle);
            return (to.currentStats.defence + 9) * (defenceBonus + 64);
        }
        else
        {
            // Player defence
            float defenceLevel = CalculateDefenceLevel(from, to, bonuses);
            int defenceBonus = GetDefenceBonus(to, bonuses.attackStyle);
            return defenceLevel * (defenceBonus + 64);
        }
    }

    /// <summary>
    /// Get defence bonus for specific style.
    /// </summary>
    private int GetDefenceBonus(Unit from, string style)
    {
        switch (style)
        {
            case "stab": return from.bonuses.defence.stab;
            case "slash": return from.bonuses.defence.slash;
            case "crush": return from.bonuses.defence.crush;
            default: return from.bonuses.defence.slash;
        }
    }

    /// <summary>
    /// Calculate defence level with prayers.
    /// SDK Reference: MeleeWeapon._defenceLevel() in MeleeWeapon.ts lines 131-154
    /// 
    /// Prayer Multipliers:
    /// - Thick Skin: 1.05
    /// - Rock Skin: 1.10
    /// - Steel Skin: 1.15
    /// - Chivalry: 1.20
    /// - Piety: 1.25
    /// - Rigour: 1.25
    /// - Augury: 1.25
    /// </summary>
    protected float CalculateDefenceLevel(Unit from, Unit to, AttackBonuses bonuses)
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

        return Mathf.Floor(to.currentStats.defence * prayerMultiplier) +
               bonuses.styleBonus + 8;
    }

    public override int CalculateHitDelay(int distance)
    {
        // Melee is instant
        return 1;
    }
}