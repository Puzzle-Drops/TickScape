/// <summary>
/// Types of set effects in the game.
/// SDK Reference: SetEffectTypes enum in SetEffect.ts lines 1-3
/// </summary>
public enum SetEffectType
{
    /// <summary>
    /// Justiciar armor set effect.
    /// Reduces incoming damage based on defence bonus.
    /// Formula: Reduction = min(bonusDefence / 3000, 1.0)
    /// </summary>
    JUSTICIAR
}
