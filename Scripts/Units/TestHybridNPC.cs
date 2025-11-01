using UnityEngine;

/// <summary>
/// Example hybrid mob that switches between ranged and melee.
/// Uses ranged by default, switches to melee when player is adjacent.
/// 
/// SETUP:
/// 1. Create empty GameObject
/// 2. Add TestHybridNPC component
/// 3. Add child cube for visuals
/// 4. Assign BOTH melee weapon AND ranged weapon in Inspector
/// 5. Configure stats in Inspector
/// 
/// BEHAVIOR:
/// - Attacks with ranged when player is far
/// - 50% chance to switch to melee when player is in melee range
/// </summary>
public class TestHybridNPC : Mob
{
    void Start()
    {
        // Set mob properties
        mobName = "Hybrid Test Mob";
        
        // Set color to yellow for hybrid (optional - set on child cube)
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.yellow;
        }
    }

    /// <summary>
    /// Default to ranged attacks.
    /// Will be overridden by canMeleeIfClose() when player is adjacent.
    /// SDK Reference: Override attackStyleForNewAttack() pattern
    /// </summary>
    public override string AttackStyleForNewAttack()
    {
        return "range"; // Default to ranged
    }

    /// <summary>
    /// Allow switching to melee when player is close.
    /// SDK Reference: Override canMeleeIfClose() pattern
    /// 
    /// Returns "slash" to indicate this mob CAN melee when close.
    /// The PerformAttack() method will randomly switch to melee (50% chance).
    /// </summary>
    public override string CanMeleeIfClose()
    {
        return "slash"; // Can switch to melee when player adjacent
    }
}
