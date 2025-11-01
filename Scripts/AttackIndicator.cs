/// <summary>
/// Visual feedback indicators for mob attacks.
/// SDK Reference: AttackIndicators enum in Mob.ts
/// </summary>
public enum AttackIndicator
{
    NONE = 0,      // No feedback
    HIT = 1,       // Will damage player
    BLOCKED = 2,   // Player prayer blocked attack
    SCAN = 3       // Searching for player
}
