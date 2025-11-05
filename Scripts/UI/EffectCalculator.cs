using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Calculates stat effects from consuming items.
/// Mirrors the exact formulas from each potion/food class.
/// </summary>
public static class EffectCalculator
{
    /// <summary>
    /// Calculate all effects from consuming an item.
    /// Returns empty list for non-consumables.
    /// </summary>
    public static List<StatEffect> CalculateEffects(Item item, Player player)
    {
        var effects = new List<StatEffect>();

        if (item is Food food)
        {
            effects.AddRange(CalculateFoodEffects(food, player));
        }
        else if (item is VitalityPotion)
        {
            effects.AddRange(CalculateVitalityEffects(player));
        }
        else if (item is StrengthPotion)
        {
            effects.AddRange(CalculateStrengthEffects(player));
        }
        else if (item is RangePotion)
        {
            effects.AddRange(CalculateRangeEffects(player));
        }
        else if (item is MagePotion)
        {
            effects.AddRange(CalculateMageEffects(player));
        }
        else if (item is RestorationPotion)
        {
            effects.AddRange(CalculateRestorationEffects(player));
        }
        else if (item is RunPotion)
        {
            effects.AddRange(CalculateRunEffects(player));
        }

        // Filter out zero effects
        return effects.FindAll(e => e.HasEffect);
    }

    /// <summary>
    /// Calculate food healing effect.
    /// </summary>
    private static List<StatEffect> CalculateFoodEffects(Food food, Player player)
    {
        var effects = new List<StatEffect>();

        int currentHP = player.currentStats.hitpoint;
        int maxHP = player.stats.hitpoint;
        int healAmount = food.healAmount;

        // Cap at max HP (no overheal for regular food)
        int newHP = Mathf.Min(currentHP + healAmount, maxHP);
        int actualChange = newHP - currentHP;

        if (actualChange > 0)
        {
            effects.Add(new StatEffect("Hitpoints", actualChange, currentHP, newHP));
        }

        return effects;
    }

    /// <summary>
    /// Calculate Vitality Potion effects.
    /// Formula from VitalityPotion.cs
    /// </summary>
    private static List<StatEffect> CalculateVitalityEffects(Player player)
    {
        var effects = new List<StatEffect>();

        // Heal hitpoints (can overheal)
        int currentHP = player.currentStats.hitpoint;
        int maxHP = player.stats.hitpoint;
        int healAmount = Mathf.FloorToInt(maxHP * 0.15f) + 2;
        int newHP = Mathf.Max(1, Mathf.Min(currentHP + healAmount, maxHP + healAmount));

        effects.Add(new StatEffect("Hitpoints", newHP - currentHP, currentHP, newHP));

        // Boost defence (calculated from current, never lower)
        int currentDef = player.currentStats.defence;
        int baseDef = player.stats.defence;
        int defenceBoost = Mathf.FloorToInt(currentDef * 0.2f) + 2;
        int maxDefenceBoost = Mathf.FloorToInt(baseDef * 0.2f) + 2;
        int newDef = currentDef + defenceBoost;
        newDef = Mathf.Max(1, Mathf.Min(newDef, baseDef + maxDefenceBoost)); // Cap at base + max boost
        newDef = Mathf.Max(newDef, currentDef); // Never lower

        effects.Add(new StatEffect("Defence", newDef - currentDef, currentDef, newDef));

        // Drain attack
        int currentAtk = player.currentStats.attack;
        int baseAtk = player.stats.attack;
        int attackNerf = Mathf.FloorToInt(currentAtk * 0.1f) + 2;
        int newAtk = Mathf.Max(1, Mathf.Min(currentAtk - attackNerf, baseAtk));

        effects.Add(new StatEffect("Attack", newAtk - currentAtk, currentAtk, newAtk));

        // Drain strength
        int currentStr = player.currentStats.strength;
        int baseStr = player.stats.strength;
        int strengthNerf = Mathf.FloorToInt(currentStr * 0.1f) + 2;
        int newStr = Mathf.Max(1, Mathf.Min(currentStr - strengthNerf, baseStr));

        effects.Add(new StatEffect("Strength", newStr - currentStr, currentStr, newStr));

        // Drain range
        int currentRng = player.currentStats.range;
        int baseRng = player.stats.range;
        int rangeNerf = Mathf.FloorToInt(currentRng * 0.1f) + 2;
        int newRng = Mathf.Max(1, Mathf.Min(currentRng - rangeNerf, baseRng));

        effects.Add(new StatEffect("Ranged", newRng - currentRng, currentRng, newRng));

        // Drain magic
        int currentMag = player.currentStats.magic;
        int baseMag = player.stats.magic;
        int magicNerf = Mathf.FloorToInt(currentMag * 0.1f) + 2;
        int newMag = Mathf.Max(1, Mathf.Min(currentMag - magicNerf, baseMag));

        effects.Add(new StatEffect("Magic", newMag - currentMag, currentMag, newMag));

        return effects;
    }

    /// <summary>
    /// Calculate Strength Potion effects.
    /// Formula from StrengthPotion.cs
    /// Boost calculated from BASE, applied to CURRENT, capped, never lowered.
    /// </summary>
    private static List<StatEffect> CalculateStrengthEffects(Player player)
    {
        var effects = new List<StatEffect>();

        // Boost attack
        int currentAtk = player.currentStats.attack;
        int baseAtk = player.stats.attack;
        int attackBoost = Mathf.FloorToInt(baseAtk * 0.15f) + 5;
        int newAtk = currentAtk + attackBoost;
        newAtk = Mathf.Min(newAtk, baseAtk + attackBoost); // Cap at base + boost
        newAtk = Mathf.Max(newAtk, currentAtk); // Never lower

        effects.Add(new StatEffect("Attack", newAtk - currentAtk, currentAtk, newAtk));

        // Boost strength
        int currentStr = player.currentStats.strength;
        int baseStr = player.stats.strength;
        int strengthBoost = Mathf.FloorToInt(baseStr * 0.15f) + 5;
        int newStr = currentStr + strengthBoost;
        newStr = Mathf.Min(newStr, baseStr + strengthBoost); // Cap at base + boost
        newStr = Mathf.Max(newStr, currentStr); // Never lower

        effects.Add(new StatEffect("Strength", newStr - currentStr, currentStr, newStr));

        // Boost defence
        int currentDef = player.currentStats.defence;
        int baseDef = player.stats.defence;
        int defenceBoost = Mathf.FloorToInt(baseDef * 0.15f) + 5;
        int newDef = currentDef + defenceBoost;
        newDef = Mathf.Min(newDef, baseDef + defenceBoost); // Cap at base + boost
        newDef = Mathf.Max(newDef, currentDef); // Never lower

        effects.Add(new StatEffect("Defence", newDef - currentDef, currentDef, newDef));

        return effects;
    }

    /// <summary>
    /// Calculate Range Potion effects.
    /// Formula from RangePotion.cs
    /// Boost calculated from BASE, applied to CURRENT, capped, never lowered.
    /// </summary>
    private static List<StatEffect> CalculateRangeEffects(Player player)
    {
        var effects = new List<StatEffect>();

        // Boost range
        int currentRng = player.currentStats.range;
        int baseRng = player.stats.range;
        int rangeBoost = Mathf.FloorToInt(baseRng * 0.1f) + 4;
        int newRng = currentRng + rangeBoost;
        newRng = Mathf.Min(newRng, baseRng + rangeBoost); // Cap at base + boost
        newRng = Mathf.Max(newRng, currentRng); // Never lower

        effects.Add(new StatEffect("Ranged", newRng - currentRng, currentRng, newRng));

        // Boost defence
        int currentDef = player.currentStats.defence;
        int baseDef = player.stats.defence;
        int defenceBoost = Mathf.FloorToInt(baseDef * 0.15f) + 5;
        int newDef = currentDef + defenceBoost;
        newDef = Mathf.Min(newDef, baseDef + defenceBoost); // Cap at base + boost
        newDef = Mathf.Max(newDef, currentDef); // Never lower

        effects.Add(new StatEffect("Defence", newDef - currentDef, currentDef, newDef));

        return effects;
    }

    /// <summary>
    /// Calculate Mage Potion effects.
    /// Formula from MagePotion.cs
    /// Boost calculated from BASE, applied to CURRENT, capped, never lowered.
    /// </summary>
    private static List<StatEffect> CalculateMageEffects(Player player)
    {
        var effects = new List<StatEffect>();

        // Boost magic
        int currentMag = player.currentStats.magic;
        int baseMag = player.stats.magic;
        int magicBoost = Mathf.FloorToInt(baseMag * 0.1f) + 4;
        int newMag = currentMag + magicBoost;
        newMag = Mathf.Min(newMag, baseMag + magicBoost); // Cap at base + boost
        newMag = Mathf.Max(newMag, currentMag); // Never lower

        effects.Add(new StatEffect("Magic", newMag - currentMag, currentMag, newMag));

        // Boost defence
        int currentDef = player.currentStats.defence;
        int baseDef = player.stats.defence;
        int defenceBoost = Mathf.FloorToInt(baseDef * 0.15f) + 5;
        int newDef = currentDef + defenceBoost;
        newDef = Mathf.Min(newDef, baseDef + defenceBoost); // Cap at base + boost
        newDef = Mathf.Max(newDef, currentDef); // Never lower

        effects.Add(new StatEffect("Defence", newDef - currentDef, currentDef, newDef));

        return effects;
    }
    
    /// <summary>
    /// Calculate Restoration Potion effects.
    /// Formula from RestorationPotion.cs
    /// IMPORTANT: Restores never lower stats, only increase them (up to base).
    /// </summary>
    private static List<StatEffect> CalculateRestorationEffects(Player player)
    {
        var effects = new List<StatEffect>();

        PlayerStats pStats = player.currentStats as PlayerStats;
        PlayerStats baseStats = player.stats as PlayerStats;

        if (pStats == null || baseStats == null) return effects;

        // Restore prayer (always, never lower)
        int currentPrayer = pStats.prayer;
        int basePrayer = baseStats.prayer;
        int prayerBonus = Mathf.FloorToInt(basePrayer * 0.27f) + 8;
        int newPrayer = Mathf.Max(currentPrayer, Mathf.Min(currentPrayer + prayerBonus, basePrayer)); // Never lower

        effects.Add(new StatEffect("Prayer", newPrayer - currentPrayer, currentPrayer, newPrayer));

        // Restore attack (if below base, never lower if above)
        int currentAtk = pStats.attack;
        int baseAtk = baseStats.attack;
        if (currentAtk < baseAtk)
        {
            int attackBonus = Mathf.FloorToInt(baseAtk * 0.25f) + 8;
            int newAtk = Mathf.Min(currentAtk + attackBonus, baseAtk);

            effects.Add(new StatEffect("Attack", newAtk - currentAtk, currentAtk, newAtk));
        }

        // Restore strength (if below base, never lower if above)
        int currentStr = pStats.strength;
        int baseStr = baseStats.strength;
        if (currentStr < baseStr)
        {
            int strengthBonus = Mathf.FloorToInt(baseStr * 0.25f) + 8;
            int newStr = Mathf.Min(currentStr + strengthBonus, baseStr);

            effects.Add(new StatEffect("Strength", newStr - currentStr, currentStr, newStr));
        }

        // Restore range (if below base, never lower if above)
        int currentRng = pStats.range;
        int baseRng = baseStats.range;
        if (currentRng < baseRng)
        {
            int rangeBonus = Mathf.FloorToInt(baseRng * 0.25f) + 8;
            int newRng = Mathf.Min(currentRng + rangeBonus, baseRng);

            effects.Add(new StatEffect("Ranged", newRng - currentRng, currentRng, newRng));
        }

        // Restore magic (if below base, never lower if above)
        int currentMag = pStats.magic;
        int baseMag = baseStats.magic;
        if (currentMag < baseMag)
        {
            int magicBonus = Mathf.FloorToInt(baseMag * 0.25f) + 8;
            int newMag = Mathf.Min(currentMag + magicBonus, baseMag);

            effects.Add(new StatEffect("Magic", newMag - currentMag, currentMag, newMag));
        }

        return effects;
    }

    /// <summary>
    /// Calculate Run Potion effects.
    /// Formula from RunPotion.cs
    /// </summary>
    private static List<StatEffect> CalculateRunEffects(Player player)
    {
        var effects = new List<StatEffect>();

        PlayerStats pStats = player.currentStats as PlayerStats;

        if (pStats == null) return effects;

        // Restore run energy (display as percentage for clarity)
        int currentRun = pStats.run;
        int newRun = Mathf.Clamp(currentRun + 2000, 0, 10000);
        int change = newRun - currentRun;

        if (change > 0)
        {
            // Convert to percentage for display (2000 = 20%)
            int changePercent = change / 100;
            int currentPercent = currentRun / 100;
            int newPercent = newRun / 100;

            effects.Add(new StatEffect("Run Energy", changePercent, currentPercent, newPercent));
        }

        return effects;
    }

}
