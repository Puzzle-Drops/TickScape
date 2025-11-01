using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Prayer types available in the game.
/// </summary>
public enum PrayerType
{
    // Protection prayers
    PROTECT_FROM_MELEE,
    PROTECT_FROM_MAGIC,
    PROTECT_FROM_RANGE,

    // Offensive prayers
    PIETY,      // Melee: +20% attack, +23% strength, +25% defence
    RIGOUR,     // Range: +20% attack, +23% strength, +25% defence
    AUGURY,     // Magic: +25% accuracy, +4% strength, +25% defence

    // Special prayer
    REDEMPTION  // Heals 25% of prayer level when HP drops below 10%
}
/// <summary>
/// Prayer group categories for filtering.
/// SDK Reference: PrayerGroups enum in BasePrayer.ts
/// </summary>
public enum PrayerGroup
{
    OVERHEAD,   // Protection prayers
    ACCURACY,   // Offensive accuracy prayers
    STRENGTH,   // Offensive strength prayers
    DEFENCE     // Defensive prayers
}

/// <summary>
/// Individual prayer with activation state.
/// SDK Reference: BasePrayer.ts
/// </summary>
[System.Serializable]
public class Prayer
{
    public PrayerType type;
    public bool isActive = false;

    public string GetName()
    {
        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE: return "Protect from Melee";
            case PrayerType.PROTECT_FROM_MAGIC: return "Protect from Magic";
            case PrayerType.PROTECT_FROM_RANGE: return "Protect from Range";
            case PrayerType.PIETY: return "Piety";
            case PrayerType.RIGOUR: return "Rigour";
            case PrayerType.AUGURY: return "Augury";
            case PrayerType.REDEMPTION: return "Redemption";
            default: return "Unknown";
        }
    }

    public List<PrayerGroup> GetGroups()
    {
        List<PrayerGroup> groups = new List<PrayerGroup>();

        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE:
            case PrayerType.PROTECT_FROM_MAGIC:
            case PrayerType.PROTECT_FROM_RANGE:
            case PrayerType.REDEMPTION:
                groups.Add(PrayerGroup.OVERHEAD);
                break;

            case PrayerType.PIETY:
                groups.Add(PrayerGroup.ACCURACY);
                groups.Add(PrayerGroup.STRENGTH);
                groups.Add(PrayerGroup.DEFENCE);
                break;

            case PrayerType.RIGOUR:
                groups.Add(PrayerGroup.ACCURACY);
                groups.Add(PrayerGroup.STRENGTH);
                groups.Add(PrayerGroup.DEFENCE);
                break;

            case PrayerType.AUGURY:
                groups.Add(PrayerGroup.ACCURACY);
                groups.Add(PrayerGroup.STRENGTH);
                groups.Add(PrayerGroup.DEFENCE);
                break;
        }

        return groups;
    }

    /// <summary>
    /// Get prayer level requirement.
    /// SDK Reference: BasePrayer.levelRequirement()
    /// </summary>
    public int GetLevelRequirement()
    {
        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE:
            case PrayerType.PROTECT_FROM_MAGIC:
            case PrayerType.PROTECT_FROM_RANGE:
                return 40;
            case PrayerType.PIETY:
                return 70;
            case PrayerType.RIGOUR:
                return 74;
            case PrayerType.AUGURY:
                return 77;
            case PrayerType.REDEMPTION:
                return 49;
            default:
                return 1;
        }
    }

    /// <summary>
    /// Get feature string for protection prayers.
    /// Used to match prayer to attack style (melee/magic/range).
    /// SDK Reference: BasePrayer.feature()
    /// </summary>
    public string GetFeature()
    {
        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE: return "melee";
            case PrayerType.PROTECT_FROM_MAGIC: return "magic";
            case PrayerType.PROTECT_FROM_RANGE: return "range";
            default: return null;
        }
    }

    /// <summary>
    /// Prayer drain rate per tick.
    /// SDK Reference: BasePrayer.drainRate()
    /// </summary>
    public float GetDrainRate()
    {
        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE:
            case PrayerType.PROTECT_FROM_MAGIC:
            case PrayerType.PROTECT_FROM_RANGE:
                return 1.0f;
            case PrayerType.PIETY:
            case PrayerType.RIGOUR:
            case PrayerType.AUGURY:
                return 2.0f;
            default:
                return 0f;
        }
    }
}

/// <summary>
/// Manages prayer activation and drain for a unit.
/// SDK Reference: PrayerController.ts
/// </summary>
public class PrayerController
{
    private Unit unit;
    public List<Prayer> prayers = new List<Prayer>();
    private float drainCounter = 0;

    public PrayerController(Unit unit)
    {
        this.unit = unit;

        // Initialize only the prayers we want
        prayers.Add(new Prayer { type = PrayerType.PROTECT_FROM_MELEE });
        prayers.Add(new Prayer { type = PrayerType.PROTECT_FROM_MAGIC });
        prayers.Add(new Prayer { type = PrayerType.PROTECT_FROM_RANGE });
        prayers.Add(new Prayer { type = PrayerType.PIETY });
        prayers.Add(new Prayer { type = PrayerType.RIGOUR });
        prayers.Add(new Prayer { type = PrayerType.AUGURY });
        prayers.Add(new Prayer { type = PrayerType.REDEMPTION });
    }

    /// <summary>
    /// Tick prayer drain. Called every game tick.
    /// SDK Reference: PrayerController.tick() in PrayerController.ts
    /// </summary>
    public void Tick()
    {
        // Calculate prayer drain this tick
        float drainThisTick = GetDrainRate();
        drainCounter += drainThisTick;

        float prayerDrainResistance = GetPrayerDrainResistance();

        // Drain prayer points
        while (drainCounter > prayerDrainResistance)
        {
            unit.currentStats.prayer--;
            drainCounter -= prayerDrainResistance;
        }

        // Deactivate all prayers if out of prayer points
        if (unit.currentStats.prayer <= 0)
        {
            DeactivateAll();
        }
    }

    /// <summary>
    /// Deactivate all prayers.
    /// SDK Reference: PrayerController.deactivateAll()
    /// </summary>
    public void DeactivateAll()
    {
        foreach (Prayer prayer in prayers)
        {
            prayer.isActive = false;
        }
        unit.currentStats.prayer = 0;
    }

    /// <summary>
    /// Get total prayer drain rate from all active prayers.
    /// SDK Reference: PrayerController.drainRate()
    /// </summary>
    private float GetDrainRate()
    {
        return ActivePrayers().Sum(p => p.GetDrainRate());
    }

    /// <summary>
    /// Calculate prayer drain resistance.
    /// Formula: 2 * prayer_bonus + 60
    /// SDK Reference: Player.prayerDrainResistance getter
    /// </summary>
    private float GetPrayerDrainResistance()
    {
        return 2 * unit.bonuses.other.prayer + 60;
    }

    public List<Prayer> ActivePrayers()
    {
        return prayers.Where(p => p.isActive).ToList();
    }

    public Prayer FindPrayerByName(string name)
    {
        return prayers.FirstOrDefault(p => p.GetName() == name);
    }

    public Prayer FindPrayerByType(PrayerType type)
    {
        return prayers.FirstOrDefault(p => p.type == type);
    }

    /// <summary>
    /// Find active prayer matching a specific group.
    /// SDK Reference: PrayerController.matchGroup()
    /// </summary>
    public Prayer MatchGroup(PrayerGroup group)
    {
        return ActivePrayers().FirstOrDefault(p => p.GetGroups().Contains(group));
    }

    /// <summary>
    /// Get active overhead (protection) prayer.
    /// SDK Reference: PrayerController.overhead()
    /// </summary>
    public Prayer GetOverhead()
    {
        return ActivePrayers().FirstOrDefault(p => p.GetGroups().Contains(PrayerGroup.OVERHEAD));
    }

    public void ActivatePrayer(PrayerType type)
    {
        Prayer prayer = FindPrayerByType(type);
        if (prayer != null)
        {
            prayer.isActive = true;
        }
    }

    public void DeactivatePrayer(PrayerType type)
    {
        Prayer prayer = FindPrayerByType(type);
        if (prayer != null)
        {
            prayer.isActive = false;
        }
    }

    public bool IsPrayerActive(PrayerType type)
    {
        Prayer prayer = FindPrayerByType(type);
        return prayer != null && prayer.isActive;
    }
}
