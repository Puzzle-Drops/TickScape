using UnityEngine;

/// <summary>
/// Example pure melee mob.
/// Always attacks with melee (slash style).
/// 
/// SETUP:
/// 1. Create empty GameObject
/// 2. Add TestMeleeNPC component
/// 3. Add child cube for visuals (scale to 1x1x1 for single tile)
/// 4. Assign melee weapon in Inspector
/// 5. Configure stats in Inspector
/// </summary>
public class TestMeleeNPC : Mob
{
    void Start()
    {
        // Set mob properties
        mobName = "Melee Test Dummy";
        
        // Set color to red for melee (optional - set on child cube)
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }
    }

    /// <summary>
    /// Always use melee attacks.
    /// SDK Reference: Override attackStyleForNewAttack() pattern
    /// </summary>
    public override string AttackStyleForNewAttack()
    {
        return "slash"; // Always melee
    }

    /// <summary>
    /// This is a pure melee mob, so never switches styles.
    /// SDK Reference: Override canMeleeIfClose() pattern
    /// </summary>
    public override string CanMeleeIfClose()
    {
        return ""; // Don't switch (already melee)
    }
}
