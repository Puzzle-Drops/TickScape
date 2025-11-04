using UnityEngine;

/// <summary>
/// Super simple ranger that bypasses the weapon system entirely.
/// </summary>
public class SimpleRanger : Mob
{
    [Header("Ranger Settings")]
    [Tooltip("3D model for arrow projectiles")]
    public GameObject arrowPrefab;

    [Tooltip("Color if no model")]
    public Color projectileColor = Color.green;

    [Tooltip("Maximum damage")]
    public int maxHit = 12;

    protected override void Start()
    {
        base.Start();

        mobName = "Simple Ranger";
        rangeLevel = 60;
        hitpointsLevel = 50;
        SetStats();
    }

    // Override these to bypass weapon system
    public override int GetAttackRange() => 8;
    public override int GetAttackSpeed() => 4;

    public override string AttackStyleForNewAttack() => "range";

    /// <summary>
    /// Directly create projectile without weapon.
    /// </summary>
    public override void PerformAttack()
    {
        if (aggro == null || aggro.IsDying())
            return;

        // Roll damage
        int damage = Random.Range(0, maxHit + 1);

        // Calculate distance for projectile timing
        int distance = Mathf.Max(
            Mathf.Abs(aggro.gridPosition.x - gridPosition.x),
            Mathf.Abs(aggro.gridPosition.y - gridPosition.y)
        );

        // Create arrow projectile
        Projectile arrow = new Projectile(
            null,  // No weapon needed
            damage,
            this,
            aggro,
            "range",
            new ProjectileOptions
            {
                hidden = false,
                modelPrefab = arrowPrefab,
                modelScale = 1f,
                motionInterpolator = new ArcProjectileMotion(1.5f),
                color = projectileColor,
                size = 0.3f
            }
        );

        // Set proper flight time
        arrow.remainingDelay = Mathf.FloorToInt((3 + distance) / 6f) + 1;
        arrow.totalDelay = arrow.remainingDelay;

        aggro.AddProjectile(arrow);
    }
}