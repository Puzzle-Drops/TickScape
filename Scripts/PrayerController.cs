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

    // THREE-STATE SYSTEM:
    public bool isActive = false;      // Functional state (protection/overhead/drain)
    public bool isLit = false;         // Panel visual state (immediate)
    public bool? nextActiveState = null; // Queued for next tick

    // Sound state tracking (SDK: BasePrayer.ts lines 49-50)
    public bool willPlayOnSound = false;
    public bool willPlayOffSound = false;

    public string GetName()
    {
        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE: return "Protect Melee";
            case PrayerType.PROTECT_FROM_MAGIC: return "Protect Magic";
            case PrayerType.PROTECT_FROM_RANGE: return "Protect Range";
            case PrayerType.PIETY: return "Boost Melee";
            case PrayerType.RIGOUR: return "Boost Range";
            case PrayerType.AUGURY: return "Boost Magic";
            case PrayerType.REDEMPTION: return "Redeem";
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
    /// 
    /// SDK VALUES:
    /// - Protection prayers: 12
    /// - Piety/Rigour/Augury: 24
    /// - Redemption: 6
    /// </summary>
    public float GetDrainRate()
    {
        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE:
            case PrayerType.PROTECT_FROM_MAGIC:
            case PrayerType.PROTECT_FROM_RANGE:
                return 12f;
            case PrayerType.PIETY:
                return 24f;
            case PrayerType.RIGOUR:
                return 24f;
            case PrayerType.AUGURY:
                return 24f;
            case PrayerType.REDEMPTION:
                return 6f;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Get sprite resource name for loading from Resources/UI/Prayers/
    /// </summary>
    public string GetSpriteName()
    {
        switch (type)
        {
            case PrayerType.PROTECT_FROM_MELEE: return "protectfrommelee";
            case PrayerType.PROTECT_FROM_MAGIC: return "protectfrommagic";
            case PrayerType.PROTECT_FROM_RANGE: return "protectfromrange";
            case PrayerType.PIETY: return "piety";
            case PrayerType.RIGOUR: return "rigour";
            case PrayerType.AUGURY: return "augury";
            case PrayerType.REDEMPTION: return "redemption";
            default: return "unknown";
        }
    }

    /// <summary>
    /// Transfer queued state to active state at TICK START.
    /// SDK Reference: BasePrayer.tick() in BasePrayer.ts lines 29-35
    /// </summary>
    public void Tick()
    {
        if (nextActiveState.HasValue)
        {
            // Only NOW does the prayer actually activate/deactivate
            isActive = nextActiveState.Value;
            isLit = isActive;  // Sync visual with functional
            nextActiveState = null;
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
    public bool hasQuickPrayersActivated = false;

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
    /// Prayer tick - processes state changes and drain.
    /// Called at START of tick, BEFORE any combat or movement.
    /// </summary>
    public void Tick()
    {
        // STEP 1: Apply queued state changes (prayers activate/deactivate NOW)
        foreach (Prayer prayer in prayers)
        {
            prayer.Tick();
        }

        // STEP 2: Calculate drain based on ACTIVE prayers (not lit!)
        float drainThisTick = 0;
        foreach (Prayer prayer in prayers)
        {
            if (prayer.isActive) // Only drain for ACTUALLY active prayers
            {
                drainThisTick += prayer.GetDrainRate();
            }
        }

        drainCounter += drainThisTick;

        // Continue with drain logic...
        float prayerDrainResistance = GetPrayerDrainResistance();
        while (drainCounter > prayerDrainResistance)
        {
            unit.currentStats.prayer--;
            drainCounter -= prayerDrainResistance;
        }

        // Deactivate if out of prayer
        if (unit.currentStats.prayer <= 0)
        {
            ForceDeactivateAll(); //Use force method (no flicking when out of prayer)
        }
    }

    /// <summary>
    /// Force deactivate all prayers immediately (used when out of prayer points).
    /// Does NOT use TogglePrayer() - bypasses flicking mechanism.
    /// </summary>
    public void ForceDeactivateAll()
    {
        foreach (Prayer prayer in prayers)
        {
            prayer.isActive = false;
            prayer.isLit = false;
            prayer.nextActiveState = false;
        }
        unit.currentStats.prayer = 0;
        hasQuickPrayersActivated = false;
    }

    /// <summary>
    /// Deactivate quick prayers by simulating clicks on all currently lit prayers.
    /// Uses TogglePrayer() to support prayer flicking.
    /// SDK Reference: This is the "panic button" - turns off ALL active prayers, not just marked ones.
    /// </summary>
    public void DeactivateQuickPrayers()
    {
        hasQuickPrayersActivated = false;

        // Toggle off ALL currently lit prayers (not just quick prayer selections)
        foreach (Prayer prayer in prayers)
        {
            if (prayer.isLit)
            {
                TogglePrayer(prayer, unit);
                Debug.Log($"[QuickPrayer] Toggled off {prayer.GetName()}");
            }
        }
    }

    /// <summary>
    /// Check and trigger Redemption prayer healing.
    /// SDK Reference: PrayerController.checkRedemption() in PrayerController.ts lines 70-81
    /// </summary>
    public void CheckRedemption(Unit unit)
    {
        Prayer redemption = FindPrayerByType(PrayerType.REDEMPTION);
        if (redemption != null && redemption.isActive &&
            unit.currentStats.hitpoint > 0 &&
            unit.currentStats.hitpoint <= Mathf.Floor(unit.stats.hitpoint / 10f))
        {
            // Deactivate all prayers
            ForceDeactivateAll();

            // Heal 25% of prayer level
            unit.currentStats.hitpoint += Mathf.FloorToInt(unit.stats.prayer / 4f);
            unit.currentStats.hitpoint = Mathf.Min(unit.currentStats.hitpoint, unit.stats.hitpoint);

            // Play redemption heal sound
            AudioClip redemptionSound = Resources.Load<AudioClip>("Audio/Prayers/redemption_heal");
            if (redemptionSound != null)
            {
                AudioSource.PlayClipAtPoint(redemptionSound, unit.transform.position, 0.5f);
            }

            Debug.Log($"[REDEMPTION] Healed for {Mathf.Floor(unit.stats.prayer / 4f)} HP!");
        }
    }

    /// <summary>
    /// Toggle prayer - IMMEDIATE visual, DELAYED functional.
    /// SDK Reference: BasePrayer.toggle() in BasePrayer.ts lines 75-88
    /// 
    /// PRAYER FLICKING: If you activate and deactivate same tick,
    /// nextActiveState inverts and prayer never actually activates!
    /// </summary>
    public void TogglePrayer(Prayer prayer, Unit unit)
    {
        // Check level requirement
        if (unit.stats.prayer < prayer.GetLevelRequirement())
        {
            return;
        }

        // Toggle visual state IMMEDIATELY
        if (prayer.isLit)
        {
            prayer.isLit = false;
            prayer.willPlayOffSound = true;
        }
        else
        {
            prayer.isLit = true;
            prayer.willPlayOnSound = true;
        }

        // Queue functional state change for NEXT tick
        // CRITICAL: If clicked multiple times same tick, this INVERTS!
        // This is what enables prayer flicking!
        if (!prayer.nextActiveState.HasValue)
        {
            prayer.nextActiveState = prayer.isLit;
        }
        else
        {
            // Clicked again same tick - INVERT the queued state
            prayer.nextActiveState = !prayer.nextActiveState.Value;
        }

        // Handle conflicts (queue deactivation of conflicting prayers)
        HandleConflicts(prayer);
    }

    /// <summary>
    /// Handle conflicting prayers (same group).
    /// SDK Reference: BasePrayer.handleConflicts() in BasePrayer.ts lines 90-97
    /// </summary>
    private void HandleConflicts(Prayer activatingPrayer)
    {
        // Only handle conflicts if we're turning prayer ON
        if (!activatingPrayer.isLit)
            return;

        var groups = activatingPrayer.GetGroups();

        foreach (Prayer prayer in prayers)
        {
            if (prayer == activatingPrayer)
                continue;

            // Check if prayers share any groups
            bool hasConflict = false;
            foreach (var group in groups)
            {
                if (prayer.GetGroups().Contains(group))
                {
                    hasConflict = true;
                    break;
                }
            }

            if (hasConflict)
            {
                // Queue this prayer to turn off next tick
                prayer.nextActiveState = false;
                // Don't change isLit here - let player see the visual conflict
            }
        }
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

    public void ActivateQuickPrayers()
    {
        if (unit.currentStats.prayer <= 0)
        {
            Debug.Log("[PrayerController] Cannot activate quick prayers - out of prayer!");
            return;
        }

        hasQuickPrayersActivated = true;

        // Get selections from UISettings
        if (UISettings.Instance != null)
        {
            foreach (PrayerType prayerType in UISettings.Instance.quickPrayerSelections)
            {
                Prayer prayer = FindPrayerByType(prayerType);
                if (prayer != null && unit.stats.prayer >= prayer.GetLevelRequirement())
                {
                    // Only toggle prayers that aren't already lit
                    if (!prayer.isLit)
                    {
                        TogglePrayer(prayer, unit);
                        Debug.Log($"[QuickPrayer] Toggled on {prayer.GetName()}");
                    }
                    else
                    {
                        Debug.Log($"[QuickPrayer] Skipped {prayer.GetName()} (already active)");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Deactivate all prayers and quick prayer state.
    /// </summary>
    public void DeactivateAllPrayers()
    {
        hasQuickPrayersActivated = false;
        foreach (Prayer prayer in prayers)
        {
            prayer.isActive = false;
            prayer.isLit = false;
            prayer.nextActiveState = false;
        }
        drainCounter = 0;
    }

    /// <summary>
    /// Toggle quick prayers on/off.
    /// </summary>
    public void ToggleQuickPrayers()
    {
        if (hasQuickPrayersActivated)
        {
            DeactivateAllPrayers();
            Debug.Log("[QuickPrayer] Deactivated all prayers");
        }
        else
        {
            ActivateQuickPrayers();
        }
    }

    /// <summary>
    /// Deactivate prayers that conflict with the given prayer.
    /// </summary>
    private void DeactivateConflictingPrayers(Prayer prayer)
    {
        var groups = prayer.GetGroups();
        foreach (var group in groups)
        {
            var conflicting = MatchGroup(group);
            if (conflicting != null && conflicting != prayer)
            {
                conflicting.isActive = false;
            }
        }
    }


}
