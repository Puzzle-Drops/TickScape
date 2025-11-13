using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders projectiles visually by updating their self-managed visuals.
/// Attach to each Unit that can receive projectiles.
/// 
/// SDK Pattern: Projectiles manage their own visual lifecycle.
/// This component just calls UpdateVisual() each frame.
/// </summary>
[RequireComponent(typeof(Unit))]
public class ProjectileRenderer : MonoBehaviour
{
    private Unit unit;

    void Start()
    {
        unit = GetComponent<Unit>();

        if (unit == null)
        {
            Debug.LogError("ProjectileRenderer: No Unit component found!");
            enabled = false;
        }
    }

    void LateUpdate()
    {
        if (unit == null || unit.incomingProjectiles == null)
            return;

        if (WorldManager.Instance == null)
            return;

        // Get tick percent for smooth interpolation
        // This ensures projectiles update in sync with unit movement
        float tickPercent = WorldManager.Instance.GetTickPercent();

        // Update all projectile visuals every frame for smooth movement
        // SDK Pattern: Projectiles interpolate between ticks using tickPercent
        foreach (var projectile in unit.incomingProjectiles)
        {
            if (!projectile.options.hidden)
            {
                projectile.UpdateVisual(tickPercent);
            }
        }

        // Clean up visuals for destroyed projectiles
        // Note: Unit.ProcessIncomingAttacks() handles removing from list
        // We just need to ensure visuals are cleaned up
        for (int i = unit.incomingProjectiles.Count - 1; i >= 0; i--)
        {
            Projectile projectile = unit.incomingProjectiles[i];

            if (projectile.ShouldDestroy())
            {
                // Ensure visual is destroyed
                projectile.DestroyVisual();
            }
        }
    }

    void OnDestroy()
    {
        // Clean up all projectile visuals when this component is destroyed
        if (unit != null && unit.incomingProjectiles != null)
        {
            foreach (var projectile in unit.incomingProjectiles)
            {
                projectile.DestroyVisual();
            }
        }
    }
}
