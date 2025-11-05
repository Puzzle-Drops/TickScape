/// <summary>
/// Ammo type categories.
/// Blessings occupy ammo slot but don't consume on attack.
/// SDK Reference: AmmoType enum in Ammo.ts lines 4-7
/// </summary>
public enum AmmoType
{
    /// <summary>
    /// Blessings (Holy blessing, Unholy blessing, etc.)
    /// Do not consume on attack, provide stat bonuses
    /// </summary>
    BLESSING = 0,
    
    /// <summary>
    /// Regular ammo (arrows, bolts, etc.)
    /// Consumed on ranged attacks
    /// </summary>
    AMMO = 1
}
